﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Jobs
{
    public class SeriesSearchJob : IJob
    {
        private readonly SeasonSearchJob _seasonSearchJob;
        private readonly SeasonProvider _seasonProvider;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SeriesSearchJob(SeasonSearchJob seasonSearchJob,
                                SeasonProvider seasonProvider)
        {
            _seasonSearchJob = seasonSearchJob;
            _seasonProvider = seasonProvider;
        }

        public string Name
        {
            get { return "Series Search"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromTicks(0); }
        }

        public void Start(ProgressNotification notification, dynamic options)
        {
            if (options == null || options.SeriesId <= 0)
                throw new ArgumentException("options.SeriesId");

            logger.Debug("Getting seasons from database for series: {0}", options.SeriesId);
            IList<int> seasons = _seasonProvider.GetSeasons(options.SeriesId);

            foreach (var season in seasons.Where(s => s > 0))
            {
                if (!_seasonProvider.IsIgnored(options.SeriesId, season))
                {
                    _seasonSearchJob.Start(notification, new { SeriesId = options.SeriesId, SeasonNumber = season });
                }
            }
        }
    }
}