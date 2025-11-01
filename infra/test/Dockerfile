FROM alpine:3.19
RUN apk add --no-cache bash curl jq
WORKDIR /infra/test
COPY smoke.sh /infra/test/smoke.sh
RUN chmod +x /infra/test/smoke.sh
CMD ["/infra/test/smoke.sh"]
