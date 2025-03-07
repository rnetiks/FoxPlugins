using UnityEngine;

namespace CTRLZDoesntWork.KK.Modifiers.GameObject
{
    public class LockPositionModifier : BaseModifier
    {
        private Vector3 _position;

        public LockPositionModifier(Vector3 position)
        {
            _position = position;
        }

        public override void Update(UnityEngine.GameObject mesh)
        {
            mesh.transform.position = _position;
        }
    }
}