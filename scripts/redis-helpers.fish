# Shell helpers for trying the server with `redis-cli` from Docker.
# Source this file from fish:
#
#     source scripts/redis-helpers.fish
#
# `redis-cli` runs from the official Redis image, so no local Redis or .NET
# installation is needed. `--raw` keeps array and pub/sub output easy to copy;
# a Redis null reply shows up as an empty line.

set -gx NET codecrafters-redis-net
set -gx MASTER redis-master
set -gx REPLICA1 redis-replica-1
set -gx REPLICA2 redis-replica-2
set -gx REDIS_CLI_IMAGE redis:7-alpine

function rcli
    docker run --rm --network "$NET" "$REDIS_CLI_IMAGE" redis-cli --raw $argv
end

function mcli
    rcli -h "$MASTER" -p 6380 $argv
end

function r1cli
    rcli -h "$REPLICA1" -p 6380 $argv
end

function r2cli
    rcli -h "$REPLICA2" -p 6380 $argv
end

function master_shell
    docker run --rm -it --network "$NET" "$REDIS_CLI_IMAGE" redis-cli --raw -h "$MASTER" -p 6380
end

function master_logs
    docker logs -f "$MASTER"
end

function replica1_logs
    docker logs -f "$REPLICA1"
end

function replica2_logs
    docker logs -f "$REPLICA2"
end
