using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Prototype
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
    }
}