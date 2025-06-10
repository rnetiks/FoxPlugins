namespace EasyWindow
{
	/// <summary>
	/// Represents an abstract drawable GUI element that can be rendered and managed in a graphical application.
	/// Provides lifecycle hooks for initialization and cleanup, as well as the ability to define persistence and uniqueness conditions.
	/// </summary>
	public abstract class GUIDrawable
	{
		/// <summary>
		/// Draws the graphical user interface for the implementing class.
		/// </summary>
		/// <remarks>
		/// This method is expected to be overridden by derived classes to define
		/// specific GUI components and behaviors. Implementations of this method
		/// should perform drawing operations using Unity's GUI system.
		/// </remarks>
		public abstract void Draw();

		/// <summary>
		/// Indicates whether the drawable instance is marked for removal from the rendering process.
		/// </summary>
		public bool ShouldKill { get; set; }

		/// <summary>
		/// Called when the GUIDrawable object is enabled or added to the GUIHandler.
		/// This method can be overridden in derived classes to implement custom logic
		/// that needs to run when the GUIDrawable becomes active. Typical use cases may
		/// include initialization or state resetting logic necessary for the drawable's functionality.
		/// </summary>
		public virtual void OnEnable()
		{
		}

		public virtual void OnUpdate(){}

		/// <summary>
		/// Lifecycle method invoked when the object is disabled or removed.
		/// Can be overridden in derived classes to implement custom behavior on disable.
		/// Typically used to release resources, detach event handlers, or handle cleanup logic.
		/// </summary>
		public virtual void OnDisable()
		{
		}

		public virtual void OnInitialize()
		{
		}

		/// <summary>
		/// Invoked during object destruction as part of the lifecycle of the GUIDrawable.
		/// </summary>
		/// <remarks>
		/// This method can be overridden by derived classes to handle cleanup tasks,
		/// such as releasing unmanaged resources, unregistering event listeners, or
		/// finalizing operations specific to the drawable object.
		/// It is called when the GUIDrawable is permanently removed or destroyed.
		/// </remarks>
		public virtual void OnDestroy()
		{
		}

		/// <summary>
		/// Indicates whether the GUIDrawable instance is unique within its collection.
		/// If set to true, only one instance of this type can exist in a collection.
		/// Attempting to add a second instance of the same type will result in an exception.
		/// </summary>
		public bool Unique { get; set; } = true;

		/// <summary>
		/// Determines whether the GUIDrawable instance is active and can participate in rendering and lifecycle operations.
		/// </summary>
		/// <remarks>
		/// When set to true, the instance is enabled, triggering the <see cref="OnEnable"/> method to execute.
		/// Conversely, setting it to false disables the instance, invoking the <see cref="OnDisable"/> method.
		/// This property ensures that only active GUIDrawable instances are processed within a GUIHandler.
		/// </remarks>
		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (value)
				{
					OnEnable();
				}
				else
				{
					OnDisable();
				}
				
				_enabled = value;
			}
		}
		
		private bool _enabled = true;
	}
}