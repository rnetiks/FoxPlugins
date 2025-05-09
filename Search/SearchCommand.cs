using System;
using UnityEngine.Assertions;

namespace Search.KKS
{
	public interface ISearchCommand : IEquatable<ISearchCommand>
	{
		/// <summary>
		/// The name of the command.
		/// </summary>
		string Name { get; }
		/// <summary>
		/// The description of the name, shows up last.
		/// </summary>
		string Description { get; }
		/// <summary>
		/// The method that will be executed upon a name press.
		/// </summary>
		void Execute();
	}

	public struct SearchCommand : ISearchCommand
	{
		public Action Callback { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public SearchCommand(string name, string description, Action callback)
		{
			Assert.IsNotNull(name, $"{nameof(name)} cannot be null!");
			Assert.IsNotNull(description, $"{nameof(description)} cannot be null!");
			Assert.IsNotNull(callback, $"{nameof(callback)} cannot be null!");

			Name = name;
			Description = description;
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
				var hashCode = (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Callback != null ? Callback.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}