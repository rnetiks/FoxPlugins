using System;
using System.Reflection;
using BepInEx.Configuration;

namespace Crystalize
{
    public static class ConfigFileExtensions
    {
        private static readonly FieldInfo _ioLockField;
        private static readonly PropertyInfo _entriesProperty;
        private static readonly PropertyInfo _orphanedEntriesProperty;
        
        static ConfigFileExtensions()
        {
            var configFileType = typeof(ConfigFile);
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
        public static bool Unbind(this ConfigFile configFile, string section, string key)
        {
            return configFile.Unbind(new ConfigDefinition(section, key));
        }
        
        /// <summary>
        /// Removes a config entry from the configuration file
        /// </summary>
        /// <param name="configFile">The config file to modify</param>
        /// <param name="configDefinition">Config definition to remove</param>
        /// <returns>True if the entry was removed, false if it didn't exist</returns>
        public static bool Unbind(this ConfigFile configFile, ConfigDefinition configDefinition)
        {
            if (configFile == null) throw new ArgumentNullException(nameof(configFile));
            if (configDefinition == null) throw new ArgumentNullException(nameof(configDefinition));
            
            var ioLock = _ioLockField.GetValue(configFile);
            var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
            var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;
            
            lock (ioLock)
            {
                bool removedFromEntries = entries.Remove(configDefinition);
                bool removedFromOrphaned = orphanedEntries.Remove(configDefinition);
                bool removed = removedFromEntries || removedFromOrphaned;
                
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
        public static ConfigEntry<T> Unbind<T>(this ConfigFile configFile, string section, string key)
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
        public static ConfigEntry<T> Unbind<T>(this ConfigFile configFile, ConfigDefinition configDefinition)
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
        public static bool Unbind<T>(this ConfigFile configFile, ConfigEntry<T> configEntry)
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
        public static int UnbindSection(this ConfigFile configFile, string section)
        {
            if (configFile == null) throw new ArgumentNullException(nameof(configFile));
            if (string.IsNullOrEmpty(section)) throw new ArgumentException("Section cannot be null or empty", nameof(section));
            
            var ioLock = _ioLockField.GetValue(configFile);
            var entries = _entriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, ConfigEntryBase>;
            var orphanedEntries = _orphanedEntriesProperty.GetValue(configFile, null) as System.Collections.Generic.Dictionary<ConfigDefinition, string>;
            
            lock (ioLock)
            {
                int removedCount = 0;
                
                var keysToRemove = new System.Collections.Generic.List<ConfigDefinition>();
                foreach (var kvp in entries)
                {
                    if (kvp.Key.Section == section)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    if (entries.Remove(key))
                        removedCount++;
                }
                
                keysToRemove.Clear();
                foreach (var kvp in orphanedEntries)
                {
                    if (kvp.Key.Section == section)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    if (orphanedEntries.Remove(key))
                        removedCount++;
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
        public static bool HasEntry(this ConfigFile configFile, string section, string key)
        {
            return configFile.HasEntry(new ConfigDefinition(section, key));
        }
        
        /// <summary>
        /// Checks if a config entry exists (including orphaned entries)
        /// </summary>
        /// <param name="configFile">The config file to check</param>
        /// <param name="configDefinition">Config definition to check</param>
        /// <returns>True if the entry exists</returns>
        public static bool HasEntry(this ConfigFile configFile, ConfigDefinition configDefinition)
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