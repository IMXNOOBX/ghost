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

        public static bool IsProcessRunning(int processId)
        {
            return System.Diagnostics.Process.GetProcessById(processId) != null;
        }

        public static async Task SetInterval(Action action, TimeSpan timeout) {

            action();

            await Task.Delay(timeout); // This should go over the action() call, but for lazyness i prefer to keep it here

            SetInterval(action, timeout);
        }


    }
}
