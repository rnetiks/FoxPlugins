using UnityEngine;

namespace CTRLZDoesntWork.KK
{
    public class ScaleModifier : BaseModifier
    {
        private Vector3 _scale;

        public ScaleModifier(Vector3 scale)
        {
            _scale = scale;
        }

        public override void Apply(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], _scale);
            }

            mesh.vertices = vertices;
        }
    }
}