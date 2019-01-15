FROM ubuntu:artful

COPY *.rss /rss/

RUN set -x \
    && apt update \
    # Install qBittorrent-nox 3.3.7
    && apt install -y qbittorrent-nox \
    && apt clean \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir /scan && mkdir /scan/1 && mkdir /scan/2 && mkdir /scan/3

EXPOSE 8080 6881

CMD ["qbittorrent-nox"]

