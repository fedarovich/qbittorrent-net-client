FROM ubuntu:xenial

COPY *.deb /install/

RUN set -x \
    && apt update \
    # Install qBittorrent-nox 4.0.4
    && ls -la /install \
    && apt install -y /install/libtorrent-rasterbar9_1.1.6+git20180215.01c41fadcf+patched-configure-1ppa1~xenial1_amd64.deb \
    && apt install -y /install/qbittorrent-nox_4.0.4.99~201802160102-6283-d2f3d1e~ubuntu16.04.1_amd64.deb \
    && apt clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /install/* 

EXPOSE 8080 6881

CMD ["qbittorrent-nox"]

