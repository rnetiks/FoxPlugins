using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace MaterialEditorRework.Exporter
{
	public class OBJExporter
	{
		// TODO Replace with Config entries
		private const bool ExportBakedMesh = true;
		private const bool ExportBakedPosition = true;
		public static void Export(Renderer renderer)
		{
			string fileName = Path.Combine(Paths.ExecutablePath, $"{renderer.name}.obj");
			using (StreamWriter streamWriter = new StreamWriter(fileName))
			{
				string mesh = MeshToObj(renderer);
				if (string.IsNullOrWhiteSpace(mesh))
				{
					streamWriter.Write(mesh);
				}
			}
		}

		private static Mesh GetMeshFromRenderer(Renderer renderer)
		{
			switch (renderer)
			{
				case MeshRenderer meshRenderer:
					return meshRenderer.GetComponent<MeshFilter>().mesh;
				case SkinnedMeshRenderer skinnedMeshRenderer:
					return skinnedMeshRenderer.sharedMesh;
				default:
					return null;
			}
		}

		private static Mesh BakeMesh(SkinnedMeshRenderer skinnedMeshRenderer)
		{
			var bakedMesh = new Mesh();
			skinnedMeshRenderer.BakeMesh(bakedMesh);
			return bakedMesh;
		}

		private static string MeshToObj(Renderer renderer)
		{
			Mesh mesh = GetMeshFromRenderer(renderer);
			if (!mesh) return string.Empty;

			var scale = renderer.transform.lossyScale;
			StringBuilder stringBuilder = new StringBuilder();
			
			for (var index = 0; index < mesh.vertices.Length; index++)
			{
				Vector3 vertex = mesh.vertices[index];
				if(ExportBakedMesh && ExportBakedPosition)
					vertex = renderer.transform.TransformPoint(vertex);

				stringBuilder.AppendLine($"v {-vertex.x} {vertex.y} {vertex.z}");
			}
			
			foreach (var uv in mesh.uv)
			{
				stringBuilder.AppendLine($"vt {uv.x} {uv.y}");
			}

			for (var i = 0; i < mesh.normals.Length; i++)
			{
				Vector3 normal = mesh.normals[i];
				if (ExportBakedMesh && ExportBakedPosition)
					normal = renderer.transform.TransformDirection(normal);
				stringBuilder.AppendLine($"vn {-normal.x} {normal.y} {normal.z}");
			}

			for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
			{
				stringBuilder.AppendLine($"g {renderer.name}_{submeshIndex}");
				int[] triangles = mesh.GetTriangles(submeshIndex);

				for (int i = 0; i < triangles.Length; i += 3)
				{
					int v1 = triangles[i] + 1;
					int v2 = triangles[i + 2] + 1;
					int v3 = triangles[i + 1] + 1;
					stringBuilder.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", v1, v2, v3);
				}
			}

			return stringBuilder.ToString();
		}
	}
}