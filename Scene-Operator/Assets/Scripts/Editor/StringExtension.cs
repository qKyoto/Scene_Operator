using System;

namespace Editor
{
    public static class StringExtension 
    {
        public static string CutText(this string value, string search) => 
            value.Substring(value.IndexOf(search, StringComparison.Ordinal) + search.Length);
    }
}