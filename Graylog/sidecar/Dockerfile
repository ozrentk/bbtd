#########################
# graylog sidecar       #
#########################
FROM debian:trixie-slim

LABEL maintainer "Sami Pajunen <sami.pajunen@digia.com>"

# Update and install some required dependencies
RUN apt-get update && apt-get install -y openssl libapr1 libdbi1 libexpat1 ca-certificates

# Install Graylog Sidecar (Filebeat included)
#ENV SIDECAR_BINARY_URL https://github.com/Graylog2/collector-sidecar/releases/download/0.1.0-rc.1/collector-sidecar_0.1.0-1.rc.1_amd64.deb
#ENV SIDECAR_BINARY_URL https://github.com/Graylog2/collector-sidecar/releases/download/1.4.0/graylog-sidecar_1.4.0-1_amd64.deb
#RUN apt-get install -y --no-install-recommends curl && curl -Lo collector.deb ${SIDECAR_BINARY_URL} && dpkg -i collector.deb && rm collector.deb && apt-get purge -y --auto-remove curl

# RUN wget https://packages.graylog2.org/repo/packages/graylog-sidecar-repository_1-5_all.deb && \
#     sudo dpkg -i graylog-sidecar-repository_1-5_all.deb && \
#     sudo apt-get update && sudo apt-get install graylog-sidecar  

ENV SIDECAR_BINARY_URL https://github.com/Graylog2/collector-sidecar/releases/download/1.4.0/graylog-sidecar_1.4.0-1_amd64.deb
ENV FILEBEAT_BINARY_URL=https://artifacts.elastic.co/downloads/beats/filebeat/filebeat-7.1.1-amd64.deb
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    curl -Lo sidecar.deb ${SIDECAR_BINARY_URL} && \
    dpkg -i sidecar.deb && \
    rm sidecar.deb && \
    curl -Lo filebeat.deb ${FILEBEAT_BINARY_URL} && \
    dpkg -i filebeat.deb && \
    rm filebeat.deb && \
    apt-get purge -y --auto-remove curl

ARG GS_SERVER_URL
ENV GS_SERVER_URL $GS_SERVER_URL

ARG GS_SERVER_API_TOKEN
ENV GS_SERVER_API_TOKEN $GS_SERVER_API_TOKEN

ARG GS_NODE_ID
ENV GS_NODE_ID $GS_NODE_ID

ARG GS_NODE_NAME
ENV GS_NODE_NAME $GS_NODE_NAME

ARG GS_UPDATE_INTERVAL
ENV GS_UPDATE_INTERVAL $GS_UPDATE_INTERVAL

ARG GS_LOG_ROTATE_MAX_FILE_SIZE
ENV GS_LOG_ROTATE_MAX_FILE_SIZE $GS_LOG_ROTATE_MAX_FILE_SIZE

ARG GS_LOG_ROTATE_KEEP_FILES
ENV GS_LOG_ROTATE_KEEP_FILES $GS_LOG_ROTATE_KEEP_FILES

ADD ./data /data

CMD /data/start.sh
