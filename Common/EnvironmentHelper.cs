using System;
using System.Collections;
using System.Collections.Generic;

namespace Common
{
    public static class EnvironmentHelper
    {
        public static Dictionary<string, string> LoadEnvironmentVariables()
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in Environment.GetEnvironmentVariables())
            {
                dict.Add(
                    ((DictionaryEntry)entry!).Key!.ToString()!,
                    ((DictionaryEntry)entry!).Value!.ToString()!);
            }

            return dict;
        }
    }
}