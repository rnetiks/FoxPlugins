using System;
using System.Collections.Generic;
using PrismaLib.Enums;
using PrismaLib.Interfaces;

namespace PrismaLib.Settings
{
    public class Settings
    {
        private static List<ISetting> allSettings;
        private static object locker = new object();
        public static IDataStorage Storage;
        public static DefaultStorageType PreferredDefaultStorageSolution { get; set; } = DefaultStorageType.Autumn;

        public static void AddSetting(ISetting set)
        {
            lock (locker)
            {
                allSettings = allSettings ?? new List<ISetting>();
                allSettings.Add(set);
            }
        }

        public static void Clear()
        {
            if (Storage == null)
            {
                CreateStorage(PreferredDefaultStorageSolution);
            }

            Storage?.Clear();
        }

        public static void CreateStorage(DefaultStorageType type)
        {
            switch (type)
            {
                // Add more here
                case DefaultStorageType.Internal:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void Load()
        {
            if (allSettings == null)
            {
                lock (locker)
                {
                    allSettings = allSettings ?? new List<ISetting>();
                }
            }

            if (Storage == null)
            {
                CreateStorage(PreferredDefaultStorageSolution);
            }

            lock (locker)
            {
                foreach (var setting in allSettings)
                {
                    setting?.Load();
                }
            }
        }

        public static void RemoveSetting(ISetting set)
        {
            lock (locker)
            {
                if (allSettings.Contains(set))
                {
                    allSettings.Remove(set);
                }
            }
        }

        public static void Save()
        {
            allSettings = allSettings ?? new List<ISetting>();

            lock (locker)
            {
                foreach (var setting in allSettings)
                {
                    setting.Save();
                }
            }
            Storage.Save();
        }
    }
}