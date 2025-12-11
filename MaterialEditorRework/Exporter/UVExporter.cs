using System.IO;
using BepInEx;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.Exporter
{
	public class UVExporter
	{
		public static void ExportUVMaps(Renderer renderer)
		{
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			var lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
			lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			lineMaterial.SetInt("_ZWrite", 0);

			Mesh mesh;
			if (renderer is MeshRenderer meshRenderer)
			{
				mesh = meshRenderer.GetComponent<MeshFilter>().mesh;
			}
			else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
			{
				mesh = skinnedMeshRenderer.sharedMesh;
			}
			else
			{
				return;
			}

			const int size = 4096;
			var lineColor = Color.black;
			
			for (var index = 0; index < mesh.subMeshCount; index++)
			{
				var triangles = mesh.GetTriangles(index);
				var uv = mesh.uv;

				var renderTexture = RenderTexture.GetTemporary(size, size);
				Graphics.SetRenderTarget(renderTexture);
				GL.PushMatrix();
				GL.LoadOrtho();
				GL.Clear(false, true, Color.clear);

				lineMaterial.SetPass(0);
				GL.Begin(GL.LINES);
				GL.Color(lineColor);
				
				for (var triangle_index = 0; triangle_index < triangles.Length; triangle_index++)
				{
					Vector2 v = new Vector2(Reduce(uv[triangles[triangle_index]].x), Reduce(uv[triangles[triangle_index]].y));
					Vector2 n1 = new Vector2(Reduce(uv[triangles[triangle_index + 1]].x), Reduce(uv[triangles[triangle_index + 1]].y));
					Vector2 n2 = new Vector2(Reduce(uv[triangles[triangle_index + 2]].x), Reduce(uv[triangles[triangle_index + 2]].y));
					
					GL.Vertex(v);
					GL.Vertex(n1);
					
					GL.Vertex(v);
					GL.Vertex(n2);
					
					GL.Vertex(n1);
					GL.Vertex(n2);
				}
				
				GL.End();
				
				GL.PopMatrix();
				Graphics.SetRenderTarget(null);
				
				var png = renderTexture.RT2T2D();
				RenderTexture.ReleaseTemporary(renderTexture);
				
				var rendererName = renderer.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
				rendererName = string.Concat(rendererName.Split(Path.GetInvalidFileNameChars())).Trim();
				string fileName = Path.Combine(Paths.ExecutablePath, $"{rendererName}_{index}");
				TextureFactory.From(png).Save(fileName);
				Object.DestroyImmediate(png);
			}
		}

		/// <summary>
		/// Reduces a given float value to its fractional part within the range [0, 1).
		/// </summary>
		/// <param name="value">The float value to be reduced.</param>
		/// <returns>The fractional part of the input value, ranging between 0 and 1.</returns>
		private static float Reduce(float value)
		{ 
			return (value % 1f + 1f) % 1f;
		}
	}
}