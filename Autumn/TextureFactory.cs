using System;
using UnityEngine;

namespace Autumn;

public class TextureFactory
{
    [Flags]
    public enum Border
    {
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
        All = 16
    }

    public static Texture2D SetBorder(Texture2D tex, int distance, Border border)
    {
        if ((border & Border.BottomLeft) != 0)
            tex = BorderBottomLeft(tex, distance);
        if ((border & Border.BottomRight) != 0)
            tex = BorderBottomRight(tex, distance);
        if ((border & Border.TopLeft) != 0)
            tex = BorderTopLeft(tex, distance);
        if ((border & Border.TopRight) != 0)
            tex = BorderTopRight(tex, distance);
        if ((border & Border.All) == Border.All)
        {
            tex = BorderTopLeft(tex, distance);
            tex = BorderTopRight(tex, distance);
            tex = BorderBottomLeft(tex, distance);
            tex = BorderBottomRight(tex, distance);
        }

        return tex;
    }

    public static Texture2D BorderBottomLeft(Texture2D texture, int distance)
    {
        if (distance <= texture.width && distance <= texture.height)
        {
            var point = new Vector2(distance, distance);
            for (int x = 0; x < distance; x++)
            {
                for (int y = 0; y < distance; y++)
                {
                    float dist = Vector2.Distance(point, new Vector2(x, y));
                    if (dist >= distance)
                        texture.SetPixel(x, y, Color.clear);
                }
            }

            texture.Apply();
        }

        return texture;
    }

    public static Texture2D Fill(Texture2D tex, Color c)
    {
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                tex.SetPixel(x, y, c);
            }
        }
        
        tex.Apply();
        return tex;
    }

    public static Texture2D BorderTopLeft(Texture2D texture, int distance)
    {
        if (distance <= texture.width && distance <= texture.height)
        {
            var point = new Vector2(distance, texture.height - distance);
            for (int x = 0; x < distance; x++)
            {
                for (int y = texture.height - distance; y < texture.height; y++)
                {
                    float dist = Vector2.Distance(point, new Vector2(x, y));
                    if (dist >= distance)
                        texture.SetPixel(x, y, Color.clear);
                }
            }

            texture.Apply();
        }

        return texture;
    }

    public static Texture2D BorderTopRight(Texture2D texture, int distance)
    {
        if (distance <= texture.width && distance <= texture.height)
        {
            var point = new Vector2(texture.width - distance, texture.height - distance);
            for (int x = texture.width - distance; x < texture.width; x++)
            {
                for (int y = texture.height - distance; y < texture.height; y++)
                {
                    float dist = Vector2.Distance(point, new Vector2(x, y));
                    if (dist >= distance)
                        texture.SetPixel(x, y, Color.clear);
                }
            }

            texture.Apply();
        }

        return texture;
    }

    public static Texture2D BorderBottomRight(Texture2D texture, int distance)
    {
        if (distance <= texture.width && distance <= texture.height)
        {
            var point = new Vector2(texture.width - distance, distance);
            for (int x = texture.width - distance; x < texture.width; x++)
            {
                for (int y = 0; y < distance; y++)
                {
                    float dist = Vector2.Distance(point, new Vector2(x, y));
                    if (dist >= distance)
                        texture.SetPixel(x, y, Color.clear);
                }
            }

            texture.Apply();
        }

        return texture;
    }
}