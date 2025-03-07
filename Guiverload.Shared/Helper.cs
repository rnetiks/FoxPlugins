using Studio;
using UnityEngine;

namespace Guiverload.KKS
{
    public class Helper
    {
        public static float _minX = float.MaxValue,
            _maxX = float.MinValue,
            _minY = float.MaxValue,
            _maxY = float.MinValue,
            _minZ = float.MaxValue,
            _maxZ = float.MinValue;

        public static void GetBoundsAll(OCIChar selectedCharacter)
        {
            _minX = float.MaxValue;
            _maxX = float.MinValue;
            _minY = float.MaxValue;
            _maxY = float.MinValue;
            _minZ = float.MaxValue;
            _maxZ = float.MinValue;
            var smr = selectedCharacter.charReference.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in smr)
            {
                var component = skinnedMeshRenderer.sharedMesh;

                if (component == null) continue;
                foreach (var meshVertex in component.vertices)
                {
                    if (meshVertex.x > _maxX)
                        _maxX = (float)System.Math.Round(meshVertex.x, 4);
                    if (meshVertex.x < _minX)
                        _minX = (float)System.Math.Round(meshVertex.x, 4);

                    if (meshVertex.y > _maxY)
                        _maxY = (float)System.Math.Round(meshVertex.y, 4);
                    if (meshVertex.y < _minY)
                        _minY = (float)System.Math.Round(meshVertex.y, 4);

                    if (meshVertex.z > _maxZ)
                        _maxZ = (float)System.Math.Round(meshVertex.z, 4);
                    if (meshVertex.z < _minZ)
                        _minZ = (float)System.Math.Round(meshVertex.z, 4);
                }
            }
        }
    }
}