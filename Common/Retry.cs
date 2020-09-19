using System;
using System.Threading.Tasks;

namespace Common
{
    public static class Retry
    {
        public static Func<int, Task> ConstantTimeBackOff(int milliseconds = 1000) => _ => Task.Delay(milliseconds);

        public static async Task<T> Get<T>(Func<Task<T>> getter, int trials, Func<int, Task> backOffStrategy)
        {
            Exception? lastEx = null;
            for (var i = 0; i < trials; i++)
            {
                try
                {
                    var ret = await getter();
                    return ret;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    await backOffStrategy(i);
                }
            }

            throw lastEx!;
        }

        public static Task Do(Func<Task> doer, int trials, Func<int, Task> backOffStrategy) => Get<bool>(async () =>
        {
            await doer();
            return true;
        }, trials, backOffStrategy);

        public static Task Do(Action doer, int trials, Func<int, Task> backOffStrategy) => Do(() =>
        {
            doer();
            return Task.CompletedTask;
        }, trials, backOffStrategy);
    }
}