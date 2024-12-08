using PrismaLib.Interfaces;

namespace PrismaLib.Settings
{
    public abstract class Setting<T> : ISetting
    {
        public readonly T DefaultValue;
        public readonly string Key;
        public T Value;

        public Setting(string key) : this(key, default)
        {
        }

        public Setting(string key, T defaultValue) : this(key, defaultValue, true)
        {
        }

        public Setting(string key, T defaultValue, bool addToPool)
        {
            if (addToPool)
            {
                if (Settings.Storage == null)
                {
                    Settings.CreateStorage(Settings.PreferredDefaultStorageSolution);
                }

                Settings.AddSetting(this);
            }
            Key = key;
            DefaultValue = defaultValue;
            Load();
        }

        ~Setting()
        {
            Save();
            Settings.RemoveSetting(this);
        }

        public abstract void Load();

        public abstract void Save();

        public override string ToString()
        {
            return Equals(Value, default(T)) ? "null" : Value.ToString();
        }

        public T ToValue()
        {
            return Value;
        }

        public static implicit operator T(Setting<T> set)
        {
            return set.Value;
        }
    }
}