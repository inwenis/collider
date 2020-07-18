using System;
using System.Diagnostics;
using System.Threading;

namespace WindowsFormsApp1
{
    internal class Progress
    {
        private int _done;
        private readonly int _total;
        private readonly Stopwatch _stopwatch;

        public Progress(int total)
        {
            _total = total;
            _stopwatch = new Stopwatch();
        }

        public void Report()
        {
            Interlocked.Increment(ref _done);

            if (_done % (_total / 100) == 0) // print 100 progress updates
            {
                var elapsed = _stopwatch.Elapsed;
                var progress = (double)_done / _total;
                var tte = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / progress); // total time estimated
                var rem = tte - elapsed; // remaining
                Console.WriteLine($"{progress * 100,3:.}% passed={elapsed} total estimated={tte} remaining={rem}");
            }

        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public static Progress StartNew(int total)
        {
            var progress = new Progress(total);
            progress.Start();
            return progress;
        }
    }
}