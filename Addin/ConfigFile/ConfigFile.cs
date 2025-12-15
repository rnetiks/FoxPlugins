using System;
using System.Reflection;
using BepInEx.Configuration;

namespace Addin
{
	public static class ConfigFile
	{
		private static readonly FieldInfo _ioLockField;
		private static readonly PropertyInfo _entriesProperty;
		private static readonly PropertyInfo _orphanedEntriesProperty;

		static ConfigFile()
		{
			var configFileType = typeof(BepInEx.Configuration.ConfigFile);
			_ioLockField = configFileType.GetField("_ioLock", BindingFlags.NonPublic | BindingFlags.Instance);
			_entriesProperty = configFileType.GetProperty("Entries", BindingFlags.NonPublic | BindingFlags.Instance);
			_orphanedEntriesProperty = configFileType.GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		/// <summary>
		/// Removes a config entry from the configuration file
		/// </summary>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="section">Section name</param>
		/// <param name="key">Key name</param>
		/// <returns>True if the entry was removed, false if it didn't exist</returns>
		public static bool Unbind(this BepInEx.Configuration.ConfigFile configFile, string section, string key)
		{
			return configFile.Unbind(new ConfigDefinition(section, key));
		}

		/// <summary>
		/// Removes a config entry from the configuration file
		/// </summary>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="configDefinition">Config definition to remove</param>
		/// <returns>True if the entry was removed, false if it didn't exist</returns>
		public static bool Unbind(this BepInEx.Configuration.ConfigFile configFile, ConfigDefinition configDefinition, bool keepOrphanedEntries = true)
		{
			if (configFile == null) throw new ArgumentNullException(nameof(configFile));
			if (configDefinition == null) throw new ArgumentNullException(nameof(configDefinition));

			var ioLock = _ioLockField.GetValue(configFile);
			var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
			var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;

			lock (ioLock)
			{
				bool removed = false;
				if (keepOrphanedEntries)
				{
					if (entries.TryGetValue(configDefinition, out ConfigEntryBase entry))
					{
						orphanedEntries[configDefinition] = entry.GetSerializedValue();
						removed = entries.Remove(configDefinition);
					}
				}
				else
				{
					bool removedFromEntries = entries.Remove(configDefinition);
					bool removedFromOrphaned = orphanedEntries.Remove(configDefinition);
					removed = removedFromEntries || removedFromOrphaned;
				}
				
				if (removed && configFile.SaveOnConfigSet)
				{
					configFile.Save();
				}
				
				return removed;
			}
		}

		/// <summary>
		/// Removes a config entry and returns the removed entry
		/// </summary>
		/// <typeparam name="T">Type of the config entry</typeparam>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="section">Section name</param>
		/// <param name="key">Key name</param>
		/// <returns>The removed ConfigEntry&lt;T&gt; or null if not found</returns>
		public static ConfigEntry<T> Unbind<T>(this BepInEx.Configuration.ConfigFile configFile, string section, string key)
		{
			return configFile.Unbind<T>(new ConfigDefinition(section, key));
		}

		/// <summary>
		/// Removes a config entry and returns the removed entry
		/// </summary>
		/// <typeparam name="T">Type of the config entry</typeparam>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="configDefinition">Config definition to remove</param>
		/// <returns>The removed ConfigEntry&lt;T&gt; or null if not found</returns>
		public static ConfigEntry<T> Unbind<T>(this BepInEx.Configuration.ConfigFile configFile, ConfigDefinition configDefinition)
		{
			if (configFile == null) throw new ArgumentNullException(nameof(configFile));
			if (configDefinition == null) throw new ArgumentNullException(nameof(configDefinition));

			var ioLock = _ioLockField.GetValue(configFile);
			var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
			var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;

			lock (ioLock)
			{
				ConfigEntry<T> removedEntry = null;
				bool removed = false;

				if (entries.TryGetValue(configDefinition, out ConfigEntryBase entry))
				{
					entries.Remove(configDefinition);
					removedEntry = entry as ConfigEntry<T>;
					removed = true;
				}

				if (orphanedEntries.Remove(configDefinition))
				{
					removed = true;
				}

				if (removed && configFile.SaveOnConfigSet)
				{
					configFile.Save();
				}

				return removedEntry;
			}
		}

		/// <summary>
		/// Removes a specific config entry instance
		/// </summary>
		/// <typeparam name="T">Type of the config entry</typeparam>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="configEntry">The config entry to remove</param>
		/// <returns>True if the entry was removed, false if it didn't exist</returns>
		public static bool Unbind<T>(this BepInEx.Configuration.ConfigFile configFile, ConfigEntry<T> configEntry)
		{
			if (configEntry?.Definition == null) return false;
			return configFile.Unbind(configEntry.Definition);
		}

		/// <summary>
		/// Removes all config entries from a specific section
		/// </summary>
		/// <param name="configFile">The config file to modify</param>
		/// <param name="section">Section name to clear</param>
		/// <returns>Number of entries removed</returns>
		public static int UnbindSection(this BepInEx.Configuration.ConfigFile configFile, string section, bool deleteOrphanedEntries = true)
		{
			if (configFile == null) throw new ArgumentNullException(nameof(configFile));
			if (string.IsNullOrEmpty(section)) throw new ArgumentException("Section cannot be null or empty", nameof(section));

			var ioLock = _ioLockField.GetValue(configFile);
			var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
			var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;

			lock (ioLock)
			{
				int removedCount = 0;

				foreach (var kvp in entries)
				{
					if (kvp.Key.Section == section)
					{
						if (entries.Remove(kvp.Key))
							removedCount++;
					}
				}

				if (deleteOrphanedEntries)
				{
					foreach (var kvp in orphanedEntries)
					{
						if (kvp.Key.Section == section)
						{
							if (orphanedEntries.Remove(kvp.Key))
								removedCount++;
						}
					}
				}

				if (removedCount > 0 && configFile.SaveOnConfigSet)
				{
					configFile.Save();
				}

				return removedCount;
			}
		}

		/// <summary>
		/// Checks if a config entry exists (including orphaned entries)
		/// </summary>
		/// <param name="configFile">The config file to check</param>
		/// <param name="section">Section name</param>
		/// <param name="key">Key name</param>
		/// <returns>True if the entry exists</returns>
		public static bool HasEntry(this BepInEx.Configuration.ConfigFile configFile, string section, string key)
		{
			return configFile.HasEntry(new ConfigDefinition(section, key));
		}

		/// <summary>
		/// Checks if a config entry exists (including orphaned entries)
		/// </summary>
		/// <param name="configFile">The config file to check</param>
		/// <param name="configDefinition">Config definition to check</param>
		/// <returns>True if the entry exists</returns>
		public static bool HasEntry(this BepInEx.Configuration.ConfigFile configFile, ConfigDefinition configDefinition)
		{
			if (configFile == null) throw new ArgumentNullException(nameof(configFile));
			if (configDefinition == null) throw new ArgumentNullException(nameof(configDefinition));

			var ioLock = _ioLockField.GetValue(configFile);
			var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
			var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;

			lock (ioLock)
			{
				return entries.ContainsKey(configDefinition) || orphanedEntries.ContainsKey(configDefinition);
			}
		}
	}
}