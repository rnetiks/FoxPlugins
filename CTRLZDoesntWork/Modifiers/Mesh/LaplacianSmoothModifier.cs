using System.Collections.Generic;
using UnityEngine;

namespace CTRLZDoesntWork.KK
{
    public class LaplacianSmoothModifier : BaseModifier
    {
        private int _iterations;
        private float _intensity;

        public LaplacianSmoothModifier(int iterations, float intensity)
        {
            _iterations = iterations;
            _intensity = intensity;
        }
        
        public override void Apply(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] originalVertices = (Vector3[])vertices.Clone();
            int[] triangles = mesh.triangles;
            
            Dictionary<int, List<int>> vertexNeighbours = new Dictionary<int, List<int>>();
            
            for (var i = 0; i < triangles.Length; i++)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                if (!vertexNeighbours.ContainsKey(v1)) vertexNeighbours[v1] = new List<int>();
                if (!vertexNeighbours.ContainsKey(v2)) vertexNeighbours[v2] = new List<int>();
                if (!vertexNeighbours.ContainsKey(v3)) vertexNeighbours[v3] = new List<int>();

                
                vertexNeighbours[v1].Add(v2);
                vertexNeighbours[v1].Add(v3);
                vertexNeighbours[v2].Add(v1);
                vertexNeighbours[v2].Add(v3);
                vertexNeighbours[v3].Add(v1);
                vertexNeighbours[v3].Add(v2);
            }

            for (int iter = 0; iter < _iterations; iter++)
            {
                Vector3[] newVertices = (Vector3[])vertices.Clone();

                for (var i = 0; i < vertices.Length; i++)
                {
                    if (vertexNeighbours.ContainsKey(i) && vertexNeighbours[i].Count > 0)
                    {
                        Vector3 average = Vector3.zero;

                        foreach (var neighbour in vertexNeighbours[i])
                        {
                            average += vertices[neighbour];
                        }
                        
                        average /= vertexNeighbours[i].Count;
                        
                        newVertices[i] = Vector3.Lerp(vertices[i], average, _intensity);
                    }
                }

                vertices = newVertices;
            }
            
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }
    }
}