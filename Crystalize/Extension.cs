using System.IO;
using System.Reflection;
using UnityEngine;

namespace Crystalize
{
    public static class Extension
    {
        public static Stream GetResourceStream(this Assembly assembly, string resourceName) => assembly.GetManifestResourceStream(resourceName);
        public static byte[] GetResource(this Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;
                byte[] buffer = new byte[stream.Length];
                int read = 0;
                while (read < buffer.Length)
                {
                    read += stream.Read(buffer, read, buffer.Length - read);
                }
                
                return buffer;
            }
        }

        /// <summary>
        /// Draws a line on the screen between two points with the specified color, texture, and width.
        /// </summary>
        /// <param name="start">The starting point of the line.</param>
        /// <param name="end">The ending point of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="tex">The texture to use for the line.</param>
        /// <param name="width">The width of the line. Defaults to 2f.</param>
        public static void DrawLine(Vector2 start, Vector2 end, Color color, Texture2D tex, float width = 2f)
        {
            Vector2 dir = end - start;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Rect lineRect = new Rect(start.x, start.y - width / 2, distance, width);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);

            Color savedColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(lineRect, tex);
            GUI.color = savedColor;

            GUI.matrix = savedMatrix;
        }
    }
}