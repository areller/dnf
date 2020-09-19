using classlib;
using System;

namespace democonsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = new Foo();
            foo.PrintHello();
            Console.WriteLine("Message A");
            Console.ReadLine();
        }
    }
}
