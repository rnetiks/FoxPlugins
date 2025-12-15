using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialEditorRework
{
	public class MaterialCache
	{
		public Material Material { get;  }
		public int PropertyCount { get; private set; }
		public PropertyData[] Properties { get; private set; }
		public MaterialCache(Material material)
		{
			Material = material;
			PropertyCount = Material.shader.GetPropertyCount();
			Properties = new PropertyData[PropertyCount];
			for (int i = 0; i < PropertyCount; i++)
			{
				Properties[i] = new PropertyData
				{
					Type = Material.shader.GetPropertyType(i),
					Name = Material.shader.GetPropertyName(i),
					Index = i
				};
			}
		}

		public struct PropertyData
		{
			public ShaderPropertyType Type;
			public string Name;
			public int Index;
		}
	}
}