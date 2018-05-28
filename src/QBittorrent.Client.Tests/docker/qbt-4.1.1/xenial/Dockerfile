FROM ubuntu:xenial

COPY *.deb /install/

RUN set -x \
    && apt update \
    # Install qBittorrent-nox 4.1.1
    && ls -la /install \
    && apt install -y /install/libtorrent-rasterbar9_1.1.7+git20180527.6ec6c1b340+patched-configure1-1ppa1~xenial1_amd64.deb \
    && apt install -y /install/qbittorrent-nox_4.1.1.99~201805272202-6377-ea5a290~ubuntu16.04.1_amd64.deb \
    && apt clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /install/* 

EXPOSE 8080 6881

CMD ["qbittorrent-nox"]

