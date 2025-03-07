using UnityEngine;

namespace CTRLZDoesntWork.KK.Modifiers.GameObject
{
    public class MinMaxPositionModifier : BaseModifier
    {
        private Vector3 _min;
        private Vector3 _max;
        private Vector3 _original;

        public MinMaxPositionModifier(Vector3 min, Vector3 max, Vector3 originalPosition)
        {
            _min = min;
            _max = max;
            _original = originalPosition;
            
        }

        public override void SpecialDestroy(Transform gameObject)
        {
            gameObject.position = _original;
        }

        public override void Update(UnityEngine.GameObject mesh)
        {
            var transformPosition = mesh.transform.position;
            mesh.transform.position = new Vector3(
                Mathf.Clamp(transformPosition.x, _min.x, _max.x),
                Mathf.Clamp(transformPosition.y, _min.y, _max.y),
                Mathf.Clamp(transformPosition.z, _min.z, _max.z)
            );
        }
    }
}