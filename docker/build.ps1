param([String]$version)
docker build -t ghcr.io/fedarovich/qbt-net-test:$version -f $version/Dockerfile .
