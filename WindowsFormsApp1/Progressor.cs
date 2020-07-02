using System;
using System.Threading;

namespace WindowsFormsApp1
{
    internal class Progressor
    {
        private int _done;

        public void Report(int total, TimeSpan elapsed)
        {
            Interlocked.Increment(ref _done);

            if (_done % (total / 100) == 0) // print 100 progress updates
            {
                var progress = (double)_done / total;
                var tte = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / progress); // total time estimated
                var rem = tte - elapsed; // remaining
                Console.WriteLine($"{progress * 100,3:.}% passed={elapsed} total estimated={tte} remaining={rem}");
            }

        }
    }
}