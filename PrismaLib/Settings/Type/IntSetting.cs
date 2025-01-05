using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PrismaLib.Settings.Type
{
    public class IntSetting : Setting<int>
    {
        public IntSetting(string key) : base(key)
        {
        }

        public IntSetting(string key, int defaultValue) : base(key, defaultValue)
        {
        }

        public IntSetting(string key, int defaultValue, bool addToPool) : base(key, defaultValue, addToPool)
        {
        }

        public override void Load()
        {
            Value = Settings.Storage.GetInt(Key, DefaultValue);
        }

        public override void Save() => Settings.Storage.SetInt(Key, Value);
    }
}