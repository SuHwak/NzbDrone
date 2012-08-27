﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NzbDrone.Common
{
    public class EnvironmentProvider
    {
        public const string NZBDRONE_PATH = "NZBDRONE_PATH";
        public const string NZBDRONE_PID = "NZBDRONE_PID";
        public const string ROOT_MARKER = "NzbDrone.Web";

        private static readonly string processName = Process.GetCurrentProcess().ProcessName.ToLower();

        private static readonly EnvironmentProvider instance = new EnvironmentProvider();

        public static bool IsProduction
        {
            get
            {
                if (IsDebug || Debugger.IsAttached) return false;
                if (instance.Version.Revision > 10000) return false; //Official builds will never have such a high revision

                var lowerProcessName = processName.ToLower();
                if (lowerProcessName.Contains("vshost")) return false;
                if (lowerProcessName.Contains("nunit")) return false;
                if (lowerProcessName.Contains("jetbrain")) return false;
                if (lowerProcessName.Contains("resharper")) return false;

                if (instance.StartUpPath.ToLower().Contains("_rawpackage")) return false;

                return true;
            }
        }

        public static bool IsDebug
        {
            get
            {

#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static Guid UGuid { get; set; }

        public virtual bool IsUserInteractive
        {
            get { return Environment.UserInteractive; }
        }

        public virtual string ApplicationPath
        {
            get
            {
                string applicationPath;

                applicationPath = CrawlToRoot(StartUpPath);
                if (!string.IsNullOrWhiteSpace(applicationPath))
                    return applicationPath;


                applicationPath = CrawlToRoot(Environment.CurrentDirectory);
                if (!string.IsNullOrWhiteSpace(applicationPath))
                    return applicationPath;

                applicationPath = CrawlToRoot(StartUpPath);
                if (!string.IsNullOrWhiteSpace(applicationPath))
                    return applicationPath;

                applicationPath = CrawlToRoot(NzbDronePathFromEnviroment);
                if (!string.IsNullOrWhiteSpace(applicationPath))
                    return applicationPath;

                throw new ApplicationException("Can't fine IISExpress folder.");
            }
        }

        public string CrawlToRoot(string dir)
        {
            var directoryInfo = new DirectoryInfo(dir);

            while (!IsRoot(directoryInfo))
            {
                if (directoryInfo.Parent == null) return null;
                directoryInfo = directoryInfo.Parent;
            }

            return directoryInfo.FullName;
        }

        private static bool IsRoot(DirectoryInfo dir)
        {
            return dir.GetDirectories(ROOT_MARKER).Length != 0;
        }

        public virtual string StartUpPath
        {
            get
            {
                return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            }
        }

        public virtual String SystemTemp
        {
            get
            {
                return Path.GetTempPath();
            }
        }

        public virtual Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public virtual DateTime BuildDateTime
        {
            get
            {
                var fileLocation = Assembly.GetCallingAssembly().Location;
                return new FileInfo(fileLocation).CreationTime;
            }
        }

        public virtual int NzbDroneProcessIdFromEnviroment
        {
            get
            {
                var id = Convert.ToInt32(Environment.GetEnvironmentVariable(NZBDRONE_PID));

                if (id == 0)
                    throw new InvalidOperationException("NZBDRONE_PID isn't a valid environment variable.");

                return id;
            }
        }

        public virtual string NzbDronePathFromEnviroment
        {
            get
            {
                return Environment.GetEnvironmentVariable(NZBDRONE_PATH);
            }
        }

        public virtual Version GetOsVersion()
        {
            OperatingSystem os = Environment.OSVersion;
            Version version = os.Version;

            return version;
        }
    }
}