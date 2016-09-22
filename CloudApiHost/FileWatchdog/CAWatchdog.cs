using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using CloudApiHost.AssemblyManager;
using CloudApiHost.ExtensionMethods;
using CloudApiHost.Helper;

namespace CloudApiHost.FileWatchdog
{
    class CAWatchdog : CASingleton<CAWatchdog>
    {
        private static string _lastChecksum;
        private readonly Timer _checkDirectoryTimer;

        private CAWatchdog()
        {
            _lastChecksum = string.Empty;

            _checkDirectoryTimer = new Timer(1000) {AutoReset = true};
            _checkDirectoryTimer.Elapsed += CheckDirectoryTimerOnElapsed;
        }

        public void Run()
        {
            _checkDirectoryTimer.Start();
        }

        private void CheckDirectoryTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var assemblyChecksum = new FileInfo(CAHostConfig.CLOUD_API_ASSEMBLY_PATH + CAHostConfig.CLOUD_API_ASSEMBLY_NAME).CalculateMd5();

            if (_lastChecksum != assemblyChecksum)
            {
                Console.WriteLine("New assembly has been deployed!");
                CAAssemblyLoader.LoadFromFile(CAHostConfig.CLOUD_API_ASSEMBLY_PATH + CAHostConfig.CLOUD_API_ASSEMBLY_NAME);
                _lastChecksum = assemblyChecksum;

                _checkDirectoryTimer.Stop();
            }
        }
    }
}
