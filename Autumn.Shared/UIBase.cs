using UnityEngine;

namespace Autumn
{
    public class UIBase : MonoBehaviour
    {
        private void Awake()
        {
            UIManager.SetParent(this);
        }

        private void OnDestroy()
        {
            if (transform != null)
            {
                transform.parent = null;
            }
        }
    }
}