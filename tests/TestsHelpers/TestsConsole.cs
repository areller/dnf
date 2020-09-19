using System.CommandLine;
using System.CommandLine.IO;
using Xunit.Abstractions;

namespace TestsHelpers
{
    public class TestsConsole : IConsole
    {
        class Writer : IStandardStreamWriter
        {
            private ITestOutputHelper _output;

            public Writer(ITestOutputHelper output)
            {
                _output = output;
            }

            public void Write(string value)
            {
                _output.WriteLine(value.TrimEnd('\r', '\n'));
            }
        }

        private ITestOutputHelper _output;

        public TestsConsole(ITestOutputHelper output)
        {
            _output = output;
        }

        public IStandardStreamWriter Out => new Writer(_output);

        public bool IsOutputRedirected => false;

        public IStandardStreamWriter Error => new Writer(_output);

        public bool IsErrorRedirected => false;

        public bool IsInputRedirected => false;
    }
}