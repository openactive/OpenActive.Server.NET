using System;
using System.IO;

namespace BookingSystem.AspNetFramework.Helpers
{
    /// <summary>
    /// Copies the SQLite DLLs to the correct location.
    /// (Required when running on Windows.)
    ///
    /// Credit: https://stackoverflow.com/a/62262246
    /// </summary>
    public static class SqliteDll
    {
        private const string RuntimeFolderName = "/runtimes";
        private static bool _copied;

        public static void Initialise()
        {
            if (_copied)
                return;

            var destinationPath = typeof(SQLitePCL.raw).Assembly.Location.Replace("\\", "/");
            var destinationLength = destinationPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var destinationDirectory = $"{destinationPath.Substring(0, destinationLength)}{RuntimeFolderName}";

            var sourcePath = new Uri(typeof(SQLitePCL.raw).Assembly.CodeBase).AbsolutePath;
            var sourceLength = sourcePath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var sourceDirectory = $"{sourcePath.Substring(0, sourceLength)}{RuntimeFolderName}";

            if (!Directory.Exists(sourceDirectory))
                throw new InvalidOperationException("Failed to locate source directory of DLLs");

            CopyFilesRecursively(new DirectoryInfo(sourceDirectory), new DirectoryInfo(destinationDirectory));
            _copied = true;
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

            foreach (var file in source.GetFiles())
            {
                var destinationFile = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(destinationFile))
                    file.CopyTo(destinationFile);
            }
        }
    }
}