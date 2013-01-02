﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Ninject;
using NzbDrone.Common;

namespace NzbDrone.Update.Providers
{
    public class UpdateProvider
    {
        private readonly DiskProvider _diskProvider;
        private readonly ServiceProvider _serviceProvider;
        private readonly ProcessProvider _processProvider;
        private readonly EnvironmentProvider _environmentProvider;
        private readonly IISProvider _iisProvider;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public UpdateProvider(DiskProvider diskProvider, ServiceProvider serviceProvider,
            ProcessProvider processProvider, EnvironmentProvider environmentProvider, IISProvider iisProvider)
        {
            _diskProvider = diskProvider;
            _serviceProvider = serviceProvider;
            _processProvider = processProvider;
            _environmentProvider = environmentProvider;
            _iisProvider = iisProvider;
        }

        public UpdateProvider()
        {
        }

        private void Verify(string targetFolder)
        {
            logger.Info("Verifying requirements before update...");

            if (String.IsNullOrWhiteSpace(targetFolder))
                throw new ArgumentException("Target folder can not be null or empty");

            if (!_diskProvider.FolderExists(targetFolder))
                throw new DirectoryNotFoundException("Target folder doesn't exist " + targetFolder);

            logger.Info("Verifying Update Folder");
            if (!_diskProvider.FolderExists(_environmentProvider.GetUpdatePackageFolder()))
                throw new DirectoryNotFoundException("Update folder doesn't exist " + _environmentProvider.GetUpdatePackageFolder());

        }

        public virtual void Start(string targetFolder)
        {
            Verify(targetFolder);
            bool isService = false;

            logger.Info("Stopping all running services");

            if (_serviceProvider.ServiceExist(ServiceProvider.NZBDRONE_SERVICE_NAME)
               && _serviceProvider.IsServiceRunning(ServiceProvider.NZBDRONE_SERVICE_NAME))
            {
                isService = true;
                _serviceProvider.Stop(ServiceProvider.NZBDRONE_SERVICE_NAME);
            }

            //TODO:Should be able to restart service if anything beyond this point fails

            logger.Info("Killing all running processes");
            var processes = _processProvider.GetProcessByName(ProcessProvider.NzbDroneProccessName);
            foreach (var processInfo in processes)
            {
                _processProvider.Kill(processInfo.Id);
            }

            logger.Info("Killing all orphan IISExpress processes");
            _iisProvider.StopServer();

            logger.Info("Creating backup of existing installation");
            _diskProvider.CopyDirectory(targetFolder, _environmentProvider.GetUpdateBackUpFolder());


            logger.Info("Moving update package to target");

            try
            {
                _diskProvider.CopyDirectory(_environmentProvider.GetUpdatePackageFolder(), targetFolder);

                logger.Trace("Deleting Update Package.");
                _diskProvider.DeleteFolder(_environmentProvider.GetUpdatePackageFolder(), true);
            }
            catch (Exception e)
            {
                RollBack(targetFolder);

                foreach (var key in e.Data.Keys)
                {
                    logger.Trace("Key: {0}, Value: {1}", key, e.Data[key]);
                }

                logger.FatalException("Failed to copy upgrade package to target folder.", e);
            }
            finally
            {
                StartNzbDrone(isService, targetFolder);
            }
        }

        private void RollBack(string targetFolder)
        {
            //TODO:this should ignore single file failures.
            logger.Info("Attempting to rollback upgrade");
            _diskProvider.CopyDirectory(_environmentProvider.GetUpdateBackUpFolder(), targetFolder);
        }


        private void StartNzbDrone(bool isService, string targetFolder)
        {
            if (isService)
            {
                _serviceProvider.Start(ServiceProvider.NZBDRONE_SERVICE_NAME);
            }
            else
            {
                _processProvider.Start(Path.Combine(targetFolder, "nzbdrone.exe"));
            }
        }
    }
}
