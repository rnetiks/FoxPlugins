using System;

using g3;
using UnityEngine;

namespace NonKoiTests
{
    public class BooleanCutout : MonoBehaviour
    {
        public GameObject go1;
        public GameObject go2;

        /// <summary>
        /// The amount of cells used for Marching Cubes, higher values equal higher quality, but take longer to calculate.
        /// Do not go above 99, will cause geometry to break, 16-72 prefered
        /// </summary>
        public int CellNum = 32;

        /// <summary>
        /// Used to reset the original mesh, if livemode is enabled
        /// </summary>
        private Mesh backupMesh;

        public bool Livemode;

        public void Start()
        {
            backupMesh = go1.GetComponent<MeshFilter>().mesh;
        }


        public void ResetMesh()
        {
            go1.GetComponent<MeshFilter>().mesh = backupMesh;
        }

        public void Apply()
        {
            GenerateBooleanCutoutMesh();
        }

        public void SetMeshes(GameObject go1, GameObject go2)
        {
            this.go1 = go1;
            this.go2 = go2;
            backupMesh = go1.GetComponent<MeshFilter>().mesh;
        }

        private Func<DMesh3, int, double, BoundedImplicitFunction3d> meshToImplicitF =
            (meshIn, num_cells, max_offset) =>
            {
                double meshCellsize = meshIn.CachedBounds.MaxDim / num_cells;
                MeshSignedDistanceGrid levelSet = new MeshSignedDistanceGrid(meshIn, meshCellsize);
                levelSet.ExactBandWidth = (int)(max_offset / meshCellsize) + 1;
                levelSet.Compute();
                return new DenseGridTrilinearImplicit(levelSet.Grid, levelSet.GridOrigin, levelSet.CellSize);
            };

        private DMesh3 GenerateMesh(BoundedImplicitFunction3d root, int num_cells)
        {
            MarchingCubes cubes = new MarchingCubes();
            cubes.Implicit = root;
            cubes.RootMode = MarchingCubes.RootfindingModes.LerpSteps;
            cubes.RootModeSteps = 5;
            cubes.Bounds = root.Bounds();
            cubes.CubeSize = cubes.Bounds.MaxDim / num_cells;
            cubes.Bounds.Expand(cubes.CubeSize * 3);
            cubes.Generate();
            MeshNormals.QuickCompute(cubes.Mesh);
            return cubes.Mesh;
        }

        private int iteration = 0;
        private const int COOLDOWN = 60;

        private void FixedUpdate()
        {
            iteration++;
            if (iteration < COOLDOWN)
            {
                return;
            }

            iteration = 0;

            if (!Livemode)
                return;

            GenerateBooleanCutoutMesh();
        }

        private void GenerateBooleanCutoutMesh()
        {
            var go1mf = go1.GetComponent<MeshFilter>();
            if (Livemode)
                go1mf.mesh = backupMesh;
            var mesh1 = UnityToDMesh3(go1);
            var mesh2 = UnityToDMesh3(go2);

            var implicitDifference3d = new ImplicitDifference3d()
            {
                A = meshToImplicitF(mesh1, 64, 0f),
                B = meshToImplicitF(mesh2, 64, 0f)
            };

            var mb = GenerateMesh(implicitDifference3d, CellNum);
            var mesh = DMeshToUnityMesh(mb);
            go1mf.mesh = mesh;
        }

        public static Mesh DMeshToUnityMesh(DMesh3 dMesh)
        {
            Mesh unityMesh = new Mesh();

            int vertexCount = dMesh.VertexCount;
            Vector3[] vertices = new Vector3[vertexCount];
            foreach (int v_id in dMesh.VertexIndices())
            {
                if (v_id >= 0 && v_id < vertexCount)
                {
                    Vector3d vert = dMesh.GetVertex(v_id);
                    vertices[v_id] = new Vector3((float)vert.x, (float)vert.y, (float)vert.z);
                }
            }

            int triangleCount = dMesh.TriangleCount;
            int[] triangles = new int[triangleCount * 3];
            int index = 0;
            foreach (int t_id in dMesh.TriangleIndices())
            {
                Index3i tri = dMesh.GetTriangle(t_id);

                if (tri.a < vertexCount && tri.b < vertexCount && tri.c < vertexCount)
                {
                    triangles[index++] = tri.a;
                    triangles[index++] = tri.b;
                    triangles[index++] = tri.c;
                }
            }

            unityMesh.vertices = vertices;
            unityMesh.triangles = triangles;
            unityMesh.RecalculateNormals();
            unityMesh.RecalculateBounds();

            return unityMesh;
        }

        public static DMesh3 UnityToDMesh3(GameObject go)
        {
            DMesh3 mesh = new DMesh3();
            var tmesh = go.GetComponent<MeshFilter>().mesh;

            Matrix4x4 worldTransform = go.transform.localToWorldMatrix;
            foreach (var tmeshVertex in tmesh.vertices)
            {
                Vector3 worldVertex = worldTransform.MultiplyPoint3x4(tmeshVertex);
                mesh.AppendVertex(new Vector3d(worldVertex.x, worldVertex.y, worldVertex.z));
            }

            int[] triangles = tmesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                mesh.AppendTriangle(triangles[index], triangles[index + 1], triangles[index + 2]);
            }

            return mesh;
        }
    }
}