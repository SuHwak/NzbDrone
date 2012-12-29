using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninject;
using NLog;
using NzbDrone.Core.Helpers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using PetaPoco;
using NzbDrone.Common;

namespace NzbDrone.Core.Providers
{
    public class MediaFileProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ConfigProvider _configProvider;
        private readonly IDatabase _database;
        private readonly EpisodeProvider _episodeProvider;

        [Inject]
        public MediaFileProvider(EpisodeProvider episodeProvider, ConfigProvider configProvider, IDatabase database)
        {
            _episodeProvider = episodeProvider;
            _configProvider = configProvider;
            _database = database;
        }

        public MediaFileProvider()
        {
        }

        public virtual int Add(EpisodeFile episodeFile)
        {
            return Convert.ToInt32(_database.Insert(episodeFile));
        }

        public virtual void Update(EpisodeFile episodeFile)
        {
            _database.Update(episodeFile);
        }

        public virtual void Delete(int episodeFileId)
        {
            _database.Delete<EpisodeFile>(episodeFileId);
        }

        public virtual bool Exists(string path)
        {
            return _database.Exists<EpisodeFile>("WHERE Path =@0", path.NormalizePath());
        }

        public virtual EpisodeFile GetFileByPath(string path)
        {
            return _database.SingleOrDefault<EpisodeFile>("WHERE Path =@0", path.NormalizePath());
        }

        public virtual EpisodeFile GetEpisodeFile(int episodeFileId)
        {
            return _database.Single<EpisodeFile>(episodeFileId);
        }

        public virtual List<EpisodeFile> GetEpisodeFiles()
        {
            return _database.Fetch<EpisodeFile>();
        }

        public virtual IList<EpisodeFile> GetSeriesFiles(int seriesId)
        {
            return _database.Fetch<EpisodeFile>("WHERE SeriesId= @0", seriesId);
        }

        public virtual IList<EpisodeFile> GetSeasonFiles(int seriesId, int seasonNumber)
        {
            return _database.Fetch<EpisodeFile>("WHERE SeriesId= @0 AND SeasonNumber = @1", seriesId, seasonNumber);
        }

        public virtual Tuple<int, int> GetEpisodeFilesCount(int seriesId)
        {
            var allEpisodes = _episodeProvider.GetEpisodeBySeries(seriesId).ToList();

            var episodeTotal = allEpisodes.Where(e => !e.Ignored && e.AirDate != null && e.AirDate <= DateTime.Today).ToList();
            var avilableEpisodes = episodeTotal.Where(e => e.EpisodeFileId > 0).ToList();

            return new Tuple<int, int>(avilableEpisodes.Count, episodeTotal.Count);
        }

        public virtual FileInfo CalculateFilePath(Series series, int seasonNumber, string fileName, string extention)
        {
            string path = series.Path;
            if (series.SeasonFolder)
            {
                var seasonFolder = _configProvider.SortingSeasonFolderFormat
                    .Replace("%0s", seasonNumber.ToString("00"))
                    .Replace("%s", seasonNumber.ToString());

                path = Path.Combine(path, seasonFolder);
            }

            path = Path.Combine(path, fileName + extention);

            return new FileInfo(path);
        }

        public virtual void CleanUpDatabase()
        {
            Logger.Trace("Verifying Episode > Episode file relationships.");

            string updateString = "UPDATE Episodes SET EpisodeFileId = 0, GrabDate = NULL, PostDownloadStatus = 0";

            if (_configProvider.AutoIgnorePreviouslyDownloadedEpisodes)
            {
                updateString += ", Ignored = 1";
            }

            var updated = _database.Execute(updateString +
                                                             @"WHERE EpisodeFileId IN
                                                             (SELECT Episodes.EpisodeFileId FROM Episodes
                                                             LEFT OUTER JOIN EpisodeFiles
                                                             ON Episodes.EpisodeFileId = EpisodeFiles.EpisodeFileId
                                                             WHERE Episodes.EpisodeFileId > 0 AND EpisodeFiles.EpisodeFileId IS NULL)");

            if (updated > 0)
            {
                Logger.Debug("Removed {0} invalid links to episode files.", updated);
            }

            Logger.Trace("Deleting orphan files.");

            updated = _database.Execute(@"DELETE FROM EpisodeFiles
                                WHERE EpisodeFileId IN
                                (SELECT EpisodeFiles.EpisodeFileId FROM EpisodeFiles
                                LEFT OUTER JOIN Episodes
                                ON EpisodeFiles.EpisodeFileId = Episodes.EpisodeFileId
                                WHERE Episodes.EpisodeFileId IS NULL)");

            if (updated > 0)
            {
                Logger.Debug("Removed {0} orphan file(s) from database.", updated);
            }
        }

        public virtual string GetNewFilename(IList<Episode> episodes, Series series, QualityTypes quality, bool proper, EpisodeFile episodeFile)
        {
            if (_configProvider.SortingUseSceneName)
            {
                Logger.Trace("Attempting to use scene name");
                if (String.IsNullOrWhiteSpace(episodeFile.SceneName))
                {
                    var name = Path.GetFileNameWithoutExtension(episodeFile.Path);
                    Logger.Trace("Unable to use scene name, because it is null, sticking with current name: {0}", name);

                    return name;
                }

                return episodeFile.SceneName;
            }

            var sortedEpisodes = episodes.OrderBy(e => e.EpisodeNumber);

            var separatorStyle = EpisodeSortingHelper.GetSeparatorStyle(_configProvider.SortingSeparatorStyle);
            var numberStyle = EpisodeSortingHelper.GetNumberStyle(_configProvider.SortingNumberStyle);

            var episodeNames = new List<String>();

            episodeNames.Add(Parser.CleanupEpisodeTitle(sortedEpisodes.First().Title));

            string result = String.Empty;

            if (_configProvider.SortingIncludeSeriesName)
            {
                result += series.Title + separatorStyle.Pattern;
            }

            if(!series.IsDaily)
            {
                result += numberStyle.Pattern.Replace("%0e",
                                                      String.Format("{0:00}", sortedEpisodes.First().EpisodeNumber));

                if(episodes.Count > 1)
                {
                    var multiEpisodeStyle =
                            EpisodeSortingHelper.GetMultiEpisodeStyle(_configProvider.SortingMultiEpisodeStyle);

                    foreach(var episode in sortedEpisodes.Skip(1))
                    {
                        if(multiEpisodeStyle.Name == "Duplicate")
                        {
                            result += separatorStyle.Pattern + numberStyle.Pattern;
                        }
                        else
                        {
                            result += multiEpisodeStyle.Pattern;
                        }

                        result = result.Replace("%0e", String.Format("{0:00}", episode.EpisodeNumber));
                        episodeNames.Add(Parser.CleanupEpisodeTitle(episode.Title));
                    }
                }

                result = result
                        .Replace("%s", String.Format("{0}", episodes.First().SeasonNumber))
                        .Replace("%0s", String.Format("{0:00}", episodes.First().SeasonNumber))
                        .Replace("%x", numberStyle.EpisodeSeparator)
                        .Replace("%p", separatorStyle.Pattern);
            }

            else
            {
                if(episodes.First().AirDate.HasValue)
                    result += episodes.First().AirDate.Value.ToString("yyyy-MM-dd");

                else
                    result += "Unknown";
            }

            if (_configProvider.SortingIncludeEpisodeTitle)
            {
                if (episodeNames.Distinct().Count() == 1)
                    result += separatorStyle.Pattern + episodeNames.First();

                else
                    result += separatorStyle.Pattern + String.Join(" + ", episodeNames.Distinct());
            }

            if (_configProvider.SortingAppendQuality)
            {
                result += String.Format(" [{0}]", quality);

                if (proper)
                    result += " [Proper]";
            }

            if (_configProvider.SortingReplaceSpaces)
                result = result.Replace(' ', '.');

            Logger.Trace("New File Name is: [{0}]", result.Trim());
            return CleanFilename(result.Trim());
        }

        public virtual void ChangeQuality(int episodeFileId, QualityTypes quality)
        {
            _database.Execute("UPDATE EpisodeFiles SET Quality = @quality WHERE EpisodeFileId = @episodeFileId", new { episodeFileId, quality });
        }

        public virtual void ChangeQuality(int seriesId, int seasonNumber, QualityTypes quality)
        {
            _database.Execute("UPDATE EpisodeFiles SET Quality = @quality WHERE SeriesId = @seriesId AND SeasonNumber = @seasonNumber", new { seriesId, seasonNumber, quality });
        }

        public static string CleanFilename(string name)
        {
            string result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", ":", "|", "\"" };
            string[] goodCharacters = { "+", "+", "{", "}", "!", "@", "-", "#", "`" };

            for (int i = 0; i < badCharacters.Length; i++)
                result = result.Replace(badCharacters[i], goodCharacters[i]);

            return result.Trim();
        }
    }
}