using System.Linq;

namespace PoseLib.KKS
{
    public static class String
    {
        public static bool IsEmptyOrWhitespace(this string value)
        {
            if (value == null || value.Length == 0)
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }

        public static bool IsEmptyOrWhitespace2(this string value)
        {
            return value.Length == 0 || value.All(char.IsWhiteSpace);
        }
    }
}