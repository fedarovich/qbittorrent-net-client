FROM ubuntu:xenial

COPY *.deb /install/
COPY *.rss /rss/

RUN set -x \
    && apt update \
    # Install qBittorrent-nox 4.1.3
    && ls -la /install \
    && apt install -y /install/libtorrent-rasterbar9_1.1.9+git20180915.8cc93e9855+patched-configure-1ppa1_16.04_amd64.deb \
    && apt install -y /install/qbittorrent-nox_4.1.3.99_201809190152-6531-a574c4a_ubuntu16.04.1_amd64.deb \
    && apt clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /install/* \
    && mkdir /scan && mkdir /scan/1 && mkdir /scan/2 && mkdir /scan/3

EXPOSE 8080 6881

CMD ["qbittorrent-nox"]

