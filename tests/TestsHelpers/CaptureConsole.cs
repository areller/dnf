using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;

namespace TestsHelpers
{
    public class CaptureConsole : IConsole
    {
        class Writer : IStandardStreamWriter
        {
            private bool _error;
            private Action<bool, string> _onMessage;

            public Writer(bool error, Action<bool, string> onMessage)
            {
                _error = error;
                _onMessage = onMessage;
            }

            public void Write(string value)
            {
                _onMessage(_error, value.TrimEnd('\r', '\n'));
            }
        }

        private Action<bool, string> _onMessage;

        public CaptureConsole(Action<bool, string> onMessage)
        {
            _onMessage = onMessage;
        }

        public IStandardStreamWriter Out => new Writer(false, _onMessage);

        public bool IsOutputRedirected => false;

        public IStandardStreamWriter Error => new Writer(true, _onMessage);

        public bool IsErrorRedirected => false;

        public bool IsInputRedirected => false;
    }
}