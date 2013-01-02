﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Ninject;
using NzbDrone.Core.Model;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Jobs
{
    public class PastWeekBacklogSearchJob : IJob
    {
        private readonly EpisodeProvider _episodeProvider;
        private readonly EpisodeSearchJob _episodeSearchJob;
        private readonly ConfigProvider _configProvider;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public PastWeekBacklogSearchJob(EpisodeProvider episodeProvider, EpisodeSearchJob episodeSearchJob,
                                            ConfigProvider configProvider)
        {
            _episodeProvider = episodeProvider;
            _episodeSearchJob = episodeSearchJob;
            _configProvider = configProvider;
        }

        public string Name
        {
            get { return "Past Week Backlog Search"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromTicks(0); }
        }

        public void Start(ProgressNotification notification, dynamic options)
        {
            var missingEpisodes = GetMissingForEnabledSeries();

            Logger.Debug("Processing missing episodes from the past week, count: {0}", missingEpisodes.Count);
            foreach (var episode in missingEpisodes)
            {
                _episodeSearchJob.Start(notification, new { EpisodeId = episode.EpisodeId });
            }
        }

        public List<Episode> GetMissingForEnabledSeries()
        {
            return _episodeProvider.EpisodesWithoutFiles(true).Where(e =>
                                                                                e.AirDate >= DateTime.Today.AddDays(-7) &&
                                                                                e.Series.Monitored
                                                                            ).ToList();
        }
    }
}