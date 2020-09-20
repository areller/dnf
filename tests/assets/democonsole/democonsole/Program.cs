using classlib;
using System;
using System.Threading;

namespace democonsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var resetEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (_, __) =>
            {
                resetEvent.Set();
            };

            var foo = new Foo();
            foo.PrintHello();
	        Console.WriteLine(string.Join(" ", args));
            Console.WriteLine("Message A");

            resetEvent.WaitOne();
        }
    }
}
