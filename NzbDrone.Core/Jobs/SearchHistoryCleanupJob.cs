﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using NLog;
using NzbDrone.Core.Helpers;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Jobs
{
    public class SearchHistoryCleanupJob : IJob
    {
        private readonly SearchHistoryProvider _searchHistoryProvider;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public SearchHistoryCleanupJob(SearchHistoryProvider searchHistoryProvider)
        {
            _searchHistoryProvider = searchHistoryProvider;
        }

        public SearchHistoryCleanupJob()
        {
        }

        public string Name
        {
            get { return "Search History Cleanup"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromHours(24); }
        }

        public virtual void Start(ProgressNotification notification, int targetId, int secondaryTargetId)
        {
            Logger.Info("Running search history cleanup.");
            _searchHistoryProvider.Cleanup();
        }
    }
}
