using MVR.FileManagementSecure;
using System;

namespace everlaster
{
    static class FileUtils
    {
        public static string ParsePackageIdFromPath(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return null;
            }

            int idx = path.IndexOf(":/", StringComparison.Ordinal);
            return idx >= 0 ? path.Substring(0, idx) : null;
        }

        public static bool DirectoryExists(string path) => FileManagerSecure.DirectoryExists(path);
        public static bool FileExists(string path) => !string.IsNullOrEmpty(path) && FileManagerSecure.FileExists(path);
    }
}
