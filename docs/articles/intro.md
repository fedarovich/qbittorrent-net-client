# Introduction

## Setup

### Installing release version

#### [NuGet (PM CONSOLE)](#tab/stable-nuget-pm)

```powershell
Install-Package QBittorrent.Client
```

#### [nuget.exe](#tab/stable-nuget-exe)

```powershell
nuget.exe install QBittorrent.Client
```

#### [.Net CLI](#tab/stable-dotnet)

```powershell
dotnet add package QBittorrent.Client
```

***

### Installing prerelease version from MyGet

#### [NuGet (PM CONSOLE)](#tab/pre-nuget-pm)

```powershell
Install-Package QBittorrent.Client -Source https://www.myget.org/F/fedarovich/api/v3/index.json -IncludePrerelease
```

#### [nuget.exe](#tab/pre-nuget-exe)

```powershell
nuget.exe install QBittorrent.Client -Source https://www.myget.org/F/fedarovich/api/v3/index.json -PreRelease
```

#### [.Net CLI](#tab/pre-dotnet)

```powershell
dotnet add package QBittorrent.Client -v 0.9.0-*
```

***

## Using the library
Create an instance of See documentation on [`QBittorrentClient`](xref:QBittorrent.Client.QBittorrentClient) for more information. class and pass the qBittorrent web interface URL to the constructor.

```cs
var client = new QBittorrentClient(new Uri("http://127.0.0.1:8080/"));
```

If your qBittorrent instance requires authentication call `LoginAsync` method:

```cs
await client.LoginAsync("login", "password");
```

Now you can use the `client` instance to query qBittorrent status, list/add/remove torrents, etc. See documentation on [`QBittorrentClient`](xref:QBittorrent.Client.QBittorrentClient) for more information.

> [!IMPORTANT]
> It is import to reuse the same `client` instance across the application, because it stores cookies required by qBittorrent web API.

Do not forget to dispose the `client` after you has finished your work with it:
```cs
client.Dispose();
```
