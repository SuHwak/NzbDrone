﻿using System.Linq;
using System;
using Ninject;
using NLog;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;

namespace NzbDrone.Core.Jobs
{
    public class DeleteSeriesJob : IJob
    {
        private readonly SeriesProvider _seriesProvider;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public DeleteSeriesJob(SeriesProvider seriesProvider)
        {
            _seriesProvider = seriesProvider;
        }

        public string Name
        {
            get { return "Delete Series"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromTicks(0); }
        }

        public void Start(ProgressNotification notification, int targetId, int secondaryTargetId)
        {
            DeleteSeries(notification, targetId);
        }

        private void DeleteSeries(ProgressNotification notification, int seriesId)
        {
            Logger.Trace("Deleting Series [{0}]", seriesId);

            var title = _seriesProvider.GetSeries(seriesId).Title;

            notification.CurrentMessage = String.Format("Deleting '{0}' from database", title);

            _seriesProvider.DeleteSeries(seriesId);

            notification.CurrentMessage = String.Format("Successfully deleted '{0}' from database", title);
        }
    }
}