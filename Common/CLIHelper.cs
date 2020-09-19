using System;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;

namespace Common
{
    public static class CLIHelper
    {
        public static void HandleException(Exception ex, InvocationContext context)
        {
            if (ex is TargetInvocationException tie && tie.InnerException != null)
                ex = tie.InnerException;

            context.Console.Error.WriteLine(ex.Message);
        }

        public static T? ParsePath<T>(ArgumentResult result)
            where T : FileSystemInfo
        {
            var token = result.Tokens.Count switch
            {
                0 => null,
                1 => result.Tokens[0].Value?.Replace("\\", @"\"),
                _ => throw new InvalidOperationException("Unexpected token count.")
            };

            if (string.IsNullOrEmpty(token))
                return null;

            if (!Path.IsPathRooted(token))
                token = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), token));

            if (typeof(T).IsAssignableFrom(typeof(DirectoryInfo)))
            {
                if (!Directory.Exists(token))
                {
                    result.ErrorMessage = $"The directory '{token}' could not be found.";
                    return null;
                }

                return new DirectoryInfo(token) as T;
            }
            else
            {
                if (!File.Exists(token))
                {
                    result.ErrorMessage = $"The file '{token}' could not be found.";
                    return null;
                }

                return new FileInfo(token) as T;
            }
        }

        public static string ParseArguments()
        {
            var cmdLine = Environment.CommandLine;
            if (string.IsNullOrEmpty(cmdLine))
                return string.Empty;

            var separator = cmdLine.IndexOf("-- ");
            if (separator == -1 || separator + 2 >= cmdLine.Length)
                return string.Empty;

            return cmdLine[(separator + 2)..].Trim();
        }
    }
}