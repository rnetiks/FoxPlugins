using BepInEx.Configuration;
using HarmonyLib;

namespace Search.KKS
{
	public partial class Search
	{
		/// <summary>
		/// This class handles Bepinex/Config detection of <see cref="BepInEx.Configuration.KeyboardShortcut"/>> and simulates fake presses.
		/// </summary>
		private static class Hooks
		{
			/// <summary>
			/// We assume that every time the keyboard shortcut is being polled, that it's being watched, and we record the time since the last get. This is useful to see if a shortcut is currently viable as many times shortcuts can be contextual.
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(ConfigEntry<KeyboardShortcut>), nameof(ConfigEntry<KeyboardShortcut>.Value),
				MethodType.Getter)]
			private static void HotkeyValueGetter(ConfigEntry<KeyboardShortcut> __instance,
				ref KeyboardShortcut __result)
			{
				var cmd = new BepInExCommand(__instance);
				var hash = cmd.GetHashCode();
				if (Instance.commands.TryGetValue(hash, out var info))
				{
					var command = (BepInExCommand)info;
					command.FramesSinceHit++;
					Instance.commands[hash] = command;
				}
				else
				{
					if (cmd.Owner != null)
						Instance.AddCommand(cmd);
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
				if (_fakePressing?.Value.Equals(__instance) != true)
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