using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    class Utilities
    {
        public static bool IsProcessRunning(string processName) {
            return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0;
        }

        public static async Task SetInterval(Action action, TimeSpan timeout) {

            action();

            await Task.Delay(timeout).ConfigureAwait(false); // This should go over the action() call, but for lazyness i prefer to keep it here

            SetInterval(action, timeout);
        }


    }
}
