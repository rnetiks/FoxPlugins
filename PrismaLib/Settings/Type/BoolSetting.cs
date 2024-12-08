using System;

namespace PrismaLib.Settings
{
    public class BoolSetting : Setting<Boolean>
    {
        public BoolSetting(string key) : base(key)
        {
        }

        public BoolSetting(string key, bool defaultValue) : base(key, defaultValue)
        {
        }

        public BoolSetting(string key, bool defaultValue, bool addToPool) : base(key, defaultValue, addToPool)
        {
        }

        public override void Load()
        {
            Value = Settings.Storage.GetBool(Key, DefaultValue);
        }

        public override void Save() => Settings.Storage.SetBool(Key, Value);
    }
}