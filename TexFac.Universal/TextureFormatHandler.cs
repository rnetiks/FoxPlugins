using System;
using UnityEngine;

namespace TexFac.Universal
{
    /// <summary>
    /// A utility class that provides functionality for working with pixel formats,
    /// including methods to get format handlers, check format support,
    /// and delegates for setting and retrieving pixel data.
    /// </summary>
    public static unsafe class TextureFormatHandler
    {
        /// <summary>
        /// Represents a method used for setting pixel data within a specific pixel format, allowing manipulation of red, green, blue, and alpha channel values at a given data index.
        /// </summary>
        public delegate void SetPixelDelegate(byte* data, int index, byte r, byte g, byte b, byte a);

        /// <summary>
        /// Represents a delegate that retrieves the RGBA values of a pixel from raw byte data at a specific index.
        /// </summary>
        /// <param name="data">Pointer to the raw byte array containing the pixel data.</param>
        /// <param name="index">The index in the data array where the pixel is located.</param>
        /// <param name="r">Outputs the red component of the pixel.</param>
        /// <param name="g">Outputs the green component of the pixel.</param>
        /// <param name="b">Outputs the blue component of the pixel.</param>
        /// <param name="a">Outputs the alpha component of the pixel.</param>
        public delegate void GetPixelDelegate(byte* data, int index, out byte r, out byte g, out byte b, out byte a);

        /// <summary>
        /// Represents a format handler that provides functionality to work with specific pixel data formats,
        /// including the number of bytes per pixel and delegates for retrieving and setting pixel values.
        /// </summary>
        public class FormatHandler
        {
            /// <summary>
            /// Gets the number of bytes allocated per pixel for the associated pixel format.
            /// This property indicates how much memory is required to store a single pixel
            /// in the specific format that the handler represents.
            /// </summary>
            public int BytesPerPixel { get; private set; }

            /// <summary>
            /// Gets the delegate responsible for obtaining pixel color values (red, green, blue, and alpha)
            /// from raw pixel data at a specified index in a specific pixel format.
            /// </summary>
            public GetPixelDelegate GetPixel { get; private set; }

            /// <summary>
            /// Gets the delegate responsible for setting a pixel's color values in a specific format.
            /// </summary>
            /// <remarks>
            /// This property defines a function delegate that modifies pixel data directly in memory.
            /// The delegate takes a pointer to the pixel data, an index for the pixel's location,
            /// and the red, green, blue, and alpha color component values to be set.
            /// </remarks>
            public SetPixelDelegate SetPixel { get; private set; }

            /// <summary>
            /// Represents a handler for defining pixel format operations such as getting and setting pixel data.
            /// </summary>
            public FormatHandler(int bytesPerPixel, GetPixelDelegate getPixel, SetPixelDelegate setPixel)
            {
                BytesPerPixel = bytesPerPixel;
                GetPixel = getPixel;
                SetPixel = setPixel;
            }
        }

        /// <summary>
        /// Retrieves the appropriate format handler for a specified texture format.
        /// </summary>
        /// <param name="format">The texture format for which the handler is required.</param>
        /// <returns>A <c>FormatHandler</c> that corresponds to the specified texture format.</returns>
        public static FormatHandler GetHandler(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGBA32:
                    return new FormatHandler(4,
                        (byte* data, int index, out byte r, out byte g, out byte b, out byte a) =>
                        {
                            uint pixel = ((uint*)data)[index];
                            r = (byte)(pixel & 0xFF);
                            g = (byte)((pixel >> 8) & 0xFF);
                            b = (byte)((pixel >> 16) & 0xFF);
                            a = (byte)((pixel >> 24) & 0xFF);
                        },
                        (data, index, r, g, b, a) =>
                        {
                            ((uint*)data)[index] = ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
                        }
                    );

                case TextureFormat.ARGB32:
                    return new FormatHandler(
                        4,
                        (byte* data, int index, out byte r, out byte g, out byte b, out byte a) =>
                        {
                            uint pixel = ((uint*)data)[index];
                            a = (byte)(pixel & 0xFF);
                            r = (byte)((pixel >> 8) & 0xFF);
                            g = (byte)((pixel >> 16) & 0xFF);
                            b = (byte)((pixel >> 24) & 0xFF);
                        },
                        (data, index, r, g, b, a) =>
                        {
                            ((uint*)data)[index] = ((uint)b << 24) | ((uint)g << 16) | ((uint)r << 8) | a;
                        }
                    );

                case TextureFormat.RGB24:
                    return new FormatHandler(
                        3,
                        (byte* data, int index, out byte r, out byte g, out byte b, out byte a) =>
                        {
                            int offset = index * 3;
                            r = data[offset];
                            g = data[offset + 1];
                            b = data[offset + 2];
                            a = 255;
                        },
                        (data, index, r, g, b, a) =>
                        {
                            int offset = index * 3;
                            data[offset] = r;
                            data[offset + 1] = g;
                            data[offset + 2] = b;
                        });

                default:
                    throw new ArgumentException($"Texture format {format} is not supported");
            }
        }

        /// <summary>
        /// Determines whether the handler supports the specified texture format.
        /// </summary>
        /// <param name="format">The texture format to check for support.</param>
        /// <returns>Returns true if the texture format is supported; otherwise, false.</returns>
        public static bool IsFormatSupported(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.RGB24:
                    return true;
                default:
                    return false;
            }
        }
    }
}