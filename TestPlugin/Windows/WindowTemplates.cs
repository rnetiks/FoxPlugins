using System;
using EasyWindow;
using UnityEngine;

namespace TestPlugin.Windows
{
	class ConfirmWindow : GUIDrawable
	{
		float width = 400, height = 200;

		public event Action<bool> OnSelect;
		private string content;
		private Rect position;

		public ConfirmWindow(string text)
		{
			content = text;
		}

		public ConfirmWindow() : this("Are you sure?")
		{
		}

		public override void Draw()
		{
			var f = Screen.width / 2f - width / 2f;
			var height1 = Screen.height / 2f - height / 2f;
			position = new Rect(f, height1, width, height);
			GUI.Box(position, "");
			if (GUI.Button(new Rect(f + 5, height1 + height - 45, 100, 40), "Confirm"))
			{
				OnSelect?.Invoke(true);
				ShouldKill = true;
			}

			if (GUI.Button(new Rect(f + width - 105, height1 + height - 45, 100, 40), "Cancel"))
			{
				OnSelect?.Invoke(false);
				ShouldKill = true;
			}

			GUI.Label(new Rect(f, height1, width, 40), content);
		}

		public override void OnUpdate()
		{
			if (position.Contains(Event.current.mousePosition))
				Input.ResetInputAxes();
		}
	}

	class PromptWindow : GUIDrawable
	{
		private Rect position;
		public event Action<string> OnSubmit;
		private string answer;
		private string content;

		public PromptWindow(string text)
		{
			content = text;
		}

		public PromptWindow()
		{
			var sw = Screen.width * 0.2f;
			var sh = Screen.height * 0.2f;
			position = new Rect(Screen.width / 2f - sw / 2, 10, sw, sh);
		}

		public override void Draw()
		{
			GUI.Box(position, "Prompt");
			GUI.SetNextControlName("textbox");
			answer = GUI.TextField(new Rect(position.x + 5, position.y + 25, position.width - 10, 20), answer);
		}

		public override void OnUpdate()
		{
			if (Event.current.keyCode == KeyCode.Return)
			{
				OnSubmit?.Invoke(answer);
				ShouldKill = true;
			}
		}
	}

	class TopLeftButtons : GUIDrawable
	{
		private Rect[] rects = new[]
		{
			new Rect(10, 10, 50, 50), new Rect(10, 70, 50, 50), new Rect(10, 130, 50, 50), new Rect(10, 190, 50, 50)
		};

		public override void Draw()
		{
			GUI.Button(rects[1], "Anim");
			GUI.Button(rects[2], "Sound");
			GUI.Button(rects[3], "System");
		}

		public override void OnUpdate()
		{
			foreach (var rect in rects)
			{
				if (rect.Contains(Event.current.mousePosition))
					Input.ResetInputAxes();
			}
		}
	}
}