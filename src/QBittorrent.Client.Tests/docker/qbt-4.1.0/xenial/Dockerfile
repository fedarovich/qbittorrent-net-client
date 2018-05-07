FROM ubuntu:xenial

COPY *.deb /install/

RUN set -x \
    && apt update \
    # Install qBittorrent-nox 4.1.0
    && ls -la /install \
    && apt install -y /install/libtorrent-rasterbar9_1.1.7+git20180503.8808eb7cdd+patched-configure-1ppa1~xenial1_amd64.deb \
    && apt install -y /install/qbittorrent-nox_4.1.0.99~201805061204-6305-e9ed621~ubuntu16.04.1_amd64.deb \
    && apt clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /install/* 

EXPOSE 8080 6881

CMD ["qbittorrent-nox"]

