using PrismaLib.Interfaces;
using UnityEngine;

namespace PrismaLib.Storage
{
    /// <summary>
    /// UnityEngine.PlayerPref storage
    /// </summary>
    public class PrefStorage : IDataStorage
    {
        public void Clear() => PlayerPrefs.DeleteAll();

        public bool GetBool(string key, bool def) => PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;

        public float GetFloat(string key, float def) => PlayerPrefs.GetFloat(key, def);

        public int GetInt(string key, int def) => PlayerPrefs.GetInt(key, def);

        public string GetString(string key, string def) => PlayerPrefs.GetString(key, def);

        public void Save() { }

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    }
}