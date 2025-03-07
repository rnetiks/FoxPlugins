using UnityEngine;
using Random = UnityEngine.Random;

namespace CTRLZDoesntWork.KK
{
    public class NoiseModifier : BaseModifier
    {
        [GUIControllable]
        private float _strength;

        public NoiseModifier(float strength)
        {
            _strength = strength;
        }

        public override void Apply(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += Random.onUnitSphere * _strength;
            }

            mesh.vertices = vertices;
        }
    }
}