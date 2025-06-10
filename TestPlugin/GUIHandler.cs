using System;
using System.IO;
using System.Linq;

namespace EasyWindow
{
	/// <summary>
	/// Handles the management of GUIDrawable objects, including rendering, addition, and removal of these objects from the internal collection.
	/// Supports lifecycle management through invoking OnEnable and OnDisable methods on the managed GUIDrawable objects.
	/// Ensures type uniqueness when adding new GUIDrawable objects, if specified.
	/// </summary>
	public class GUIHandler
	{
		/// <summary>
		/// Gets the collection of GUIDrawable objects managed by this GUIHandler.
		/// These objects can be drawn, added, removed, and processed for their lifecycle behaviors such as enabling or disabling.
		/// </summary>
		/// <remarks>
		/// If a GUIDrawable object has its ShouldKill property set to true, it will be removed from this collection
		/// during the rendering process.
		/// </remarks>
		public GUIDrawable[] Drawables { get; private set; }

		/// <summary>
		/// Iterates through all GUIDrawable objects in the Drawables collection and invokes their Draw method.
		/// Removes any GUIDrawable object from the collection if its ShouldKill property is set to true after drawing.
		/// </summary>
		/// <remarks>
		/// This method ensures that all valid GUIDrawable objects in the collection are drawn and processes those marked for removal.
		/// </remarks>
		public void DrawAll()
		{
			foreach (var guiDrawable in Drawables)
			{
				if (guiDrawable.Enabled)
					guiDrawable.Draw();
				if (guiDrawable.ShouldKill)
				{
					guiDrawable.OnDestroy();
					Remove(guiDrawable);
				}
			}
		}

		/// <summary>
		/// Checks if the internal collection of GUIDrawable objects contains an object of the specified type.
		/// </summary>
		/// <param name="type">The type to search for in the collection.</param>
		/// <returns>True if an object of the specified type exists in the collection; otherwise, false.</returns>
		public bool Contains(Type type)
		{
			return Drawables.Any(e => e.GetType() == type);
		}

		/// <summary>
		/// Invokes the Draw method for the specified drawable at the given index in the Drawables collection.
		/// If the drawable's ShouldKill property is set to true, it will be removed from the collection.
		/// </summary>
		/// <param name="index">The index of the drawable in the Drawables collection to be drawn.</param>
		/// <remarks>
		/// If the specified index is out of range, the method does nothing and returns immediately.
		/// </remarks>
		public void Draw(int index)
		{
			if (Drawables.Length <= index)
				return;
			var iguiDrawable = Drawables[index];
			if (iguiDrawable.Enabled)
			{
				iguiDrawable.Draw();
			}

			if (iguiDrawable.ShouldKill)
			{
				iguiDrawable.OnDestroy();
				Remove(iguiDrawable);
			}
		}

		/// <summary>
		/// Manages a collection of GUIDrawable objects, allowing handling of their rendering, addition, and removal from the collection.
		/// Provides lifecycle hooks for added or removed GUIDrawable objects and ensures type uniqueness if specified.
		/// </summary>
		public GUIHandler(GUIDrawable[] drawables)
		{
			Drawables = drawables;
		}

		/// <summary>
		/// Manages a collection of GUIDrawable objects, allowing handling of their rendering, addition,
		/// and removal from the collection. Provides lifecycle hooks for added or removed GUIDrawable
		/// objects and ensures type uniqueness if specified.
		/// </summary>
		public GUIHandler()
		{
			Drawables = Array.Empty<GUIDrawable>();
		}

		/// <summary>
		/// Adds a GUIDrawable object to the Drawables collection, enabling it for rendering and lifecycle management.
		/// Throws an exception if the drawable is marked as unique and an instance of the same type already exists in the collection.
		/// </summary>
		/// <param name="drawable">The GUIDrawable object to be added to the Drawables collection.</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown when attempting to add a unique GUIDrawable of a type that is already present in the collection.
		/// </exception>
		public void Add(GUIDrawable drawable)
		{
			if (Drawables.Any(x => x.GetType() == drawable.GetType() && (x.Unique || drawable.Unique)))
			{
				UnityEngine.Debug.Log("An GUIDrawable of this type already exists.");
				return;
			}
			
			drawable.OnInitialize();
			Drawables = Drawables.Concat(new[] { drawable }).ToArray();
		}

		/// <summary>
		/// Adds the specified GUIDrawable object to the internal collection of drawables.
		/// Ensures the added object is correctly initialized and included in the collection.
		/// </summary>
		/// <param name="drawable">The GUIDrawable object to be added to the collection.</param>
		public void Add(Type type)
		{
			if (!typeof(GUIDrawable).IsAssignableFrom(type))
			{
				UnityEngine.Debug.LogError("The provided type must inherit from GUIDrawable.");
				return;
			}

			var drawable = (GUIDrawable)Activator.CreateInstance(type);
			Add(drawable);
		}

		/// <summary>
		/// Adds a new GUIDrawable object to the Drawables collection.
		/// If the added drawable's type is marked as unique and already exists in the collection, the addition is skipped.
		/// Invokes the OnInitialize method of the drawable before adding it.
		/// </summary>
		/// <param name="drawable">The GUIDrawable object to be added to the collection.</param>
		public void Add<T>(bool unique = true) where T : GUIDrawable, new()
		{
			var drawable = new T { Unique = unique };
			Add(drawable);
		}

		/// <summary>
		/// Removes the specified drawable from the Drawables collection and invokes its OnDisable lifecycle method.
		/// </summary>
		/// <param name="drawable">The GUIDrawable object to be removed from the Drawables collection.</param>
		/// <remarks>
		/// If the specified drawable is not present in the collection, the method does nothing.
		/// The OnDisable method of the drawable will be triggered before its removal.
		/// </remarks>
		public void Remove(GUIDrawable drawable)
		{
			drawable.OnDestroy();
			Drawables = Drawables.Where(x => x != drawable).ToArray();
		}

		/// <summary>
		/// Removes the first GUIDrawable object from the collection that matches the specified type.
		/// Invokes the OnDisable method on the matching object before removing it.
		/// </summary>
		/// <param name="type">The type of the GUIDrawable object to remove.</param>
		/// <returns>
		/// True if a GUIDrawable object of the specified type was found and removed; otherwise, false.
		/// </returns>
		public bool Remove(Type type)
		{
			foreach (var guiDrawable in Drawables.Where(e => e.GetType() == type))
			{
				Remove(guiDrawable);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes all GUIDrawable objects from the internal collection that match the specified array of types.
		/// </summary>
		/// <param name="types">An array of Type objects representing the types of GUIDrawable objects to remove.</param>
		/// <returns>
		/// True if at least one GUIDrawable object was removed from the collection; otherwise, false.
		/// </returns>
		public bool Remove(Type[] types)
		{
			foreach (var guiDrawable in Drawables.Where(e => types.Contains(e.GetType())))
			{
				Remove(guiDrawable);
				return true;
			}

			return false;
		}
	}
}