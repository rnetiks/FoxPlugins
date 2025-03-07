using UnityEngine;

namespace CTRLZDoesntWork.KK
{
    public abstract class BaseModifier
    {
        public virtual void Apply(Mesh mesh){}
        public virtual void Update(GameObject mesh){}
        public virtual void SpecialDestroy(Transform gameObject){}

        public bool Updatable;

        public bool MeshChanged;
    }
}