using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KoiUpdater.Shared.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartRectV0;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace KoiUpdater.Shared;

public class PluginInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Hash { get; set; }
    public bool Updatable { get; set; }
    public string Uid { get; set; }
}

static class JsonExtension
{
    public static object FromJSON(this string text)
    {
        try
        {
            return JsonConvert.DeserializeObject(text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static JArray FromJSONArray(this string text)
    {
        try
        {
            return JsonConvert.DeserializeObject<JArray>(text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static JObject FromJSONObject(this string text)
    {
        try
        {
            return JsonConvert.DeserializeObject<JObject>(text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string ToJson<T>(this T array)
    {
        return JsonConvert.SerializeObject(array);
    }
}

internal class KoiUpdaterUI : UnityEngine.MonoBehaviour
{
    public static string currentFile;
    private UniRx.Progress<float> _progressor;
    public static string[] p = new string[0];
    private SmartRect _logRect = new(0, 0, 200, 25);
    public static int loadedPluginCount = 0;
    private Subject<int> progressSubject = new();
    internal static Texture2D progressDot = new(1, 1);
    internal static Texture2D progressBar = new(1, 1);
    private int step = 0;
    public static List<PluginInfo> plugins = [];

    public static string PluginsPath;

    private void Awake()
    {
        PluginsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, @"BepInEx\plugins");
        cctrl = setcctrl();
        progressSubject.ObserveOnMainThread().Subscribe(progress => { loadedPluginCount = progress; });
        Observable.Start(() =>
        {
            Entry._logger.LogInfo("Plugins Path: " + PluginsPath);
            p = Directory.GetFiles(PluginsPath, "*.dll", SearchOption.AllDirectories);
            var t = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int i = 1;
            foreach (var plugin in p)
            {
                currentFile = plugin;
                plugins.Add(new PluginInfo()
                {
                    Hash = FileHash(plugin),
                    Name = new FileInfo(plugin).Name,
                    Path = plugin,
                    Updatable = false
                });
                progressSubject.OnNext(i++); // Update progress
            }

            Entry._logger.LogWarning(
                $"Took: {DateTimeOffset.Now.ToUnixTimeMilliseconds() - t}ms AVG: {(DateTimeOffset.Now.ToUnixTimeMilliseconds() - t) / p.Length}ms");
        }).Subscribe(Resolve);
    }

    private void Resolve(Unit obj)
    {
        var requestData = plugins.Select(e => e.Name).ToJson();
        var serverUri = Entry._serverUrl.Value;
        if (!serverUri.EndsWith("/"))
            serverUri += "/";
        serverUri += "resolve";
        Entry._logger.LogInfo("Request to " + serverUri);
        var uwr = UnityWebRequest.Post(serverUri, requestData);
#if KKS
        uwr.SendWebRequest().AsAsyncOperationObservable(_progressor).Subscribe(operation =>
        {
            try
            {
                var data = operation.webRequest.downloadHandler.text;
                if (data == string.Empty)
                {
                    Entry._logger.LogError("Server did not respond > " + operation.webRequest.responseCode);
                    return;
                }

                var js = data.FromJSONArray();
                foreach (var j in js)
                {
                    var Name = (j["Name"] ?? throw new InvalidOperationException()).Value<string>();
                    var Hash = (j["Hash"] ?? throw new InvalidOperationException()).Value<string>();
                    var Uid = (j["Uid"] ?? throw new InvalidOperationException()).Value<string>();

                    var pluginInfo = plugins.FirstOrDefault(e => e.Name == Name);
                    if (pluginInfo == null || Hash == pluginInfo.Hash) continue;
                    pluginInfo.Updatable = true;
                    pluginInfo.Uid = Uid;
                }
            }
            catch (Exception e)
            {
                Entry._logger.LogError(e);
            }
        });
#endif
    }

    private ListWindow lst = new ListWindow();
    private SetupWindow sst = new SetupWindow();

    private void OnGUI()
    {
        cctrl.GetComponent<Studio.CameraControl>().enabled = !ListWindow._isEnabled;
        if (plugins.Count > 0) lst.OnGui();
        if (!SetupWindow._finishSetup) sst.OnGui();
    }

    private Camera cctrl;

    private Camera setcctrl()
    {
        foreach (var cam in FindObjectsOfType<Camera>())
        {
            var c = cam.GetComponent<Studio.CameraControl>();
            if (c != null)
                return cam;
        }

        return null;
    }

    private void Update()
    {
        if (Entry._openUI.Value.IsDown())
            ListWindow._isEnabled = !ListWindow._isEnabled;
    }

    private string FileHash(string path)
    {
        using (FileStream fs = File.OpenRead(path))
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }

    
    private string PluginList()
    {
        TextWriter tw = new StringWriter();
        JsonWriter writer = new JsonTextWriter(tw);
    
    
        string[] plugins = Directory.GetFiles(PluginsPath, "*.dll", SearchOption.AllDirectories)
            .Select(
                e =>
                {
                    FileInfo fileInfo = new FileInfo(e);
                    return fileInfo.Name;
                }).ToArray();
        writer.WriteStartArray();
        foreach (var plugin in plugins)
        {
            writer.WriteValue(plugin);
        }
    
        writer.WriteEndArray();
    
        writer.Close();
    
        return tw.ToString();
    }


    private void ProgressHandler(float obj)
    {
        Entry._logger.LogInfo($"{obj}%");
    }
}