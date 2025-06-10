using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BepInEx.Logging;
using HarmonyLib;
using TestPlugin;
using UnityEngine;
using Screen = UnityEngine.Screen;

public class HybridIMGUIConverter
{
	public static readonly Dictionary<int, HybridConvertedWindow>
		ConvertedWindows = new Dictionary<int, HybridConvertedWindow>();

	private static readonly List<RecordedUIElement> CurrentRecording	= new List<RecordedUIElement>();
	private static int _currentWindowId									= -1;
	private static bool _gameWindowBoundsNeedUpdate						= true;
	private static int _frameCounter;
	private static bool _isRecording;


	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

	[StructLayout(LayoutKind.Sequential)]
	public struct Rect
	{
		public int Left, Top, Right, Bottom;
	}


	[HarmonyPatch(typeof(GUI), nameof(GUI.Window), typeof(int), typeof(UnityEngine.Rect), typeof(GUI.WindowFunction),
		typeof(string))]
	public static class GUIWindowPatch
	{
		public static bool Prefix(int id, UnityEngine.Rect clientRect, GUI.WindowFunction func, string text,
			ref UnityEngine.Rect __result)
		{
			_frameCounter++;
			UpdateGameWindowBounds();

			_isRecording		= true;
			_currentWindowId	= id;
			CurrentRecording.Clear();

			try
			{
				func(id);
			}
			catch (Exception ex)
			{
				Entry.Logger.LogError($"Error recording window contents: {ex}");
			}

			_isRecording = false;

			bool shouldBeOutside;

			if (!ConvertedWindows.ContainsKey(id))
			{
				shouldBeOutside = IsWindowOutsideGameBounds(clientRect, false);
				CreateNewHybridWindow(id, clientRect, func, text, CurrentRecording, shouldBeOutside);
			}
			else
			{
				shouldBeOutside = IsWindowOutsideGameBounds(clientRect, ConvertedWindows[id].IsInFormsMode);
				UpdateHybridWindow(id, clientRect, text, CurrentRecording, shouldBeOutside);
			}

			var window = ConvertedWindows[id];

			if (window.IsInFormsMode)
			{
				if (window.Form != null && !window.Form.IsDisposed)
				{
					try
					{
						window.Form.Invoke(new Action(() =>
						{
							clientRect.x = window.Form.Location.X;
							clientRect.y = window.Form.Location.Y;
						}));
					}
					catch
					{
						// ignored
					}
				}

				__result = clientRect;
				return false;
			}

			window.LastIMGUIRect = clientRect;
			__result = clientRect;
			return true;
		}

		private static void CreateNewHybridWindow(int id, UnityEngine.Rect clientRect, GUI.WindowFunction func,
			string text, List<RecordedUIElement> elements, bool startInFormsMode)
		{
			var window = new HybridConvertedWindow
			{
				OriginalCallback	= func,
				CurrentElements		= new List<RecordedUIElement>(elements),
				LastUpdateFrame		= _frameCounter,
				LastIMGUIRect		= clientRect,
				IsInFormsMode		= startInFormsMode
			};

			if (startInFormsMode)
			{
				window.Form = CreateWindowsForm(clientRect, text, elements, window);
			}

			ConvertedWindows[id] = window;
		}

		private static void UpdateHybridWindow(int id, UnityEngine.Rect clientRect, string text,
			List<RecordedUIElement> newElements, bool shouldBeOutside)
		{
			var window = ConvertedWindows[id];
			bool modeChanged = window.IsInFormsMode != shouldBeOutside;

			if (modeChanged)
			{
				SwitchWindowMode(window, clientRect, text, newElements, shouldBeOutside);
			}
			else
			{
				if (window.IsInFormsMode)
				{
					UpdateFormsWindow(window, newElements);
				}
			}

			window.LastUpdateFrame = _frameCounter;
			window.CurrentElements = new List<RecordedUIElement>(newElements);
		}

		private static void SwitchWindowMode(HybridConvertedWindow window, UnityEngine.Rect clientRect,
			string text, List<RecordedUIElement> elements, bool toFormsMode)
		{
			if (toFormsMode)
			{
				Entry.Logger.LogDebug("Switching to Windows Forms mode");
				window.LastIMGUIRect	= clientRect;
				window.Form				= CreateWindowsForm(clientRect, text, elements, window);
				window.IsInFormsMode	= true;
			}
			else
			{
				Entry.Logger.LogDebug("Switching to IMGUI mode");
				if (window.Form != null && !window.Form.IsDisposed)
				{
					try
					{
						window.Form.Invoke(new Action(() =>
						{
							var windowLastIMGUIRect = window.LastIMGUIRect;
							windowLastIMGUIRect.x = window.Form.Location.X;
							windowLastIMGUIRect.y = window.Form.Location.Y;
							window.LastIMGUIRect = windowLastIMGUIRect;
							window.Form.Close();
						}));
					}
					catch
					{
						// ignored
					}
				}

				window.Form				= null;
				window.IsInFormsMode	= false;
			}
		}
	}

	private static bool IsWindowOutsideGameBounds(UnityEngine.Rect windowRect, bool currentlyInForms)
	{
		float margin = 10;
		float centerX = windowRect.x + windowRect.width / 2;
		float centerY = windowRect.y + windowRect.height / 2;

		return centerX > Screen.width + margin || centerX < -margin ||
			   centerY > Screen.height + margin || centerY < -margin;
	}

	private static void UpdateGameWindowBounds()
	{
		if (_gameWindowBoundsNeedUpdate || _frameCounter % 60 == 0)
		{
			try
			{
				IntPtr gameWindow = GetActiveWindow();
				if (GetWindowRect(gameWindow, out Rect rect))
				{
					new UnityEngine.Rect(rect.Left, rect.Top,
						rect.Right - rect.Left, rect.Bottom - rect.Top);
					_gameWindowBoundsNeedUpdate = false;
				}
			}
			catch
			{
				new UnityEngine.Rect(0, 0, Screen.width, Screen.height);
			}
		}
	}

	private static void UpdateFormsWindow(HybridConvertedWindow window, List<RecordedUIElement> newElements)
	{
		if (window.Form == null || window.Form.IsDisposed) return;


		var movedInside = false;
		try
		{
			window.Form.Invoke(new Action(() =>
			{
				var formRect = new UnityEngine.Rect(window.Form.Location.X, window.Form.Location.Y,
					window.Form.Width, window.Form.Height);
				movedInside = !IsWindowOutsideGameBounds(formRect, true);
			}));
		}
		catch
		{
			return;
		}

		if (movedInside)
		{
			return;
		}


		var elementsToAdd	= newElements.Where(ne => !window.CurrentElements.Any(ce => ce.Id == ne.Id)).ToList();
		var elementsToRemove = window.CurrentElements.Where(ce => !newElements.Any(ne => ne.Id == ce.Id)).ToList();
		var elementsToUpdate = newElements
			.Where(ne => window.CurrentElements.Any(ce => ce.Id == ne.Id && !ElementsEqual(ce, ne))).ToList();

		if (elementsToAdd.Any() || elementsToRemove.Any() || elementsToUpdate.Any())
		{
			UpdateWindowForm(window, elementsToAdd, elementsToRemove, elementsToUpdate);
		}
	}


	[HarmonyPatch(typeof(GUI), nameof(GUI.Button), typeof(UnityEngine.Rect), typeof(string))]
	public static class GUIButtonPatch
	{
		public static bool Prefix(UnityEngine.Rect position, string text, ref bool __result)
		{
			if (_currentWindowId != -1 && ConvertedWindows.TryGetValue(_currentWindowId, out var window))
			{
				// if (!window.IsInFormsMode) return true;
				var elementId = GenerateElementId(UIElementType.Button, position, text);
				__result = window.GetButtonState(elementId);
				if (__result)
				{
					return false;
				}
			}
			// TODO issue, because recording is always true the bottom never gets executed
			if (_isRecording)
			{
				RecordUIElement(UIElementType.Button, position, text);
				__result = false;
				return false;
			}


			return true;
		}
	}


	[HarmonyPatch(typeof(GUI), nameof(GUI.Label), typeof(UnityEngine.Rect), typeof(string))]
	public static class GUILabelPatch
	{
		public static bool Prefix(UnityEngine.Rect position, string text)
		{
			if (_isRecording)
			{
				RecordUIElement(UIElementType.Label, position, text);
				return false;
			}

			if (_currentWindowId != -1 && ConvertedWindows.TryGetValue(_currentWindowId, out var window))
			{
				return !window.IsInFormsMode;
			}

			return true;
		}
	}

	/// <summary>
	/// Represents a Harmony patch for the Unity GUI.TextField method.
	/// This patch enables interception of TextField UI element rendering
	/// and interaction. It allows for custom behavior, such as recording
	/// the state of the TextField during a hybrid IMGUI workflow or
	/// managing the element in a converted forms-based mode.
	/// </summary>
	[HarmonyPatch(typeof(GUI), nameof(GUI.TextField), typeof(UnityEngine.Rect), typeof(string))]
	public static class GUITextFieldPatch
	{
		public static bool Prefix(UnityEngine.Rect position, string text, ref string __result)
		{
			if (_isRecording)
			{
				RecordUIElement(UIElementType.TextField, position, text);
				__result = text;
				return false;
			}

			if (_currentWindowId != -1 && ConvertedWindows.TryGetValue(_currentWindowId, out var window))
			{
				if (!window.IsInFormsMode) return true;
				var elementId = GenerateElementId(UIElementType.TextField, position, null);
				__result = window.GetTextFieldValue(elementId);
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Represents a Harmony patch for the Unity GUI.Toggle method.
	/// This patch enables interception of Toggle UI element rendering
	/// and interaction, facilitating custom behavior such as recording
	/// state during a hybrid IMGUI workflow or handling in a converted forms-based mode.
	/// </summary>
	[HarmonyPatch(typeof(GUI), nameof(GUI.Toggle), typeof(UnityEngine.Rect), typeof(bool), typeof(string))]
	public static class GUITogglePatch
	{
		public static bool Prefix(UnityEngine.Rect position, bool value, string text, ref bool __result)
		{
			if (_isRecording)
			{
				RecordUIElement(UIElementType.Toggle, position, text, value);
				__result = value;
				return false;
			}

			if (_currentWindowId != -1 && ConvertedWindows.TryGetValue(_currentWindowId, out var window))
			{
				if (!window.IsInFormsMode) return true;
				var elementId = GenerateElementId(UIElementType.Toggle, position, text);
				__result = window.GetToggleState(elementId);
				return false;
			}

			return true;
		}
	}


	/// <summary>
	/// Records a UI element by storing its type, position, text, and optional value
	/// into the current recording session for further processing or conversion.
	/// </summary>
	/// <param name="type">The type of the UI element being recorded, represented as <see cref="UIElementType"/>.</param>
	/// <param name="position">The position and size of the UI element defined as a <see cref="UnityEngine.Rect"/>.</param>
	/// <param name="text">The text associated with the UI element, which can be a label, name, or identifier.</param>
	/// <param name="value">An optional value associated with the UI element (e.g., a state for a Toggle), which can be null.</param>
	private static void RecordUIElement(UIElementType type, UnityEngine.Rect position, string text, object value = null)
	{
		CurrentRecording.Add(new RecordedUIElement
		{
			Type = type,
			Position = position,
			Text = text,
			Value = value,
			Id = GenerateElementId(type, position, text)
		});
	}

	/// <summary>
	/// Generates a unique identifier for a UI element based on its type, position, and optional text.
	/// </summary>
	/// <param name="type">The type of the UI element, such as Button, Label, or Toggle.</param>
	/// <param name="position">The position and size of the element defined as a <see cref="UnityEngine.Rect"/>.</param>
	/// <param name="text">Optional text associated with the element, which may be null or empty.</param>
	/// <returns>A string representing a unique identifier for the specified UI element.</returns>
	private static string GenerateElementId(UIElementType type, UnityEngine.Rect position, string text)
	{
		return $"{type}_{position.x:F0}_{position.y:F0}_{text?.GetHashCode() ?? 0}";
	}

	/// <summary>
	/// Determines whether two <see cref="RecordedUIElement"/> instances are considered equal
	/// based on their identifiers, types, text contents, and positions.
	/// </summary>
	/// <param name="elem1">The first UI element to compare.</param>
	/// <param name="elem2">The second UI element to compare.</param>
	/// <returns>True if the two elements are equal; otherwise, false.</returns>
	private static bool ElementsEqual(RecordedUIElement elem1, RecordedUIElement elem2)
	{
		return elem1.Id == elem2.Id &&
			   elem1.Type == elem2.Type &&
			   elem1.Text == elem2.Text &&
			   Math.Abs(elem1.Position.x - elem2.Position.x) < 0.1f &&
			   Math.Abs(elem1.Position.y - elem2.Position.y) < 0.1f;
	}

	/// <summary>
	/// Creates a Windows Forms instance using the specified client rectangle, title, UI elements, and associated hybrid window.
	/// </summary>
	/// <param name="clientRect">The rectangle defining the position and dimensions of the form on the screen.</param>
	/// <param name="title">The title text to be displayed on the form's title bar.</param>
	/// <param name="elements">A list of recorded UI elements to populate the form with corresponding controls.</param>
	/// <param name="window">The hybrid window object that will be associated with the created form.</param>
	/// <returns>A new instance of a Windows Form populated with the specified UI elements and properties.</returns>
	private static Form CreateWindowsForm(UnityEngine.Rect clientRect, string title, List<RecordedUIElement> elements,
		HybridConvertedWindow window)
	{
		var form = new Form
		{
			Text = title + " (External)",
			Location = new Point((int)clientRect.x, (int)clientRect.y),
			Size = new Size((int)clientRect.width, (int)clientRect.height),
			FormBorderStyle = FormBorderStyle.Sizable,
			StartPosition = FormStartPosition.Manual,
			TopMost = true,
			ControlBox = false
		};

		foreach (var element in elements)
		{
			var control = CreateControlForElement(element, window);
			if (control != null)
			{
				form.Controls.Add(control);
			}
		}

		form.Show();

		return form;
	}

	/// <summary>
	/// Creates a Windows Forms control for the specified UI element and associates it with a window.
	/// </summary>
	/// <param name="element">The UI element to create a control for, containing information such as type, position, text, value, and ID.</param>
	/// <param name="window">The hybrid converted window to which the created control will belong.</param>
	/// <returns>A Windows Forms control corresponding to the specified UI element, or null if the element type is not supported.</returns>
	private static Control CreateControlForElement(RecordedUIElement element, HybridConvertedWindow window)
	{
		var location = new Point((int)element.Position.x, (int)element.Position.y - 20);
		var size = new Size((int)element.Position.width, (int)element.Position.height);

		switch (element.Type)
		{
			case UIElementType.Button:
				var button = new System.Windows.Forms.Button
				{
					Text = element.Text,
					Location = location,
					Size = size,
					Tag = element.Id
				};
				button.Click += (s, e) => { window.SetElementState(element.Id, true); };
				return button;

			case UIElementType.Label:
				return new System.Windows.Forms.Label
				{
					Text = element.Text,
					Location = location,
					Size = size,
					Tag = element.Id
				};

			case UIElementType.TextField:
				var textBox = new System.Windows.Forms.TextBox
				{
					Text = element.Text,
					Location = location,
					Size = size,
					Tag = element.Id
				};
				textBox.TextChanged += (s, e) => { window.SetElementState(element.Id, textBox.Text); };
				return textBox;

			case UIElementType.Toggle:
				var checkBox = new System.Windows.Forms.CheckBox
				{
					Text = element.Text,
					Location = location,
					Size = size,
					Checked = element.Value is bool b && b,
					Tag = element.Id
				};
				checkBox.CheckedChanged += (s, e) => window.SetElementState(element.Id, checkBox.Checked);
				return checkBox;
		}

		return null;
	}

	/// <summary>
	/// Updates the specified window form by adding, removing, and updating UI elements.
	/// </summary>
	/// <param name="window">The hybrid converted window that contains the form and associated elements.</param>
	/// <param name="toAdd">The list of UI elements that need to be added to the form.</param>
	/// <param name="toRemove">The list of UI elements that need to be removed from the form.</param>
	/// <param name="toUpdate">The list of UI elements that need to be updated within the form.</param>
	private static void UpdateWindowForm(HybridConvertedWindow window,
		List<RecordedUIElement> toAdd,
		List<RecordedUIElement> toRemove,
		List<RecordedUIElement> toUpdate)
	{
		if (window.Form == null || window.Form.IsDisposed) return;

		try
		{
			window.Form.Invoke(new Action(() =>
			{
				foreach (var element in toRemove)
				{
					var controlToRemove = window.Form.Controls.Cast<Control>()
						.FirstOrDefault(c => c.Tag?.ToString() == element.Id);
					if (controlToRemove != null)
					{
						window.Form.Controls.Remove(controlToRemove);
						controlToRemove.Dispose();
					}
				}


				foreach (var element in toAdd)
				{
					var control = CreateControlForElement(element, window);
					if (control != null)
					{
						window.Form.Controls.Add(control);
					}
				}
			}));
		}
		catch (Exception ex)
		{
			Entry.Logger.LogError($"Error updating window form: {ex}");
		}
	}
}

public class HybridConvertedWindow
{
	/// <summary>
	/// Gets or sets the System.Windows.Forms.Form associated with the hybrid converted window.
	/// This property represents the Windows Forms instance utilized for rendering
	/// and managing the window when in Forms mode.
	/// </summary>
	public Form Form { get; set; }

	/// <summary>
	/// Gets or sets the callback function associated with the original GUI.Window API.
	/// This delegate represents the logic that defines the content and behavior of
	/// the specific GUI window being processed or displayed.
	/// </summary>
	public GUI.WindowFunction OriginalCallback { get; set; }

	/// <summary>
	/// Gets or sets the current list of recorded UI elements associated with the window.
	/// This property maintains the UI element states and definitions as they appear in the
	/// hybrid IMGUI or Forms-based rendering system.
	/// </summary>
	public List<RecordedUIElement> CurrentElements { get; set; }

	/// <summary>
	/// Gets or sets the frame number in which the window was last updated.
	/// This property is used to track the most recent update to the window's state,
	/// ensuring synchronization between the IMGUI system and the hybrid integration approach.
	/// </summary>
	public int LastUpdateFrame { get; set; }

	/// <summary>
	/// Gets or sets the last recorded position and size of the IMGUI window
	/// associated with this instance. This property is used to maintain the
	/// most recent IMGUI bounds for accurate rendering and mode switching
	/// between IMGUI and alternative forms-based UI systems.
	/// </summary>
	public Rect LastIMGUIRect { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the window is operating in
	/// Forms mode as opposed to IMGUI mode. This property determines
	/// the rendering mode and behavior of the window, switching between
	/// traditional IMGUI rendering or utilizing Windows Forms for UI representation.
	/// </summary>
	public bool IsInFormsMode { get; set; }

	/// <summary>
	/// A dictionary that stores the state of UI elements in the hybrid IMGUI system.
	/// The dictionary uses the unique element identifier as the key and an object
	/// representing the state or value of the element as the value. This allows tracking
	/// the state of various UI components, such as buttons, text fields, toggles, and others.
	/// </summary>
	private Dictionary<string, object> elementStates = new Dictionary<string, object>();

	public bool GetButtonState(string elementId)
	{
		lock (elementStates)
		{
			if (!elementStates.TryGetValue(elementId, out var state) || !(state is bool b)) return false;
			elementStates[elementId] = false;
			return b;
		}
	}

	/// <summary>
	/// Retrieves the stored value of a text field identified by its unique element ID
	/// from the hybrid IMGUI system's element state dictionary.
	/// </summary>
	/// <param name="elementId">The unique identifier of the text field whose value is to be retrieved.</param>
	/// <returns>The value of the text field if it exists and is a string; otherwise, an empty string.</returns>
	public string GetTextFieldValue(string elementId)
	{
		lock (elementStates)
		{
			Entry.Logger.LogDebug(elementId);
			return elementStates.GetValueSafe(elementId).ToString();
			// return elementStates.TryGetValue(elementId, out var state) && state is string s ? s : "err";
		}
	}

	/// <summary>
	/// Retrieves the current toggle state for the specified UI element based on its unique identifier.
	/// </summary>
	/// <param name="elementId">The unique identifier of the toggle UI element.</param>
	/// <returns>A boolean value indicating the current state of the toggle (true for "on", false for "off").</returns>
	public bool GetToggleState(string elementId)
	{
		lock (elementStates)
		{
			return elementStates.TryGetValue(elementId, out var state) && state is bool b && b;
		}
	}

	/// <summary>
	/// Updates the state of a specific UI element within the hybrid IMGUI system.
	/// </summary>
	/// <param name="elementId">The unique identifier of the UI element whose state is being updated.</param>
	/// <param name="state">The new state or value to assign to the UI element.</param>
	public void SetElementState(string elementId, object state)
	{
		lock (elementStates)
		{
			elementStates[elementId] = state;
		}
	}
}

/// <summary>
/// Represents a UI element recorded within the hybrid IMGUI and Forms-based system.
/// This class encapsulates the characteristics and state of a UI element
/// such as its type, layout, text content, and associated value or identifier.
/// </summary>
public class RecordedUIElement
{
	/// <summary>
	/// Gets or sets the type of the UI element being recorded or processed.
	/// This property specifies the category of the UI element, such as
	/// Button, Label, TextField, Toggle, or other defined types.
	/// </summary>
	public UIElementType Type { get; set; }

	/// <summary>
	/// Gets or sets the position and size of the UI element.
	/// The position is represented using a Rect structure, which defines the
	/// coordinates of the element and its width and height within the graphical interface.
	/// </summary>
	public Rect Position { get; set; }

	/// <summary>
	/// Gets or sets the textual content associated with the UI element.
	/// The text represents the main label or information that appears on or
	/// within the UI component, such as a button label or placeholder text
	/// for a text box.
	/// </summary>
	public string Text { get; set; }

	/// <summary>
	/// Gets or sets the value associated with the UI element. This property can
	/// represent the current state or input of the element, such as text for
	/// a text field, a boolean state for a toggle, or any other data relevant
	/// to the UI element.
	/// </summary>
	public object Value { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for an entity or object.
	/// </summary>
	public string Id { get; set; }
}

/// <summary>
/// Represents the types of UI elements that can be handled or processed
/// within the hybrid IMGUI system. These elements define various components
/// of the graphical user interface.
/// </summary>
public enum UIElementType
{
	Button,
	Label,
	TextField,
	Toggle,
	Slider,
	Box
}

/// <summary>
/// Utility class for managing and controlling the hybrid IMGUI and Forms-based UI conversion functionality.
/// </summary>
public static class HybridIMGUIConverterUtils
{
	/// <summary>
	/// Forces a specific window to use either IMGUI or Forms-based mode in the hybrid UI system.
	/// </summary>
	/// <param name="windowId">The unique identifier of the target window.</param>
	/// <param name="useFormsMode">Indicates whether the window should use Forms-based mode. Pass <c>true</c> to use Forms mode, or <c>false</c> to use IMGUI mode.</param>
	public static void ForceWindowMode(int windowId, bool useFormsMode)
	{
		if (HybridIMGUIConverter.ConvertedWindows.TryGetValue(windowId, out var window))
		{
			window.IsInFormsMode = !useFormsMode;
		}
	}

	/// <summary>
	/// Determines whether a specified window is currently operating in Forms-based mode within the hybrid UI system.
	/// </summary>
	/// <param name="windowId">The unique identifier of the target window.</param>
	/// <returns>Returns <c>true</c> if the specified window is in Forms-based mode; otherwise, <c>false</c>.</returns>
	public static bool IsWindowInFormsMode(int windowId)
	{
		return HybridIMGUIConverter.ConvertedWindows.TryGetValue(windowId, out var window) &&
			   window.IsInFormsMode;
	}
}