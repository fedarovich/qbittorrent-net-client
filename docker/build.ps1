param([String]$version)
docker build -t fedarovich-docker-qbittorrent-cli-docker.bintray.io/qbt-net-test:$version -f $version/Dockerfile .
