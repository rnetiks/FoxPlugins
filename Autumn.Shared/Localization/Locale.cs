using System;
using System.Collections.Generic;
using System.Threading;
using PrismaLib;
using UnityEngine;
using File = System.IO.File;

namespace Autumn.Localization
{
    public class Locale
    {
        public const string Extension = ".lang";

        private bool custom;
        public readonly string Element;
        public readonly string MyLanguage;
        private Dictionary<string, string> localizedText = new Dictionary<string, string>();
        private readonly Dictionary<string, string[]> localizedTextArrayCache = new Dictionary<string, string[]>();
        private readonly object locker = new object();
        private bool notAllowClose;
        public readonly char Separator;

        public bool AlwaysOpen { get; set; } = false;
        public bool Formatable { get; set; }
        public bool IsOpen { get; private set; }
        public string Path { get; private set; }

        public Locale(string element, bool format = false, char separator = ',')
        {
            Formatable = format;
            Separator = separator;
            Element = element;
            Path = Language.Directory + Element + Extension;
            Load();
            MyLanguage = Language.SelectedLanguage;
            Language.AddLocale(this);
        }

        public Locale(string lang, string element, bool format, char separator)
        {
            Formatable = format;
            Separator = separator;
            Element = element;
            MyLanguage = lang;
            Path = Language.Path + lang + "/" + Element + Extension;
            Load();
            custom = true;
            Language.AddLocale(this);
        }

        ~Locale()
        {
            Unload();
            Language.RemoveLocale(this);
        }

        public string Format(string key, params string[] values)
        {
            return string.Format(this[key], values);
        }

        public string Format(string key, string str)
        {
            return string.Format(this[key], str);
        }

        public string Get(string key)
        {
            return this[key];
        }

        public string[] GetArray(string key)
        {
            if (localizedTextArrayCache.TryGetValue(key, out var result))
            {
                return result;
            }
            try
            {
                result = localizedText[key].Split(Separator);
                localizedTextArrayCache.Add(key, result);
            }
            catch
            {
                Debug.Log("Invalid key: " + key);
                throw;
            }
            return result;
        }

        public void KeepOpen(int seconds)
        {
            if (!AlwaysOpen)
            {
                new Thread(() =>
                {
                    if (!IsOpen)
                    {
                        Load();
                    }
                    notAllowClose = true;
                    Thread.Sleep(seconds * 1000);
                    notAllowClose = false;
                    Unload();
                })
                { IsBackground = true }.Start();
            }
        }

        public void Load()
        {
            if (!File.Exists(Path))
            {
                return;
            }
            lock (locker)
            {
                localizedText.Clear();
                localizedTextArrayCache.Clear();
                IsOpen = false;
                using (ConfigFile config = new ConfigFile(Path, ':', false))
                {
                    config.AutoSave = false;
                    config.Load();
                    foreach (KeyValuePair<string, string> pair in config.AllValues)
                    {
                        localizedText.Add(pair.Key, pair.Value.Replace(@"\n", Environment.NewLine));
                    }
                }
                IsOpen = true;
            }
        }

        public void Reload()
        {
            if (!custom)
            {
                Path = Language.Directory + Element + Extension;
            }
            Load();
        }

        public void Unload()
        {
            if (AlwaysOpen || notAllowClose)
            {
                return;
            }
            lock (locker)
            {
                localizedText.Clear();
                localizedTextArrayCache.Clear();
                IsOpen = false;
            }
        }

        public string this[string key] => localizedText.TryGetValue(key, out var item) ? item : $"?{key}?";
    }
}