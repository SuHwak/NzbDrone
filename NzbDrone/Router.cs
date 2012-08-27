﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Model;

namespace NzbDrone
{
    public class Router
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ApplicationServer _applicationServer;
        private readonly ServiceProvider _serviceProvider;
        private readonly ConsoleProvider _consoleProvider;
        private readonly EnvironmentProvider _environmentProvider;

        public Router(ApplicationServer applicationServer, ServiceProvider serviceProvider, ConsoleProvider consoleProvider, EnvironmentProvider environmentProvider)
        {
            _applicationServer = applicationServer;
            _serviceProvider = serviceProvider;
            _consoleProvider = consoleProvider;
            _environmentProvider = environmentProvider;
        }

        public void Route(IEnumerable<string> args)
        {

            Route(GetApplicationMode(args));
        }

        public void Route(ApplicationMode applicationMode)
        {
            logger.Info("Application mode: {0}", applicationMode);    

            //TODO:move this outside, it should be one of application modes (ApplicationMode.Service?)
            if (!_environmentProvider.IsUserInteractive)
            {
                _serviceProvider.Run(_applicationServer);
            }
            else
            {
                switch (applicationMode)
                {

                    case ApplicationMode.Console:
                        {
                            _applicationServer.Start();
                            _consoleProvider.WaitForClose();
                            break;
                        }
                    case ApplicationMode.InstallService:
                        {
                            if (_serviceProvider.ServiceExist(ServiceProvider.NZBDRONE_SERVICE_NAME))
                            {
                                _consoleProvider.PrintServiceAlreadyExist();
                            }
                            else
                            {
                                _serviceProvider.Install(ServiceProvider.NZBDRONE_SERVICE_NAME);
                                _serviceProvider.Start(ServiceProvider.NZBDRONE_SERVICE_NAME);
                            }
                            break;
                        }
                    case ApplicationMode.UninstallService:
                        {
                            if (!_serviceProvider.ServiceExist(ServiceProvider.NZBDRONE_SERVICE_NAME))
                            {
                                _consoleProvider.PrintServiceDoestExist();
                            }
                            else
                            {
                                _serviceProvider.UnInstall(ServiceProvider.NZBDRONE_SERVICE_NAME);
                            }

                            break;
                        }
                    default:
                        {
                            _consoleProvider.PrintHelp();
                            break;
                        }
                }
            }
        }

        public static ApplicationMode GetApplicationMode(IEnumerable<string> args)
        {
            if (args == null) return ApplicationMode.Console;

            var cleanArgs = args.Where(c => c != null && !String.IsNullOrWhiteSpace(c)).ToList();
            if (cleanArgs.Count == 0) return ApplicationMode.Console;
            if (cleanArgs.Count != 1) return ApplicationMode.Help;

            var arg = cleanArgs.First().Trim('/', '\\', '-').ToLower();

            if (arg == "i") return ApplicationMode.InstallService;
            if (arg == "u") return ApplicationMode.UninstallService;

            return ApplicationMode.Help;
        }
    }
}
