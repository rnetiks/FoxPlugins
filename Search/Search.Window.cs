using System.Linq;
using UnityEngine;

namespace Search.KKS
{
	public partial class Search
	{
		private void WindowFunc(int id)
		{
			_tab = GUILayout.Toolbar(_tab, new[]
			{
				"Commands",
				"BepInEx",
			});

			_searchText = GUILayout.TextField(_searchText);
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

			var collection = commands.Values;

			if (collection.Where(command => command.Name.ToLower().Contains(_searchText.ToLower()))
				.OrderBy(d => d.Name).Where(command => _tab != 0 || command is SearchCommand).Any(RenderCommandButton))
			{
				return;
			}

			GUILayout.EndScrollView();
			GUI.DragWindow();
		}

		private bool RenderCommandButton(ISearchCommand command)
		{
			GUILayout.BeginHorizontal();

			var text = !IsNullOrWhiteSpace(command.Description)
				? $"{command.Name}: {command.Description}"
				: $"{command.Name}";

			if (GUILayout.Button(text, GUILayout.ExpandWidth(true)))
			{
				command.Execute();
				// showUI = false;
				GUILayout.EndHorizontal();
				GUILayout.EndScrollView();
				return true;
			}

			GUILayout.EndHorizontal();
			return false;
		}
	}
}