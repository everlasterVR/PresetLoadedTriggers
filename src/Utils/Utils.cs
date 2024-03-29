using System;
using System.Text.RegularExpressions;

namespace everlaster
{
    static class Utils
    {
        public static Regex NewRegex(string regexStr) => new Regex(regexStr, RegexOptions.Compiled);
    }
}
