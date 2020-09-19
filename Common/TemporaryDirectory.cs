using System;
using System.IO;
using System.Threading.Tasks;

namespace Common
{
    public class TemporaryDirectory : IAsyncDisposable
    {
        public string Value { get; }

        public TemporaryDirectory(string? rootPath = default)
        {
            var dnfDir = Path.Join(Path.GetTempPath(), "dnf");

            Value = Path.Combine(rootPath ?? dnfDir, Path.GetRandomFileName());
            Directory.CreateDirectory(Value);
        }

        public void CopyFrom(string path)
        {
            if (!Directory.Exists(path))
                throw new IOException($"Could not find directory in '{path}'.");

            foreach (var dirPath in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(path, Value));
            }

            foreach (var filePath in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(path, Value), true);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Retry.Do(() => DeleteDirectory(Value), 5, Retry.ConstantTimeBackOff());
        }

        private static void DeleteDirectory(string dir)
        {
            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                DeleteDirectory(sub);
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var fi = new FileInfo(file);
                fi.Attributes = FileAttributes.Normal;
                fi.Delete();
            }

            Directory.Delete(dir);
        }
    }
}