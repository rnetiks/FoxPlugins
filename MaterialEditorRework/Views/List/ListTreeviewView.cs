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
		public List<TreeViewItemParent> items = new List<TreeViewItemParent>();

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
			foreach (var parent in Entry.Instance.listTreeviewView.items)
			{
				parent.Selected = false;
				foreach (var child in parent.Children)
				{
					child.Selected = false;
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

			int yx = 0;
			if (items.Any(x => x.Renderer.name.Contains(Filter)))
			{
				int sum = items.Where(x => x.Renderer.name.ToLower().Contains(_filter.ToLower())).Sum(x => x.CalculateHeight());
				bool reduce = sum > rect.height;
				scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, 0, sum + 5));
				foreach (var item in items)
				{
					if (item.Renderer.name.Contains(Filter))
					{
						item.Draw(new Rect(8, yx + 8, reduce ? 293 : 303, 50));
						yx += item.CalculateHeight();
					}
				}
				GUI.EndScrollView();
			}
			else
			{
				int sum = items.Sum(x => x.CalculateHeight());
				bool reduce = sum > rect.height;
				scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, 0, sum + 5));
				foreach (var item in items)
				{
					item.Draw(new Rect(8, yx + 8, reduce ? 293 : 303, 50));
					yx += item.CalculateHeight();
				}
				GUI.EndScrollView();
			}

		}
	}
}