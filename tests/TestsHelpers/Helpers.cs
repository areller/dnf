﻿using Common;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TestsHelpers
{
    public class Helpers
    {
        public static async Task<TemporaryDirectory> CopyTestAssets(string name)
        {
            var assetPath = Path.Join(Directory.GetCurrentDirectory(), "assets", name);
            if (!Directory.Exists(assetPath))
                throw new IOException($"Could not find the '{name}' test asset.");

            var tempDir = new TemporaryDirectory();
            try
            {
                tempDir.CopyFrom(assetPath);
                return tempDir;
            }
            catch (Exception)
            {
                await tempDir.DisposeAsync();
                throw;
            }
        }

        public static async Task WaitOrTimeout(Task task, int timeoutMilliseconds = 10000)
        {
            var cancel = new CancellationTokenSource();
            var timeout = Task.Delay(timeoutMilliseconds, cancel.Token);
            var firstToFinish = await Task.WhenAny(task, timeout);
            if (firstToFinish == timeout)
                throw new Exception("timeout");

            cancel.Cancel();
        }

        public static async Task ShouldTimeout(Task task, int timeoutMilliseconds = 10000)
        {
            var cancel = new CancellationTokenSource();
            var timeout = Task.Delay(timeoutMilliseconds, cancel.Token);
            var firstToFinish = await Task.WhenAny(task, timeout);
            if (firstToFinish == task)
            {
                cancel.Cancel();
                throw new Exception("didn't timeout");
            }
        }

        public static int CreateRandomPort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }
}
