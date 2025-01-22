using System.IO;
using System.Linq;
using Autumn;
using Illusion.Extensions;
using SmartRectV0;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace KoiUpdater.Shared.Windows;

public class ListWindow
{
    private Rect _listWindowRect;
    private Vector2 _listScroll;
    public static bool _isEnabled;
    private string searchQuery = string.Empty;
    private GUIStyle _windowStyle;
    private GUIStyle _updateButtonStyle;
    private Texture2D UpdateButtonTexture;
    private Texture2D UpdateButtonTextureHover;

    public void OnGui()
    {
        if (_isEnabled)
        {
            var windowTitle = "KPU";
            if (searchQuery.Trim() != string.Empty)
                windowTitle += $" | Filtered";
            _listWindowRect = GUI.Window(34287, _listWindowRect, ListWindowFunc, windowTitle, _windowStyle);
        }
    }

    public ListWindow()
    {
        _listScroll = Vector2.zero;
        _listWindowRect = new Rect(Screen.width * 0.5f - (Screen.width * 0.4f / 2), Screen.height * 0.5f, Screen.width * 0.4f,
            Screen.height * 0.3f);
        UpdateButtonTexture = TextureFactory.SetBorder(TextureFactory.Fill(new Texture2D(400, 120), new Color(1, 1, 1)), 60, TextureFactory.Border.All);
        UpdateButtonTextureHover = TextureFactory.SetBorder(TextureFactory.Fill(new Texture2D(400, 120), new Color(0.8f, .8f, .8f)), 60, TextureFactory.Border.All);
        _updateButtonStyle = new GUIStyle()
        {
          normal  =
          {
              background = UpdateButtonTexture
          },
          hover =
          {
              background = UpdateButtonTextureHover
          },
          alignment = TextAnchor.MiddleCenter
        };
        _windowStyle = new GUIStyle
        {
            normal =
            {
                background = Autumn.TextureFactory.SetBorder(
                    Autumn.TextureFactory.Fill(new Texture2D((int)_listWindowRect.width, (int)_listWindowRect.height),
                        new Color(0.4f, 0.4f, 0.4f, 0.9f)), 15, TextureFactory.Border.All),
                textColor = Color.white
            },
            alignment = TextAnchor.UpperCenter,
        };
    }

    private void ListWindowFunc(int id)
    {
        GUI.enabled = !hideUI;
        var skinLabel = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        SmartRect rect = new SmartRect(0, 0, _listWindowRect.width * 0.79f, 30);
        searchQuery = GUI.TextArea(new Rect(30, 0, 150, 20), searchQuery).TrimStart();
        var filteredList = KoiUpdaterUI.plugins.Where(e =>
            e.Name.ToLowerInvariant().Contains(searchQuery.ToLowerInvariant().Replace("#", string.Empty)));
        if (searchQuery.StartsWith("#"))
        {
            filteredList = filteredList.Where(e => e.Updatable);
        }
        _listScroll = GUI.BeginScrollView(new Rect(10, 20, _listWindowRect.width - 15, _listWindowRect.height - 25),
            _listScroll,
            new Rect(0, 0, _listWindowRect.width, 20 * filteredList.Count()), new GUIStyle(),
            GUI.skin.verticalScrollbar);
        foreach (var pluginInfo in filteredList)
        {
            GUI.Label(rect.ToRect(), pluginInfo.Name, skinLabel);
            if (pluginInfo.Updatable && SetupWindow._finishSetup)
            {
                rect.NextColumn();
                rect.Width = 100;
                if (GUI.Button(rect.ToRect(), "Update", _updateButtonStyle))
                {
                    var serverUri = Entry._serverUrl.Value;
                    if (!serverUri.EndsWith("/"))
                        serverUri += "/";
                    Download($"{serverUri}plugins/{pluginInfo.Uid}/download", pluginInfo.Path, pluginInfo);
                }
                rect.Width = rect.DefaultWidth;
            }
            rect.NextRow();
        }

        GUI.EndScrollView();
        GUI.DragWindow();
    }

    private bool hideUI;
    public void Download(string uri, string path, PluginInfo pluginInfo)
    {
        hideUI = true;
        Entry._logger.LogInfo("Download file from: " + uri);
        UnityWebRequest request = UnityWebRequest.Get(uri);
#if KKS
        request.SendWebRequest().AsAsyncOperationObservable().Subscribe(op =>
        {
            if (op.webRequest.isHttpError || op.webRequest.downloadHandler.data.Length == 0)
            {
                Entry._logger.LogError("File not found, or empty.");
                hideUI = false;
                return;
            }
            var tempDirectory = Path.Combine(Directory.GetParent(KoiUpdaterUI.PluginsPath).FullName, "old_plugins");
            if (!Directory.Exists(tempDirectory))
                Directory.CreateDirectory(tempDirectory);
            File.Move(path, tempDirectory + @"\" + pluginInfo.Name);
            File.WriteAllBytes(path, op.webRequest.downloadHandler.data);
            pluginInfo.Updatable = false;
            hideUI = false;
        });
#endif
    }
}