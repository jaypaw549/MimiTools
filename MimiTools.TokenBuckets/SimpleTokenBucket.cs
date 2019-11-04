using MimiTools.Extensions.Tasks;
using MimiTools.Sync;
using System;
using System.Threading.Tasks;

namespace MimiTools.TokenBuckets
{
    public class SimpleTokenBucket
    {
        private readonly AsyncSync AsyncLock = new AsyncSync();
        private readonly DateTimeOffset[] Bucket;
        private readonly TimeSpan Cooldown;
        private int Pointer = 0;
        public SimpleTokenBucket(int count, TimeSpan cooldown)
        {
            Bucket = new DateTimeOffset[count];
            DateTimeOffset now = DateTimeOffset.Now - cooldown;
            for (int i = 0; i < count; i++)
                Bucket[i] = now;
            Cooldown = cooldown;
        }

        public bool HasToken { get => AsyncLock.Execute(() => Bucket[Pointer] <= DateTime.UtcNow).WaitAndUnwrapException(); }

        public bool GetToken()
        {
            DateTimeOffset now = default, valid_at = default;

            AsyncLock.Execute(() =>
            {
                now = DateTimeOffset.Now;
                valid_at = Bucket[Pointer];
                if (now >= valid_at)
                    Bucket[Pointer++] = now + Cooldown;
                Pointer %= Bucket.Length;
            }).WaitAndUnwrapException();

            return now >= valid_at;
        }

        public void WaitToken()
            => WaitTokenAsync().WaitAndUnwrapException();

        public async Task WaitTokenAsync()
        {
            TimeSpan wait = default;
            await AsyncLock.Execute(() =>
            {
                DateTimeOffset now = DateTimeOffset.Now;
                DateTimeOffset valid_at = Bucket[Pointer];
                wait = valid_at - now;
                if (wait > TimeSpan.Zero)
                    Bucket[Pointer++] = now + wait + Cooldown;
                else
                    Bucket[Pointer++] = now + Cooldown;
                Pointer %= Bucket.Length;
            });

            if (wait > TimeSpan.Zero)
                await Task.Delay(wait);
        }
    }
}
