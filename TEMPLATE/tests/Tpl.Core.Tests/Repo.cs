using System;
using System.IO;
using Xunit;

namespace Tpl.Tests
{
    /// <summary>Finds files in the repo from the test output folder.</summary>
    static class Repo
    {
        public static string Root()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "native", "include", "tpl.h")))
                dir = Path.GetDirectoryName(dir);
            Assert.NotNull(dir);
            return dir!;
        }

        public static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root(), Path.Combine(parts)));
    }
}
