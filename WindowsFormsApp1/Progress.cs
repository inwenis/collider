using System;
using System.Diagnostics;
using System.Threading;

namespace WindowsFormsApp1
{
    internal class Progress
    {
        private int _done;
        private readonly Stopwatch _stopwatch;

        public Progress()
        {
            _stopwatch = new Stopwatch();
        }

        public void Report(int total)
        {
            Interlocked.Increment(ref _done);

            if (_done % (total / 100) == 0) // print 100 progress updates
            {
                var elapsed = _stopwatch.Elapsed;
                var progress = (double)_done / total;
                var tte = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / progress); // total time estimated
                var rem = tte - elapsed; // remaining
                Console.WriteLine($"{progress * 100,3:.}% passed={elapsed} total estimated={tte} remaining={rem}");
            }

        }

        public void Start()
        {
            _stopwatch.Start();
        }
    }
}