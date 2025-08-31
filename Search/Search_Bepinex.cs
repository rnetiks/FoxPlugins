using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using UnityEngine.Assertions;

namespace Search
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
				FramesSinceHit = 0;
				_fakePressing = Setting;
				Instance.commands[GetHashCode()] = this;
			}

			public bool Equals(ISearchCommand other)
			{
				if (other is BepInExCommand hotkey)
				{
					return hotkey.Setting == Setting;
				}

				return false;
			}

			public override int GetHashCode()
			{
				return Setting.GetHashCode();
			}
		}
	}
}