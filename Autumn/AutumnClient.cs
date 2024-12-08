using System;
using UnityEngine;

namespace Autumn
{
    public class AutumnClient
    {
        public static AutumnClient Create()
        {
            return new AutumnClient();
        }

        public AutumnElement CreateNew(Action<AutumnElement> action)
        {
            AutumnElement e = new AutumnElement(100, 100);
            action(e);
            return e;
        }
    }

    /// <summary>
    /// Creates a cached texture
    /// </summary>
    public class AutumnElement
    {
        private int Width, Height;
        public AutumnElement(int? width, int? height)
        {
            Width   = width  ?? Screen.width;
            Height  = height ?? Screen.height;
        }
        
        public void Build(){}

        public Vector2 GetSize => new Vector2(Width, Height);

        public void SetSize(int width, int height)
        {
            Width   = width;
            Height  = height;
        }
    }

    /// <summary>
    /// Container element that can hold multiple and render them at once
    /// </summary>
    public abstract class AutumnRenderContainer
    {
        public int X, Y;

        public abstract void Render();

        public void ResetPosition()
        {
            X = 0;
            Y = 0;
        }
    }
}