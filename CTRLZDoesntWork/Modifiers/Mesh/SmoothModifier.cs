using UnityEngine;
using UnityEngine.AI;

namespace CTRLZDoesntWork.KK
{
    public class SmoothModifier : BaseModifier
    {
        private float intensity;
        public override void Apply(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] smoothedVertices = new Vector3[vertices.Length];
            int[] triangles = mesh.triangles;
            int[] neighbourCount = new int[vertices.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                smoothedVertices[i] = Vector3.zero;
            }

            for (var i = 0; i < triangles.Length; i++)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                smoothedVertices[v1] += vertices[v2] + vertices[v3];
                smoothedVertices[v2] += vertices[v1] + vertices[v3];
                smoothedVertices[v3] += vertices[v1] + vertices[v2];

                neighbourCount[v1] += 2;
                neighbourCount[v2] += 2;
                neighbourCount[v3] += 2;
            }
            
            for (var i = 0; i < vertices.Length; i++)
            {
                if (neighbourCount[i] > 0)
                {
                    vertices[i] = Vector3.Lerp(vertices[i], smoothedVertices[i] / neighbourCount[i], intensity);
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }
    }
}