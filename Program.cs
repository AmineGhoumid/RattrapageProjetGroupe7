using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace easysave
{
    // Main program entry point
    class Program
    {
        static void Main(string[] args)
        {
            Thread t = new Thread((ThreadStart)(() =>
            {
                App theApp = new App(); // Start our software, CONTROLLER MVC
                                        // The constructor takes the step
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
    }
}

