using System;
using System.Collections.Generic;

namespace Crystalize
{
    public class DataBinder
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly Dictionary<string, List<Action<object>>> _subscribers = new Dictionary<string, List<Action<object>>>();

        public void Bind<T>(string property, Action<T> callback)
        {
            if (!_subscribers.ContainsKey(property))
                _subscribers[property] = new List<Action<object>>();
        
            _subscribers[property].Add(value => callback((T)value));
        }

        public void SetProperty<T>(string property, T value)
        {
            var oldValue = GetProperty<T>(property);
            if (!EqualityComparer<T>.Default.Equals(oldValue, value))
            {
                _properties[property] = value;
                NotifyChanged(property, value);
            }
        }

        public T GetProperty<T>(string property, T defaultValue = default)
        {
            return _properties.TryGetValue(property, out var value) ? (T)value : defaultValue;
        }

        private void NotifyChanged(string property, object value)
        {
            if (_subscribers.TryGetValue(property, out var callbacks))
            {
                foreach (var callback in callbacks)
                    callback?.Invoke(value);
            }
        }
    }
}