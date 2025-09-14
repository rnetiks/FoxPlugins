using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;

namespace Crystalize
{
    public class Bepin
    {
        /// Retrieves the instance of the plugin that is calling this method.
        /// This method examines the stack trace to identify the type in the calling assembly
        /// that is decorated with the BepInPlugin attribute. If such a type is located
        /// and it is assignable to BaseUnityPlugin, the method attempts to find and
        /// return an instance of that plugin. If no valid plugin type or instance is found, null is returned.
        /// <returns>
        /// An instance of the calling plugin as a BaseUnityPlugin object if found, or null if no valid plugin instance is identified.
        /// </returns>
        public static List<BaseUnityPlugin> GetCallingPlugins()
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            List<BaseUnityPlugin> plugins = new List<BaseUnityPlugin>();

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                var declaringType = method?.DeclaringType;

                if (declaringType != null)
                {
                    var pluginAttribute = Attribute.GetCustomAttribute(declaringType, typeof(BepInPlugin)) as BepInPlugin;
                    if (pluginAttribute != null && typeof(BaseUnityPlugin).IsAssignableFrom(declaringType))
                    {
                        plugins.Add(FindPluginInstance(declaringType));
                    }

                    var assembly = declaringType.Assembly;
                    var pluginType = assembly.GetTypes()
                        .FirstOrDefault(t => Attribute.GetCustomAttribute(t, typeof(BepInPlugin)) != null &&
                                             typeof(BaseUnityPlugin).IsAssignableFrom(t)
                                             );

                    if (pluginType != null)
                    {
                        plugins.Add(FindPluginInstance(pluginType));
                    }
                }
            }

            return plugins;
        }

        /// Retrieves detailed information about the plugin assembly that is calling this method.
        /// This method analyzes the stack trace to identify the plugin type in the calling assembly
        /// that is decorated with the BepInPlugin attribute. If such a type is found, the method extracts
        /// the plugin's GUID, name, version, type, and assembly information, and returns it as a PluginInfo object.
        /// If no compatible plugin type is found in the calling assembly, null is returned.
        /// <returns>
        /// A PluginInfo object containing the GUID, name, version, type, and assembly details of the calling plugin,
        /// or null if no valid plugin assembly is found.
        /// </returns>
        public static PluginInfo GetCallingPluginInfo()
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                var declaringType = method?.DeclaringType;

                if (declaringType != null)
                {
                    var pluginAttribute = Attribute.GetCustomAttribute(declaringType, typeof(BepInPlugin)) as BepInPlugin;
                    if (pluginAttribute != null)
                    {
                        return new PluginInfo
                        {
                            GUID = pluginAttribute.GUID,
                            Name = pluginAttribute.Name,
                            Version = pluginAttribute.Version,
                            PluginType = declaringType,
                            Assembly = declaringType.Assembly
                        };
                    }

                    var assembly = declaringType.Assembly;
                    var pluginType = assembly.GetTypes()
                        .FirstOrDefault(t => Attribute.GetCustomAttribute(t, typeof(BepInPlugin)) != null);

                    if (pluginType != null)
                    {
                        var attr = Attribute.GetCustomAttribute(pluginType, typeof(BepInPlugin)) as BepInPlugin;
                        return new PluginInfo
                        {
                            GUID = attr.GUID,
                            Name = attr.Name,
                            Version = attr.Version,
                            PluginType = pluginType,
                            Assembly = assembly
                        };
                    }
                }
            }

            return null;
        }
        
        private static BaseUnityPlugin FindPluginInstance(Type pluginType)
        {
            var chainloader = BepInEx.Bootstrap.Chainloader.ManagerObject;
            if (chainloader != null)
            {
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                var pluginAttribute = Attribute.GetCustomAttribute(pluginType, typeof(BepInPlugin)) as BepInPlugin;

                if (pluginAttribute != null && pluginInfos.TryGetValue(pluginAttribute.GUID, out var pluginInfo))
                {
                    return pluginInfo.Instance;
                }
            }

            return null;
        }

        public class PluginInfo
        {
            public string GUID { get; set; }
            public string Name { get; set; }
            public Version Version { get; set; }
            public Type PluginType { get; set; }
            public Assembly Assembly { get; set; }
    
            public override string ToString()
            {
                return $"{Name} ({GUID}) v{Version}";
            }
        }

    }
    
}