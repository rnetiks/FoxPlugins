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

    public void OnGui()
    {
        if (_isEnabled)
        {
            _listWindowRect = GUI.Window(34287, _listWindowRect, ListWindowFunc, $"KPU", _windowStyle);
        }
    }

    public ListWindow()
    {
        _listScroll = Vector2.zero;
        _listWindowRect = new Rect(Screen.width * 0.5f, Screen.height * 0.5f, Screen.width * 0.4f,
            Screen.height * 0.3f);
        _windowStyle = new GUIStyle
        {
            normal =
            {
                background = Autumn.TextureFactory.SetBorder(
                    Autumn.TextureFactory.Fill(new Texture2D((int)_listWindowRect.width, (int)_listWindowRect.height),
                        Color.gray), 15, TextureFactory.Border.All)
            },
            alignment = TextAnchor.UpperCenter
        };
    }

    private void ListWindowFunc(int id)
    {
        var skinLabel = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        SmartRect rect = new SmartRect(0, 0, _listWindowRect.width * 0.79f, 30);
        searchQuery = GUI.TextArea(new Rect(30, 0, 150, 20), searchQuery);
        _listScroll = GUI.BeginScrollView(new Rect(10, 20, _listWindowRect.width - 15, _listWindowRect.height - 25),
            _listScroll,
            new Rect(0, 0, _listWindowRect.width, 20 * KoiUpdaterUI.plugins.Count), new GUIStyle(),
            GUI.skin.verticalScrollbar);
        foreach (var pluginInfo in KoiUpdaterUI.plugins.Where(e =>
                     e.Name.ToLowerInvariant().Contains(searchQuery.ToLowerInvariant())))
        {
            GUI.Label(rect.ToRect(), pluginInfo.Name, skinLabel);
            if (pluginInfo.Updatable && SetupWindow._finishSetup)
            {
                rect.NextColumn();
                rect.Width = 100;
                if (GUI.Button(rect.ToRect(), "Update"))
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

    public void Download(string uri, string path, PluginInfo pluginInfo)
    {
        UnityWebRequest request = UnityWebRequest.Get(uri);
        request.SendWebRequest().AsAsyncOperationObservable().Subscribe(op =>
        {
            var tempDirectory = Path.Combine(Directory.GetParent(KoiUpdaterUI.PluginsPath).FullName, "temp");
            if (!Directory.Exists(tempDirectory))
                Directory.CreateDirectory(tempDirectory);
            File.Move(path, tempDirectory);
            File.WriteAllBytes(path, op.webRequest.downloadHandler.data);
            pluginInfo.Updatable = false;
        });
    }
}