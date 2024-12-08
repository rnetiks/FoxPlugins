using System;
using System.Runtime.CompilerServices;
using Autumn.Attributes;
using g4;
using UnityEngine;

namespace Autumn
{
    public class Test : MonoBehaviour
    {
        /// <summary>
        /// Extends upon <see cref="AutumnRenderContainer"/>
        /// </summary>
        private readonly FoxWindow _element = new FoxWindow();

        Test()
        {
            var mesh = gameObject.GetComponent<MeshFilter>();
        }

        public static DMesh3 UnityMeshToDMesh(Mesh mesh)
        {
            DMesh3 dMesh = new DMesh3();
            foreach (var meshVertex in mesh.vertices)
            {
                dMesh.AppendVertex(new Vector3d(meshVertex.x, meshVertex.y, meshVertex.z));
            }
            
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i++)
            {
                dMesh.AppendTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
            }

            return dMesh;
        }

        private void OnGUI()
        {
            _element.ResetPosition();
            for (int i = 0; i < 20; i++)
            {
                _element.Render();
            }
        }
    }

    public class FoxWindow : AutumnRenderContainer
    {
        [AutumnTexture2D("background")] private Texture2D background;

        public override void Render()
        {
            GUI.DrawTexture(new Rect(X, Y, 160, 380), background);
        }

        [AutumnTexture2D("background")]
        private void _createBackground()
        {
            AutumnClient client = AutumnClient.Create();
            client.CreateNew(e => { e.SetSize((int)e.GetSize.x, (int)e.GetSize.y); }).Build();
        }
    }
}