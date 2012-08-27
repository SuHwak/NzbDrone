﻿using System.Linq;
using System;
using System.Diagnostics;
using NLog;
using Ninject;

namespace NzbDrone.Common
{
    public class IISProvider
    {
        private static readonly Logger IISLogger = LogManager.GetCurrentClassLogger();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ConfigFileProvider _configFileProvider;
        private readonly ProcessProvider _processProvider;
        private readonly EnvironmentProvider _environmentProvider;


        [Inject]
        public IISProvider(ConfigFileProvider configFileProvider, ProcessProvider processProvider, EnvironmentProvider environmentProvider)
        {
            _configFileProvider = configFileProvider;
            _processProvider = processProvider;
            _environmentProvider = environmentProvider;
        }

        public IISProvider()
        {
        }

        public string AppUrl
        {
            get { return string.Format("http://localhost:{0}/", _configFileProvider.Port); }
        }

        public int IISProcessId { get; private set; }

        public bool ServerStarted { get; private set; }

        public void StartServer()
        {
            Logger.Info("Preparing IISExpress Server...");

            var startInfo = new ProcessStartInfo();

            startInfo.FileName = _environmentProvider.GetIISExe();
            startInfo.Arguments = String.Format("/config:\"{0}\" /trace:i", _environmentProvider.GetIISConfigPath());
            startInfo.WorkingDirectory = _environmentProvider.ApplicationPath;

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;


            startInfo.EnvironmentVariables[EnvironmentProvider.NZBDRONE_PATH] = _environmentProvider.ApplicationPath;
            startInfo.EnvironmentVariables[EnvironmentProvider.NZBDRONE_PID] = Process.GetCurrentProcess().Id.ToString();

            try
            {
                _configFileProvider.UpdateIISConfig(_environmentProvider.GetIISConfigPath());
            }
            catch (Exception e)
            {
                Logger.ErrorException("An error has occurred while trying to update the config file.", e);
            }

            var iisProcess = _processProvider.Start(startInfo);
            IISProcessId = iisProcess.Id;

            iisProcess.OutputDataReceived += (OnOutputDataReceived);
            iisProcess.ErrorDataReceived += (OnErrorDataReceived);

            iisProcess.BeginErrorReadLine();
            iisProcess.BeginOutputReadLine();

            ServerStarted = true;
        }

        private static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e == null || String.IsNullOrWhiteSpace(e.Data))
                return;

            IISLogger.Error(e.Data);
        }


        public void RestartServer()
        {
            ServerStarted = false;
            Logger.Warn("Attempting to restart server.");
            StopServer();
            StartServer();
        }


        public virtual void StopServer()
        {
            _processProvider.Kill(IISProcessId);

            Logger.Info("Finding orphaned IIS Processes.");
            foreach (var process in _processProvider.GetProcessByName("IISExpress"))
            {
                Logger.Info("[{0}]IIS Process found. Path:{1}", process.Id, process.StartPath);
                if (DiskProvider.PathEquals(process.StartPath, _environmentProvider.GetIISExe()))
                {
                    Logger.Info("[{0}]Process is considered orphaned.", process.Id);
                    _processProvider.Kill(process.Id);
                }
                else
                {
                    Logger.Info("[{0}]Process has a different start-up path. skipping.", process.Id);
                }
            }
        }


        private void OnOutputDataReceived(object s, DataReceivedEventArgs e)
        {
            if (e == null || String.IsNullOrWhiteSpace(e.Data) || e.Data.StartsWith("Request started:") ||
                e.Data.StartsWith("Request ended:") || e.Data == ("IncrementMessages called"))
                return;

            Console.WriteLine(e.Data);
        }

    }
}