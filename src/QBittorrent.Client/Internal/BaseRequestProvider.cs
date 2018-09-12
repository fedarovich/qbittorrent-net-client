using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client.Internal
{
    internal abstract class BaseRequestProvider : IRequestProvider
    {
        public abstract IUrlProvider Url { get; }
        
        public abstract ApiLevel ApiLevel { get; }

        public virtual (Uri url, HttpContent request) Login(string username, string password)
        {
            return BuildForm(Url.Login(), 
                ("username", username), 
                ("password", password));
        }

        public virtual (Uri url, HttpContent request) Logout() => BuildForm(Url.Logout());

        public virtual (Uri url, HttpContent request) AddTorrents(AddTorrentFilesRequest request)
        {
            var data = AddTorrentsCore(request);
            foreach (var file in request.TorrentFiles)
            {
                data.AddFile("torrents", file, "application/x-bittorrent");
            }

            return (Url.AddTorrentFiles(), data);
        }

        public virtual (Uri url, HttpContent request) AddTorrents(AddTorrentUrlsRequest request)
        {
            var urls = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri));
            var data = AddTorrentsCore(request)
                .AddValue("urls", urls);

            return (Url.AddTorrentUrls(), data);
        }

        public abstract (Uri url, HttpContent request) AddTorrents(AddTorrentsRequest request);

        protected virtual MultipartFormDataContent AddTorrentsCore(AddTorrentRequestBase request)
        {
            return new MultipartFormDataContent()
                .AddNonEmptyString("savepath", request.DownloadFolder)
                .AddNonEmptyString("cookie", request.Cookie)
                .AddNonEmptyString("category", request.Category)
                .AddValue("skip_checking", request.SkipHashChecking)
                .AddValue("paused", request.Paused)
                .AddNotNullValue("root_folder", request.CreateRootFolder)
                .AddNonEmptyString("rename", request.Rename)
                .AddNotNullValue("upLimit", request.UploadLimit)
                .AddNotNullValue("dlLimit", request.DownloadLimit)
                .AddValue("sequentialDownload", request.SequentialDownload)
                .AddValue("firstLastPiecePrio", request.FirstLastPiecePrioritized);
        }

        public abstract (Uri url, HttpContent request) Pause(IEnumerable<string> hashes);

        public abstract (Uri url, HttpContent request) PauseAll();

        public abstract (Uri url, HttpContent request) Resume(IEnumerable<string> hashes);

        public abstract (Uri url, HttpContent request) ResumeAll();

        public virtual (Uri url, HttpContent request) AddCategory(string category)
        {
            return BuildForm(Url.AddCategory(), 
                ("category", category));
        }

        public virtual (Uri url, HttpContent request) DeleteCategories(IEnumerable<string> categories)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));

            var builder = new StringBuilder(4096);
            foreach (var category in categories)
            {
                if (string.IsNullOrWhiteSpace(category))
                    throw new ArgumentException("The collection must not contain nulls or empty strings.", nameof(categories));

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(category);
            }

            if (builder.Length == 0)
                throw new ArgumentException("The collection must contain at least one category.", nameof(categories));

            var categoriesString = builder.ToString();

            return BuildForm(Url.DeleteCategories(), 
                ("categories", categoriesString));
        }

        public virtual (Uri url, HttpContent request) SetCategory(IEnumerable<string> hashes, string category)
        {
            return BuildForm(Url.SetCategory(),
                ("hashes", JoinHashes(hashes)),
                ("category", category));
        }

        public virtual (Uri url, HttpContent request) GetTorrentDownloadLimit(IEnumerable<string> hashes)
        {
            return BuildForm(Url.GetTorrentDownloadLimit(), 
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) SetTorrentDownloadLimit(IEnumerable<string> hashes, long limit)
        {
            return BuildForm(Url.SetTorrentDownloadLimit(),
                ("hashes", JoinHashes(hashes)),
                ("limit", limit.ToString()));
        }

        public virtual (Uri url, HttpContent request) GetTorrentUploadLimit(IEnumerable<string> hashes)
        {
            return BuildForm(Url.GetTorrentUploadLimit(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) SetTorrentUploadLimit(IEnumerable<string> hashes, long limit)
        {
            return BuildForm(Url.SetTorrentUploadLimit(),
                ("hashes", JoinHashes(hashes)),
                ("limit", limit.ToString()));
        }

        public virtual (Uri url, HttpContent request) GetGlobalDownloadLimit()
        {
            return BuildForm(Url.GetGlobalDownloadLimit());
        }

        public virtual (Uri url, HttpContent request) SetGlobalDownloadLimit(long limit)
        {
            return BuildForm(Url.SetGlobalDownloadLimit(),
                ("limit", limit.ToString()));
        }

        public virtual (Uri url, HttpContent request) GetGlobalUploadLimit()
        {
            return BuildForm(Url.GetGlobalUploadLimit());
        }

        public virtual (Uri url, HttpContent request) SetGlobalUploadLimit(long limit)
        {
            return BuildForm(Url.SetGlobalUploadLimit(),
                ("limit", limit.ToString()));
        }

        public virtual (Uri url, HttpContent request) MinTorrentPriority(IEnumerable<string> hashes)
        {
            return BuildForm(Url.MinTorrentPriority(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) MaxTorrentPriority(IEnumerable<string> hashes)
        {
            return BuildForm(Url.MaxTorrentPriority(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) IncTorrentPriority(IEnumerable<string> hashes)
        {
            return BuildForm(Url.IncTorrentPriority(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) DecTorrentPriority(IEnumerable<string> hashes)
        {
            return BuildForm(Url.DecTorrentPriority(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) SetFilePriority(string hash, int fileId, TorrentContentPriority priority)
        {
            return BuildForm(Url.SetFilePriority(),
                ("hash", hash),
                ("id", fileId.ToString()),
                ("priority", priority.ToString("D")));
        }

        public abstract (Uri url, HttpContent request) DeleteTorrents(IEnumerable<string> hashes, bool withFiles);

        public virtual (Uri url, HttpContent request) SetLocation(IEnumerable<string> hashes, string newLocation)
        {
            return BuildForm(Url.SetLocation(),
                ("hashes", JoinHashes(hashes)),
                ("location", newLocation));
        }

        public virtual (Uri url, HttpContent request) Rename(string hash, string newName)
        {
            return BuildForm(Url.Rename(),
                ("hash", hash),
                ("name", newName));
        }

        public virtual (Uri url, HttpContent request) AddTrackers(string hash, IEnumerable<Uri> trackers)
        {
            var builder = new StringBuilder(4096);
            foreach (var tracker in trackers)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (tracker == null)
                    throw new ArgumentException("The collection must not contain nulls.", nameof(trackers));
                if (!tracker.IsAbsoluteUri)
                    throw new ArgumentException("The collection must contain absolute URIs.", nameof(trackers));

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(tracker.AbsoluteUri);
            }

            if (builder.Length == 0)
                throw new ArgumentException("The collection must contain at least one URI.", nameof(trackers));

            var urls = builder.ToString();

            return BuildForm(Url.AddTrackers(),
                ("hash", hash),
                ("urls", urls));
        }

        public abstract (Uri url, HttpContent request) Recheck(IEnumerable<string> hashes);

        public virtual (Uri url, HttpContent request) ToggleAlternativeSpeedLimits()
        {
            return BuildForm(Url.ToggleAlternativeSpeedLimits());
        }

        public virtual (Uri url, HttpContent request) SetAutomaticTorrentManagement(IEnumerable<string> hashes, bool enabled)
        {
            return BuildForm(Url.SetAutomaticTorrentManagement(),
                ("hashes", JoinHashes(hashes)),
                ("enable", enabled.ToLowerString()));
        }

        public virtual (Uri url, HttpContent request) SetForceStart(IEnumerable<string> hashes, bool enabled)
        {
            return BuildForm(Url.SetForceStart(),
                ("hashes", JoinHashes(hashes)),
                ("value", enabled.ToLowerString()));
        }

        public virtual (Uri url, HttpContent request) SetSuperSeeding(IEnumerable<string> hashes, bool enabled)
        {
            return BuildForm(Url.SetSuperSeeding(),
                ("hashes", JoinHashes(hashes)),
                ("value", enabled.ToLowerString()));
        }

        public virtual (Uri url, HttpContent request) ToggleFirstLastPiecePrioritized(IEnumerable<string> hashes)
        {
            return BuildForm(Url.ToggleFirstLastPiecePrioritized(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) ToggleSequentialDownload(IEnumerable<string> hashes)
        {
            return BuildForm(Url.ToggleSequentialDownload(),
                ("hashes", JoinHashes(hashes)));
        }

        public virtual (Uri url, HttpContent request) SetPreferences(string json)
        {
            return BuildForm(Url.SetPreferences(),
                ("json", json));
        }

        public abstract (Uri url, HttpContent request) Reannounce(IEnumerable<string> hashes);

        protected (Uri, HttpContent) BuildForm(Uri uri, params (string key, string value)[] fields)
        {
            return (uri, new CompatibleFormUrlEncodedContent(fields));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string JoinHashes(IEnumerable<string> hashes) => string.Join("|", hashes);
    }
}
