# shellcheck shell=bash
#
# Shell helpers for trying the server with `redis-cli` from Docker.
# Source this file from bash or zsh:
#
#     source scripts/redis-helpers.sh
#
# `redis-cli` runs from the official Redis image, so no local Redis or .NET
# installation is needed. `--raw` keeps array and pub/sub output easy to copy;
# a Redis null reply shows up as an empty line.

NET=codecrafters-redis-net
MASTER=redis-master
REPLICA1=redis-replica-1
REPLICA2=redis-replica-2
REDIS_CLI_IMAGE=redis:7-alpine

rcli() {
  docker run --rm --network "$NET" "$REDIS_CLI_IMAGE" redis-cli --raw "$@"
}

mcli() {
  rcli -h "$MASTER" -p 6380 "$@"
}

r1cli() {
  rcli -h "$REPLICA1" -p 6380 "$@"
}

r2cli() {
  rcli -h "$REPLICA2" -p 6380 "$@"
}

master_shell() {
  docker run --rm -it --network "$NET" "$REDIS_CLI_IMAGE" redis-cli --raw -h "$MASTER" -p 6380
}

master_logs() {
  docker logs -f "$MASTER"
}

replica1_logs() {
  docker logs -f "$REPLICA1"
}

replica2_logs() {
  docker logs -f "$REPLICA2"
}
