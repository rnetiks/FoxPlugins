using System;
using System.Linq;
using Addin;
using KKAPI.Utilities;
using MaterialEditorRework.Views;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
	public class PropertiesContainer : ToggleContainerBase
	{
		private MaterialCache _target;
		private object[] _uiComponents;

		public PropertiesContainer(Vector2 size, Material material) : base(size)
		{
			_target = new MaterialCache(material);
			var filter = _target.Properties.Where(x => x.Type == ShaderPropertyType.Float || x.Type == ShaderPropertyType.Range || x.Type == ShaderPropertyType.Vector).ToArray();
			_uiComponents = new object[filter.Length];
			for (var i = 0; i < filter.Length; i++)
			{
				var property = filter[i];
				float currentValue;
				switch (filter[i].Type)
				{
					case ShaderPropertyType.Vector:
						break;
					case ShaderPropertyType.Float:
						currentValue = material.GetFloat(filter[i].Name);
						_uiComponents[i] = new Slider(currentValue, 0, 1)
						{
							AllowUnclamped = true
						};
						( (Slider)_uiComponents[i] ).OnValueChanged += (value) => material.SetFloat(property.Name, value);
						break;
					case ShaderPropertyType.Range:
						currentValue = material.GetFloat(filter[i].Name);
						var minmax = material.shader.GetPropertyRangeLimits(filter[i].Index);
						_uiComponents[i] = new Slider(currentValue, minmax.x, minmax.y)
						{
							AllowUnclamped = false
						};
						( (Slider)_uiComponents[i] ).OnValueChanged += (value) => material.SetFloat(property.Name, value);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		public override void DrawHeader(Rect rect)
		{
			throw new System.NotImplementedException();
		}
		public override void DrawContent(Rect rect)
		{
			throw new System.NotImplementedException();
		}
		public override int GetHeight()
		{
			throw new System.NotImplementedException();
		}
	}
}