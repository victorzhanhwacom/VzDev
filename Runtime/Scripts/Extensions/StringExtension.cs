using System.Runtime.CompilerServices;

namespace VzDev.Extensions
{
    public static class StringExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValueExist(this string self)
        {
            if (ReferenceEquals(self, null)) return false;
            
            int len = self.Length;
            if (len == 0) return false;

            // Scan through characters to avoid the string allocation caused by Trim()
            for (int i = 0; i < len; i++)
            {
                if (!char.IsWhiteSpace(self[i]))
                {
                    return true; // Found a valid character, string is neither empty nor just whitespaces
                }
            }
            return false;
        }
    }
}