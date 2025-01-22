using PrismaLib;
using PrismaLib.Interfaces;
using UnityEngine;

namespace Autumn.Storage
{
    /// <summary>
    /// Simple storage using Config files
    /// </summary>
    public class AutumnStorage : ConfigFile, IDataStorage
    {
        public AutumnStorage() : base(Application.dataPath + "/Configuration/Settings.cfg", '`', true)
        {
            Load();
        }

        public AutumnStorage(string path, char seperator = '`', bool autocreate = true) : base(path, seperator, autocreate)
        {
        }

        public void Clear()
        {
            Delete();
            Create();
        }

        public bool GetBool(string key, bool def)
        {
            if (booleans.TryGetValue(key, out var result))
                return result;
            if (allValues.TryGetValue(key, out var val))
            {
                if (!bool.TryParse(val, out result)) return def;

                booleans.Add(key, result);
                return result;
            }

            SetBool(key, def);
            return def;
        }

        public float GetFloat(string key, float def)
        {
            if (floats.TryGetValue(key, out var result))
                return result;
            if (allValues.TryGetValue(key, out var val))
            {
                if (!float.TryParse(val, out result)) return def;

                floats.Add(key, result);
                return result;
            }

            SetFloat(key, def);
            return def;
        }

        public int GetInt(string key, int def)
        {
            if (integers.TryGetValue(key, out var result))
                return result;
            if (allValues.TryGetValue(key, out var val))
            {
                if (!int.TryParse(val, out result)) return def;
                integers.Add(key, result);
                return result;
            }

            SetInt(key, def);
            return def;
        }

        public string GetString(string key, string def)
        {
            if (strings.TryGetValue(key, out var result))
                return result;
            if (allValues.TryGetValue(key, out result))
            {
                strings.Add(key, result);
                return result;
            }

            SetString(key, def);
            return def;
        }
    }
}