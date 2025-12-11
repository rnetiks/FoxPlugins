using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MaterialEditorRework.CustomElements;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.Views
{
	public class ListTreeviewView : BaseElementView
	{
		List<TreeViewItemParent> items = new List<TreeViewItemParent>();

		private string _filter = string.Empty;
		public string Filter => _filter;

		public void SetFilter(string filter)
		{
			_filter = filter;
		}

		public override void Draw(Rect rect)
		{
			var borderSizeValue = Entry.borderSize.Value;
			GUI.DrawTexture(rect, TextureCache.GetOrCreateSolid(Color.white));
			GUI.DrawTexture(new Rect(rect.width - borderSizeValue, rect.y, borderSizeValue, rect.height), TextureCache.GetOrCreateSolid(new Color(0.9f, 0.91f, 0.92f)));
			if (Entry.Instance.listHeaderView._dropdown.IsOpen)
				GUI.enabled = false;
			DrawList(rect);
			GUI.enabled = true;
		}

		public void CleanItems()
		{
			items.Clear();
		}

		public void AddItems(TreeViewItemParent[] items, bool clearOld = true)
		{
			if (clearOld)
				this.items.Clear();
			this.items.AddRange(items);
		}

		public void DeselectAll()
		{
			foreach (var treeViewItemParent in items)
			{
				treeViewItemParent.Selected = false;
				if (treeViewItemParent.Children != null)
				{
					foreach (var treeViewItemChild in treeViewItemParent.Children)
					{
						treeViewItemChild.Selected = false;
					}
				}
			}
		}

		public bool AnySelected()
		{
			foreach (var treeViewItemParent in items)
			{
				if (treeViewItemParent.Selected) return true;
				if ( treeViewItemParent.Children != null )
					foreach (var treeViewItemChild in treeViewItemParent.Children)
					{
						if (treeViewItemChild.Selected) return true;
					}
			}

			return false;
		}

		private Vector2 scrollPosition = Vector2.zero;

		private void DrawList(Rect rect)
		{
			bool reduce = CalculateHeight() > rect.height;
			int i = 0;
			scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, 0, CalculateHeight()));

			if (items.Any(x => x.Renderer.name.Contains(Filter)))
			{
				foreach (var item in items)
				{
					if (item.Renderer.name.Contains(Filter))
					{
						item.Draw(new Rect(8, 55 * i + 8, reduce ? 293 : 303, 50));
						i++;
					}
				}
			}
			else
			{
				foreach (var item in items)
				{

					item.Draw(new Rect(8, 55 * i + 8, reduce ? 293 : 303, 50));
					i++;
				}
			}

			GUI.EndScrollView();
		}

		private int CalculateHeight()
		{
			int i = 0;
			foreach (var treeViewItemParent in items)
			{
				if (treeViewItemParent.Renderer.name.Contains(Filter) || (treeViewItemParent.Children != null && treeViewItemParent.Children.Any(x => x.Material.name.Contains(Filter)) && treeViewItemParent.Open))
				{
					i += 55;
					if (treeViewItemParent.Children != null && treeViewItemParent.Open)
					{
						foreach (var unused in treeViewItemParent.Children)
						{
							i += 55;
						}
					}
				}
			}

			return i;
		}
	}
}