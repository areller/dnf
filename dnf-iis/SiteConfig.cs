using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace dnf_iis
{
    class SiteConfig
    {
        private string _name;
        private string _path;
        private int _port;

        private static readonly Assembly _assembly = Assembly.GetCallingAssembly();

        public SiteConfig(string name, string path, int port)
        {
            _name = name;
            _path = path;
            _port = port;
        }

        public async Task<string> Create(string directory)
        {
            var fullName = Path.Combine(directory, Path.GetRandomFileName() + ".config");

            using var tpl = _assembly.GetManifestResourceStream("dnf_iis.Templates.default.config.tpl");
            using var reader = new StreamReader(tpl);

            using var newFile = new FileStream(fullName, FileMode.Create);
            using var writer = new StreamWriter(newFile);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                {
                    line = ReplaceValues(line);
                    await writer.WriteLineAsync(line);
                }
            }

            return fullName;
        }

        private string ReplaceValues(string line)
        {
            return line
                .Replace("{{sitename}}", _name)
                .Replace("{{sitepath}}", _path)
                .Replace("{{siteport}}", _port.ToString());
        }
    }
}