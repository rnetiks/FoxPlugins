using System;

namespace Search.KKS
{
    public struct SearchCommand : IEquatable<SearchCommand>
    {
        public string command;
        public string description;
        public Action callback;

        public bool Equals(SearchCommand other)
        {
            return command == other.command && description == other.description && Equals(callback, other.callback);
        }

        public override bool Equals(object obj)
        {
            return obj is SearchCommand other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (command != null ? command.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (description != null ? description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (callback != null ? callback.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

}