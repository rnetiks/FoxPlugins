using UnityEngine;

public class GUITransparent
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static bool Button(Rect rect)
    {
        return rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseUp;
    }

    public static bool Button(int x, int y, int width, int height)
    {
        return Button(new Rect(x, y, width, height));
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static bool Toggle(Rect rect, bool value)
    {
        if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseUp)
            return !value;
        return value;
    }

    public static bool Toggle(int x, int y, int width, int height, bool value)
    {
        return Toggle(new Rect(x, y, width, height), value);
    }
}