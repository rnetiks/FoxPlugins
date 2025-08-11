using System;
using System.Collections.Generic;

namespace Prototype
{
    public class DataBinder<T> where T : class
    {
        private T _data;
        private readonly Dictionary<string, List<Action>> _subscribers = new Dictionary<string, List<Action>>();
        private readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

        public T Data
        {
            get => _data;
            set
            {
                _data = value;
                NotifyAllChanged();
            }
        }

        public void Bind(string property, Action callback)
        {
            if (!_subscribers.ContainsKey(property))
                _subscribers[property] = new List<Action>();

            _subscribers[property].Add(callback);
        }

        public void Unbind(string property, Action callback)
        {
            if (_subscribers.ContainsKey(property))
                _subscribers[property].Remove(callback);
        }

        public void NotifyChanged(string property)
        {
            if (_subscribers.TryGetValue(property, out var subscriber))
            {
                foreach (var callback in subscriber)
                    callback?.Invoke();
            }
        }

        /// <summary>
        /// Notifies all subscribers of changes for any bound properties.
        /// Invokes all registered callbacks in the subscribers list.
        /// </summary>
        public void NotifyAllChanged()
        {
            foreach (var subscribers in _subscribers.Values)
            {
                foreach (var callback in subscribers)
                    callback?.Invoke();
            }
        }

        /// <summary>
        /// Sets the value of a specified property and notifies any subscribers of changes if the value is updated.
        /// </summary>
        /// <typeparam name="TValue">The type of the property value.</typeparam>
        /// <param name="property">The name of the property to set.</param>
        /// <param name="value">The new value to assign to the property.</param>
        public void SetProperty<TValue>(string property, TValue value)
        {
            if (!_propertyValues.ContainsKey(property) || !_propertyValues[property].Equals(value))
            {
                _propertyValues[property] = value;
                NotifyChanged(property);
            }
        }

        public TValue GetProperty<TValue>(string property, TValue defaultValue = default)
        {
            return _propertyValues.ContainsKey(property) ? (TValue)_propertyValues[property] : defaultValue;
        }
    }
}