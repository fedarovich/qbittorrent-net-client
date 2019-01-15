FROM microsoft/windowsservercore:ltsc2016

COPY *.rss /rss/

RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"

RUN "%ALLUSERSPROFILE%\chocolatey\bin\choco" install qbittorrent -y --version 4.0.4
COPY qBittorrent.ini C:/Users/ContainerAdministrator/AppData/Roaming/qBittorrent/

ENTRYPOINT ["c:/Program Files/qBittorrent/qBittorrent.exe"]
