using System;
using System.Text.RegularExpressions;

namespace everlaster
{
    static class Utils
    {
        public static string NewGuid(int length = 5) => Guid.NewGuid().ToString().Substring(0, length);
        public static Regex NewRegex(string regexStr) => new Regex(regexStr, RegexOptions.Compiled);
    }
}
