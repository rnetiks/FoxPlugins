using System;
using System.IO;
using UnityEngine;

namespace KoiUpdater.Shared.Windows;

public class SetupWindow
{
    private Rect _setupWindowRect;
    internal static bool _finishSetup;

    public SetupWindow()
    {
        float vw = 0.2f, vh = 0.1f;
        _setupWindowRect = new Rect(Screen.width / 2f - Screen.width * vw / 2,
            Screen.height / 2f - Screen.height * vh / 2, Screen.width * vw, Screen.height * vh);
    }

    public void OnGui()
    {
        if (KoiUpdaterUI.p.Length > 0)
            _setupWindowRect = GUI.Window(2398, _setupWindowRect, SetupWindowFunc,
                $"Calculating Hashes ({Math.Min(Math.Round(100f / KoiUpdaterUI.p.Length * KoiUpdaterUI.loadedPluginCount, 2), 100)}%)");
    }

    private void SetupWindowFunc(int id)
    {
        float progress = (float)KoiUpdaterUI.loadedPluginCount / KoiUpdaterUI.p.Length;
        var contentName = new GUIContent(new FileInfo(KoiUpdaterUI.currentFile).Name);
        GUI.Label(new Rect(_setupWindowRect.width / 2 - GUI.skin.label.CalcSize(contentName).x / 2, 30, 300, 20),
            contentName);
        GUI.DrawTexture(new Rect(50, 50, _setupWindowRect.width - 100, 20),
            Autumn.TextureFactory.Fill(KoiUpdaterUI.progressBar, Color.gray));
        GUI.DrawTexture(new Rect(50, 50, (_setupWindowRect.width - 100) * progress, 20),
            Autumn.TextureFactory.Fill(KoiUpdaterUI.progressDot, Color.green));
        var contentProgress = new GUIContent($"{KoiUpdaterUI.loadedPluginCount - 1}/{KoiUpdaterUI.p.Length}");

        GUI.Label(
            new Rect(_setupWindowRect.width / 2 - GUI.skin.label.CalcSize(contentProgress).x / 2, 70, 100, 20),
            contentProgress);
        if (KoiUpdaterUI.p.Length <= KoiUpdaterUI.loadedPluginCount)
            _finishSetup = true;

        GUI.DragWindow();
    }
}