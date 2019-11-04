using System;
using System.Threading.Tasks;

namespace MimiTools.Sync
{
    public class MultiBandAsyncSync : AsyncSync
    {
        private readonly AsyncSync[] Bands;

        public MultiBandAsyncSync(int bands)
        {
            Bands = new AsyncSync[bands];
            for (int i = 0; i < bands; i++)
                Bands[i] = new AsyncSync();
        }

        public override Task Execute(Action a)
            => Execute(a, Bands.Length);

        public Task Execute(Action a, int band)
        {
            if (band < 0)
                throw new IndexOutOfRangeException(nameof(band));

            if (band == 0)
                return base.Execute(a);
            band--;

            return Bands[band].ExecuteAsync(() => Execute(a, band));
        }

        public override Task ExecuteAsync(Func<Task> f)
            => ExecuteAsync(f, Bands.Length);

        public Task ExecuteAsync(Func<Task> f, int band)
        {
            if (band < 0)
                throw new IndexOutOfRangeException(nameof(band));

            if (band == 0)
                return base.ExecuteAsync(f);
            band--;

            return Bands[band].ExecuteAsync(() => ExecuteAsync(f, band));
        }
    }
}
