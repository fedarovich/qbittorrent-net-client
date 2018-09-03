using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace QBittorrent.Client.Internal
{
    internal interface IRequestProvider
    {
        IUrlProvider Url { get; }

        (Uri url, HttpContent request) Login(string username, string password);

        (Uri url, HttpContent request) Logout();

        (Uri url, HttpContent request) AddTorrentFiles();

        (Uri url, HttpContent request) AddTorrentUrls();

        (Uri url, HttpContent request) Pause(IEnumerable<string> hashes);

        (Uri url, HttpContent request) PauseAll();

        (Uri url, HttpContent request) Resume(IEnumerable<string> hashes);

        (Uri url, HttpContent request) ResumeAll();

        (Uri url, HttpContent request) AddCategory(string category);

        (Uri url, HttpContent request) DeleteCategories(IEnumerable<string> categories);

        (Uri url, HttpContent request) SetCategory(IEnumerable<string> hashes, string category);

        (Uri url, HttpContent request) GetTorrentDownloadLimit(IEnumerable<string> hashes);

        (Uri url, HttpContent request) SetTorrentDownloadLimit(IEnumerable<string> hashes, long limit);

        (Uri url, HttpContent request) GetTorrentUploadLimit(IEnumerable<string> hashes);

        (Uri url, HttpContent request) SetTorrentUploadLimit(IEnumerable<string> hashes, long limit);

        (Uri url, HttpContent request) GetGlobalDownloadLimit();

        (Uri url, HttpContent request) SetGlobalDownloadLimit(long limit);

        (Uri url, HttpContent request) GetGlobalUploadLimit();

        (Uri url, HttpContent request) SetGlobalUploadLimit(long limit);

        (Uri url, HttpContent request) MinTorrentPriority(IEnumerable<string> hashes);

        (Uri url, HttpContent request) MaxTorrentPriority(IEnumerable<string> hashes);

        (Uri url, HttpContent request) IncTorrentPriority(IEnumerable<string> hashes);

        (Uri url, HttpContent request) DecTorrentPriority(IEnumerable<string> hashes);

        (Uri url, HttpContent request) SetFilePriority(string hash, int fileId, TorrentContentPriority priority);

        (Uri url, HttpContent request) DeleteTorrents(IEnumerable<string> hashes, bool withFiles);

        (Uri url, HttpContent request) SetLocation(IEnumerable<string> hashes, string newLocation);

        (Uri url, HttpContent request) Rename(string hash, string newName);

        (Uri url, HttpContent request) AddTrackers(string hash, IEnumerable<Uri> trackers);

        (Uri url, HttpContent request) Recheck(string hash);

        (Uri url, HttpContent request) ToggleAlternativeSpeedLimits();

        (Uri url, HttpContent request) SetAutomaticTorrentManagement(IEnumerable<string> hashes, bool enabled);

        (Uri url, HttpContent request) SetForceStart(IEnumerable<string> hashes, bool enabled);

        (Uri url, HttpContent request) SetSuperSeeding(IEnumerable<string> hashes, bool enabled);

        (Uri url, HttpContent request) ToggleFirstLastPiecePrioritized(IEnumerable<string> hashes);

        (Uri url, HttpContent request) ToggleSequentialDownload(IEnumerable<string> hashes);

        (Uri url, HttpContent request) SetPreferences(string json);
    }
}
