using System.Collections.Generic;
using System.Linq;
using System;
using NLog;
using Ninject;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;

namespace NzbDrone.Core.Jobs
{
    public class RenameSeriesJob : IJob
    {
        private readonly MediaFileProvider _mediaFileProvider;
        private readonly DiskScanProvider _diskScanProvider;
        private readonly ExternalNotificationProvider _externalNotificationProvider;
        private readonly SeriesProvider _seriesProvider;
        private readonly MetadataProvider _metadataProvider;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Inject]
        public RenameSeriesJob(MediaFileProvider mediaFileProvider, DiskScanProvider diskScanProvider,
                                ExternalNotificationProvider externalNotificationProvider, SeriesProvider seriesProvider,
                                MetadataProvider metadataProvider)
        {
            _mediaFileProvider = mediaFileProvider;
            _diskScanProvider = diskScanProvider;
            _externalNotificationProvider = externalNotificationProvider;
            _seriesProvider = seriesProvider;
            _metadataProvider = metadataProvider;
        }

        public string Name
        {
            get { return "Rename Series"; }
        }

        public TimeSpan DefaultInterval
        {
            get { return TimeSpan.FromTicks(0); }
        }

        public void Start(ProgressNotification notification, int targetId, int secondaryTargetId)
        {
            List<Series> seriesToRename;

            if (targetId <= 0)
            {
                seriesToRename = _seriesProvider.GetAllSeries().ToList();
            }

            else
            {
                seriesToRename = new List<Series>{  _seriesProvider.GetSeries(targetId) };
            }

            foreach(var series in seriesToRename)
            {
                notification.CurrentMessage = String.Format("Renaming episodes for '{0}'", series.Title);

                Logger.Debug("Getting episodes from database for series: {0}", series.SeriesId);
                var episodeFiles = _mediaFileProvider.GetSeriesFiles(series.SeriesId);

                if (episodeFiles == null || episodeFiles.Count == 0)
                {
                    Logger.Warn("No episodes in database found for series: {0}", series.SeriesId);
                    return;
                }

                var newEpisodeFiles = new List<EpisodeFile>();
                var oldEpisodeFiles = new List<EpisodeFile>();

                foreach (var episodeFile in episodeFiles)
                {
                    try
                    {
                        var oldFile = new EpisodeFile(episodeFile);
                        var newFile = _diskScanProvider.MoveEpisodeFile(episodeFile);

                        if (newFile != null)
                        {
                            newEpisodeFiles.Add(newFile);
                            oldEpisodeFiles.Add(oldFile);
                        }
                    }

                    catch (Exception e)
                    {
                        Logger.WarnException("An error has occurred while renaming file", e);
                    }
                }

                //Remove & Create Metadata for episode files
                _metadataProvider.RemoveForEpisodeFiles(oldEpisodeFiles);
                _metadataProvider.CreateForEpisodeFiles(newEpisodeFiles);

                //Start AfterRename

                var message = String.Format("Renamed: Series {0}", series.Title);
                _externalNotificationProvider.AfterRename(message, series);

                notification.CurrentMessage = String.Format("Rename completed for {0}", series.Title);
            }
        }
    }
}