using UnityEngine;

namespace Crystalize.UIElements
{
    /// <summary>
    /// Represents an abstract base class for immediate mode GUI (IMGUI) elements.
    /// This class provides the foundation for creating custom IMGUI elements,
    /// which can be rendered and interacted with using Unity's immediate mode GUI system.
    /// </summary>
    public abstract class IMGUIElement
    {
        public abstract void OnGUI(Rect rect);
        public virtual Vector2 GetPreferredSize() => new Vector2(200, 200);
    }
}