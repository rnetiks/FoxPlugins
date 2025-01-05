namespace PrismaLib.Settings.Type
{
    public class StringSetting : Setting<string>
    {
        public StringSetting(string key) : base(key)
        {
        }

        public StringSetting(string key, string defaultValue) : base(key, defaultValue)
        {
        }

        public StringSetting(string key, string defaultValue, bool addToPool) : base(key, defaultValue, addToPool)
        {
        }

        public override void Load() => Value = Settings.Storage.GetString(Key, DefaultValue);

        public override void Save() => Settings.Storage.SetString(Key, Value);
    }
}