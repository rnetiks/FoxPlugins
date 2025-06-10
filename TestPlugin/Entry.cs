#define KKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BepInEx;
using BepInEx.Logging;
using EasyWindow;
using HarmonyLib;
using TestPlugin.Windows;
using UnityEngine;
using Object = UnityEngine.Object;
using Screen = UnityEngine.Screen;

namespace TestPlugin
{
	/// <summary>
	/// Represents the entry point for plugin tests, never intended to be released to the public
	/// </summary>
	[BepInPlugin("org.fox.testplugin", "TestPlugin", "1.0.0")]
	public class Entry : BaseUnityPlugin
	{
		private Harmony _harmony;
		public new static ManualLogSource Logger;
		private void Awake()
		{
			Logger = base.Logger;
			_harmony = Harmony.CreateAndPatchAll(typeof(HybridIMGUIConverter.GUIWindowPatch));
			_harmony.PatchAll(typeof(HybridIMGUIConverter.GUIButtonPatch));
			_harmony.PatchAll(typeof(HybridIMGUIConverter.GUILabelPatch));
			_harmony.PatchAll(typeof(HybridIMGUIConverter.GUITextFieldPatch));
			_harmony.PatchAll(typeof(HybridIMGUIConverter.GUITogglePatch));
		}

		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}

		private string testString = "";

		Rect _windowRect = new Rect(200, 200, 300, 300);
		private void OnGUI()
		{
			GUI.Label(new Rect(100, 10, 100, 20), testString, new GUIStyle(GUI.skin.label));
			_windowRect = GUI.Window(29348, _windowRect, Func, "Empty Window");
		}

		private void Func(int id)
		{
			testString = GUI.TextField(new Rect(20, 70, 100, 20), testString);
			if (GUI.Button(new Rect(20, 20, 40, 40), "BT"))
			{
				Logger.LogDebug("Clicked");
			}
			if (_windowRect.Contains(Event.current.mousePosition))
				Input.ResetInputAxes();
			GUI.DragWindow();
		}
	}
}