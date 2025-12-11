using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorRework
{
	public class StyleCache
	{
		public static Dictionary<string, GUIStyle> Styles = new Dictionary<string, GUIStyle>();

		public static bool AddStyle(string styleName, GUIStyle style)
		{
			if (Styles.ContainsKey(styleName))
				return false;
			Styles.Add(styleName, style);
			return true;
		}

		public static string AddStyle(GUIStyle style)
		{
			var styleName = style.GetHashCode().ToString();
			if (Styles.ContainsKey(styleName))
				return string.Empty;
			Styles.Add(styleName, style);
			return styleName;
		}

		public static GUIStyle GetStyle(string styleName)
		{
			return Styles.TryGetValue(styleName, out var style) ? style : null;
		}
	}
}