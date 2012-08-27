using System.Linq;
using System.Web.Hosting;
using NLog;
using NzbDrone.Common;
using NzbDrone.Services.Service.App_Start;

[assembly: WebActivator.PreApplicationStartMethod(typeof(Logging), "PreStart")]

namespace NzbDrone.Services.Service.App_Start
{

    public static class Logging
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void PreStart()
        {
            string logPath = string.Format("C:\\NLog\\{0}\\{1}\\${{shortdate}}-${{logger}}.log", HostingEnvironment.SiteName, new EnvironmentProvider().Version);
            string error = string.Format("C:\\NLog\\{0}\\{1}\\${{shortdate}}_Error.log", HostingEnvironment.SiteName, new EnvironmentProvider().Version);

            LogConfiguration.RegisterUdpLogger();
            LogConfiguration.RegisterFileLogger(logPath, LogLevel.Trace);
            //LogConfiguration.RegisterFileLogger(error, LogLevel.Warn);
            LogConfiguration.Reload();

            logger.Info("Logger has been configured. (App Start)");


        }
    }
}