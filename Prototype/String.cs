using System.Linq;

namespace Prototype
{
    public static class String
    {
        /// <summary>
        /// Determines whether a specified string is either null, empty, or consists solely of whitespace characters.
        /// </summary>
        /// <param name="value">The string to evaluate.</param>
        /// <returns>True if the string is null, empty, or contains only whitespace characters; otherwise, false.</returns>
        public static bool IsEmptyOrWhitespace(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }
    }
}