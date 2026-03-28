using System;
using UnityEngine.Assertions;

namespace Search
{
    public interface ISearchCommand : IEquatable<ISearchCommand>
    {
        string Name { get; }
        string Description { get; }
        string Category { get; }
        void Execute();
    }

    public struct SearchCommand : ISearchCommand
    {
        public Action Callback { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public SearchCommand(string name, string description, Action callback)
            : this(name, description, string.Empty, callback)
        {
        }

        public SearchCommand(string name, string description, string category, Action callback)
        {
            Assert.IsNotNull(name, $"{nameof(name)} cannot be null!");
            Assert.IsNotNull(description, $"{nameof(description)} cannot be null!");
            Assert.IsNotNull(callback, $"{nameof(callback)} cannot be null!");

            Name = name;
            Description = description;
            Category = category ?? string.Empty;
            Callback = callback;
        }

        public void Execute()
        {
            Callback?.Invoke();
        }

        public bool Equals(ISearchCommand other)
        {
            return other is SearchCommand searchCommand &&
                   other.Name.Equals(Name) &&
                   other.Description.Equals(Description) &&
                   Callback == searchCommand.Callback;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Name != null ? Name.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (Callback != null ? Callback.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}