using System.Linq;

namespace DefaultNamespace
{
    public class threefive_proc
    {
        public class String
        {
            public static bool IsNullOrWhitespace(string str)
            {
                return str == null || str.All(char.IsWhiteSpace);
            }
        }
    }
}