param([String]$version)
docker build -t docker.pkg.github.com/fedarovich/qbittorrent-net-client/qbt-net-test:$version -f $version/Dockerfile .
