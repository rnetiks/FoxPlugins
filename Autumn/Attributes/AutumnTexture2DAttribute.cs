using System;
using System.Reflection;
using Sirenix.Serialization.Utilities;
using UnityEngine;
using File = System.IO.File;

namespace Autumn.Attributes
{
    public class AutumnTexture2DAttribute : Attribute
    {
        private string _texture;

        public AutumnTexture2DAttribute(string textureName)
        {
            _texture = textureName;
            ValidateTexture(Assembly.GetCallingAssembly());
        }

        private void ValidateTexture(object target)
        {
            var fields = target.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<AutumnTexture2DAttribute>();
                if (attribute == null) continue;
                if (field.FieldType != typeof(Texture2D))
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' in '{target.GetType().Name}' must be of type 'Texture' to use the 'AutumnTexture' attribute.");
                }

                if (!File.Exists(_texture)) continue;
                var tempTexture = new Texture2D(1, 1);
                field.SetValue(field, tempTexture.LoadImage(File.ReadAllBytes(_texture)));
            }
        }
    }
}