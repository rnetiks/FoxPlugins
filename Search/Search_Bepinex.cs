using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using UnityEngine.Assertions;

namespace Search.KKS
{
	public partial class Search
	{
		private Harmony harmonyInstance;
		private static ConfigEntry<KeyboardShortcut> _fakePressing;

		private void BepinAwake()
		{
			try
			{
				harmonyInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
			}
			catch
			{
				OnDestroy();
				throw;
			}
		}
		private void OnDestroy()
		{
			harmonyInstance?.UnpatchSelf();
		}

		private struct BepInExCommand : ISearchCommand
		{
			public ConfigEntry<KeyboardShortcut> Setting { get; }
			public PluginInfo Owner { get; }
			public int FramesSinceHit { get; set; }

			public string Name => $"{Owner.Metadata.Name} :: {Setting.Definition.Key}";
			public string Description => Setting.GetSerializedValue();

			public BepInExCommand(ConfigEntry<KeyboardShortcut> setting)
			{
				Assert.IsNotNull(setting, $"{nameof(Setting)} cannot be null.");

				Setting = setting;
				Owner = Chainloader.PluginInfos.Values.FirstOrDefault(x => x.Instance != null && x.Instance.Config.ContainsKey(setting.Definition));
				FramesSinceHit = -1;
			}

			public void Execute()
			{
				_fakePressing = Setting;
			}

			public bool Equals(ISearchCommand other)
			{
				if (other is BepInExCommand hotkey)
				{
					return hotkey.Setting == Setting;
				}

				return false;
			}
		}

		/// <summary>
		/// This class handles Bepinex/Config detection of <see cref="KeyboardShortcut"/>> and simulates fake presses.
		/// </summary>
		private static class Hooks
		{
			/// <summary>
			/// We assume that every time the keyboard shortcut is being polled, that it's being watched, and we record the time since the last get. This is useful to see if a shortcut is currently viable as many times shortcuts can be contextual.
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(ConfigEntry<KeyboardShortcut>), nameof(ConfigEntry<KeyboardShortcut>.Value), MethodType.Getter)]
			private static void HotkeyValueGetter(ConfigEntry<KeyboardShortcut> __instance, ref KeyboardShortcut __result)
			{
				if (!Instance.commands.TryGetValue(__instance, out var info))
				{
					info = new BepInExCommand(__instance);
					Instance.AddCommand(info);
				}

				if (info is BepInExCommand hotInfo)
				{
					hotInfo.FramesSinceHit = 0;
				}
			}

			/// <summary>
			/// Simulates fake presses.
			/// </summary>
			[HarmonyPrefix]
			[HarmonyPatch(typeof(KeyboardShortcut), nameof(KeyboardShortcut.IsDown))]
			[HarmonyPatch(typeof(KeyboardShortcut), nameof(KeyboardShortcut.IsPressed))]
			[HarmonyPatch(typeof(KeyboardShortcut), nameof(KeyboardShortcut.IsUp))]
			private static bool KeyboardShortcutPressOverride(KeyboardShortcut __instance, ref bool __result)
			{
				if (_fakePressing == null || _fakePressing.Value.Equals(__instance) == false)
				{
					return true;
				}

				_fakePressing = null;
				__result = true;
				return false;
			}
		}
	}
}