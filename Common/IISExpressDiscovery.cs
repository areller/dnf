using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Common
{
    public static class IISExpressDiscovery
    {
        static readonly string[] Paths = new[]
        {
            @"C:\Program Files\IIS Express\iisexpress.exe",
            @"C:\Program Files (x86)\IIS Express\iisexpress.exe"
        };

        public static async Task<string?> GetIISExpressPath()
        {
            static async Task<bool> IsPath(string path)
            {
                try
                {
                    return (await ProcessUtil.RunAsync(path, "/h", throwOnError: false)).ExitCode == 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new Exception("Detected a non Windows operating system.");

            foreach (var path in Paths)
            {
                if (await IsPath(path))
                    return path;
            }

            throw new Exception("Could not detect an IIS Express executable in the system.");
        }
    }
}