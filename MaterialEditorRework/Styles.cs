using UnityEngine;

namespace MaterialEditorRework
{
	public class Styles
	{
		public static GUIStyle DefaultLabelBlack
		{
			get
			{
				if (!StyleCache.Styles.ContainsKey("defaultLabelBlack"))
				{
					StyleCache.Styles["defaultLabelBlack"] = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.black } };
				}

				return StyleCache.Styles["defaultLabelBlack"];
			}
		}

		public static GUIStyle DefaultLabelCenteredBlack
		{
			get
			{
				if (!StyleCache.Styles.ContainsKey("defaultLabelBlackCentered"))
				{
					StyleCache.Styles["defaultLabelBlackCentered"] = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.black }, alignment = TextAnchor.MiddleCenter};
				}

				return StyleCache.Styles["defaultLabelBlackCentered"];
			}
		}

		public static GUIStyle BoldBlack
		{
			get
			{
				if (!StyleCache.Styles.ContainsKey("boldBlack"))
				{
					StyleCache.Styles["boldBlack"] = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.black } };
				}
				return StyleCache.Styles["boldBlack"];
			}
		}
	}
}