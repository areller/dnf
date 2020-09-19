using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;

namespace TestsHelpers
{
    public class MultiplexerConsole : IConsole
    {
        class Writer : IStandardStreamWriter
        {
            private bool _error;
            private IList<IConsole> _consoles;

            public Writer(bool error, IList<IConsole> consoles)
            {
                _error = error;
                _consoles = consoles;
            }

            public void Write(string value)
            {
                foreach (var console in _consoles)
                {
                    IStandardStreamWriter writer = _error ? console.Error : console.Out;
                    writer.WriteLine(value.TrimEnd('\r', '\n'));
                }
            }
        }

        private IList<IConsole> _consoles;

        public MultiplexerConsole(IList<IConsole> consoles)
        {
            _consoles = consoles;
        }

        public IStandardStreamWriter Out => new Writer(false, _consoles);

        public bool IsOutputRedirected => false;

        public IStandardStreamWriter Error => new Writer(true, _consoles);

        public bool IsErrorRedirected => false;

        public bool IsInputRedirected => false;
    }
}