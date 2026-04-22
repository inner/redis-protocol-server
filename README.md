## Redis-inspired RESP protocol server (C#)

- Implemented an in-memory data store using the RESP protocol, inspired by Redis
- Built custom protocol parsing, command execution, and in-memory data structures
- Developed as part of hands-on systems programming practice (Codecrafters)

## Authorship note

The core server implementation was developed without AI assistance as part of Codecrafters practice.
Some later non-implementation work, specifically parts of the automated test suite and documentation, was created or expanded with AI assistance.

This repository contains a Redis-inspired server written in C#.

This README is intentionally Docker-first and `redis-cli`-first. You do not need a local .NET SDK to try the server. If you have Docker installed, you can build the image, start a master with replicas, and copy/paste the examples below exactly as written.

## Scope And Limitations

This is not a full Redis implementation, and it is not a complete RESP-compatible drop-in replacement for real Redis.

- Only a subset of Redis commands is implemented
- Only a subset of RESP-driven client behavior has been exercised
- In practice, the supported surface is closer to a partial Redis-inspired server than full Redis command compatibility

The examples in this README focus on the command set that is currently implemented and tested. Outside of that surface, behavior may be unsupported, incomplete, or intentionally simplified.

## Implementation Notes

The active server path is currently a straightforward `TcpListener` / `TcpClient` / `NetworkStream` implementation. It is functional, but it is not especially performance-oriented.

**This codebase is also intentionally not a polished architecture-heavy implementation. It prioritizes learning and directness over layered interfaces, deep abstractions, and framework-style structure.**

- The current runtime path performs more allocations than necessary
- It relies heavily on string-based parsing and reconstruction
- It does not use a Linux-specific eventing model such as `epoll` behind the scenes
- It should be viewed as a learning-oriented server implementation first, not a production-tuned Redis clone

The repository also contains code under [Redis/Sockets](Redis/Sockets) that is not part of the active runtime path today. That code represents an experimental direction toward a lower-allocation, more asynchronous transport layer. The intention there is to have a foundation for a future refactor if the server is pushed further toward performance work.

The RDB support is also intentionally very limited at the moment. The reader under [Redis/Rdb](Redis/Rdb) is a bare-bones loader rather than a full Redis RDB implementation.

- It mainly supports loading simple string values
- It understands only a narrow subset of RDB opcodes and data types
- It should be treated as minimal persistence/bootstrap support, not broad Redis dump compatibility

## Prerequisites

- Docker

## Build The Image

Run this once from the repository root:

```console
$ docker build -t codecrafters-redis-csharp .
```

## Start A Master And Two Replicas

Create a private Docker network and start one master plus two replicas:

```console
$ docker network create codecrafters-redis-net
$ docker run -d --name redis-master --network codecrafters-redis-net -p 6380:6380 codecrafters-redis-csharp
$ docker run -d --name redis-replica-1 --network codecrafters-redis-net -p 6381:6380 codecrafters-redis-csharp --replicaof "redis-master 6380"
$ docker run -d --name redis-replica-2 --network codecrafters-redis-net -p 6382:6380 codecrafters-redis-csharp --replicaof "redis-master 6380"
```

## Optional Shell Helpers

The helper snippets below are optional. They make the examples much shorter, but they are shell-specific:

- `bash`
- `zsh`
- `fish`

If you do not want shell-specific setup, you can still run the long-form `docker run ... redis-cli ...` commands directly.

### bash / zsh

Paste this snippet into every `bash` or `zsh` shell you want to use for the examples. It runs `redis-cli` from the official Redis Docker image, so you do not need a local Redis installation either.

```sh
NET=codecrafters-redis-net
MASTER=redis-master
REPLICA1=redis-replica-1
REPLICA2=redis-replica-2

rcli() {
  docker run --rm --network "$NET" redis:7-alpine redis-cli --raw "$@"
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
  docker run --rm -it --network "$NET" redis:7-alpine redis-cli --raw -h "$MASTER" -p 6380
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
```

### fish

If you use `fish`, use this version instead:

```fish
set -gx NET codecrafters-redis-net
set -gx MASTER redis-master
set -gx REPLICA1 redis-replica-1
set -gx REPLICA2 redis-replica-2

function rcli
    docker run --rm --network "$NET" redis:7-alpine redis-cli --raw $argv
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
    docker run --rm -it --network "$NET" redis:7-alpine redis-cli --raw -h "$MASTER" -p 6380
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
```

The helpers use `redis-cli --raw` on purpose. That keeps array and pub/sub output easy to copy, but it also means a Redis null reply appears as an empty line.

You can also inspect container logs with:

- `master_logs`
- `replica1_logs`
- `replica2_logs`

## Smoke Test

```console
$ mcli PING
PONG

$ mcli INFO replication
role:master
master_replid:
master_repl_offset:0
connected_slaves:2
second_repl_offset:-1
repl_backlog_active:0
repl_backlog_size:1048576
repl_backlog_first_byte_offset:0
repl_backlog_histlen:
```

## Strings, Keys, And Counters

```console
$ mcli SET greeting hello
OK

$ mcli GET greeting
hello

$ mcli EXISTS greeting
1

$ mcli TYPE greeting
string

$ mcli INCR page-views
1

$ mcli INCR page-views
2

$ mcli SET temp-key short PX 200
OK

$ mcli GET temp-key
short
```

Wait a moment, then:

```console
$ mcli GET temp-key

```

That empty output line is the Redis null reply.

Delete a key:

```console
$ mcli DEL greeting
1

$ mcli EXISTS greeting
0
```

Pattern matching:

```console
$ mcli SET demo:key:one 1
OK

$ mcli SET demo:key:two 2
OK

$ mcli KEYS "demo:key:*"
demo:key:one
demo:key:two
```

## Replication And WAIT

Write on the master, then read from the replicas:

```console
$ mcli SET shared-key replicated
OK

$ r1cli GET shared-key
replicated

$ r2cli GET shared-key
replicated
```

Ask the master to wait until both replicas acknowledge the write:

```console
$ mcli WAIT 2 5000
2
```

## Lists

Push and inspect a list:

```console
$ mcli LPUSH tasks write-docs review-pr merge
3

$ mcli LRANGE tasks 0 -1
merge
review-pr
write-docs

$ mcli LLEN tasks
3

$ mcli LPOP tasks
merge

$ mcli LRANGE tasks 0 -1
review-pr
write-docs
```

Right-push keeps append order:

```console
$ mcli RPUSH queue one two three
3

$ mcli LRANGE queue 0 -1
one
two
three
```

Replication also works for list mutations:

```console
$ mcli RPUSH replicated-list one two three
3

$ r1cli LRANGE replicated-list 0 -1
one
two
three

$ r2cli LRANGE replicated-list 0 -1
one
two
three
```

## BLPOP With Two Terminals

Paste the helper snippet into both terminals first.

**Terminal 1**

```console
$ mcli BLPOP jobs 0
```

The command blocks until another client pushes a value.

**Terminal 2**

```console
$ mcli LPUSH jobs build-readme
1
```

**Back in Terminal 1**

```console
jobs
build-readme
```

## Transactions With One Interactive Session

`MULTI` / `EXEC` / `DISCARD` are connection-scoped, so use a persistent `redis-cli` session.

Start an interactive shell:

```console
$ master_shell
```

Then paste these commands:

```console
MULTI
SET tx-counter 1
INCR tx-counter
EXEC
GET tx-counter
```

Expected output:

```console
OK
QUEUED
QUEUED
OK
2
2
```

Discard a transaction:

```console
MULTI
SET tx-discarded value
DISCARD
GET tx-discarded
```

Expected output:

```console
OK
QUEUED
OK

```

That empty output line is the Redis null reply.

## Streams

Append entries:

```console
$ mcli XADD sensor-stream "*" temperature 21
1760000000000-0

$ mcli XADD sensor-stream "1760000000001-*" temperature 22
1760000000001-1
```

Read a range:

```console
$ mcli XRANGE sensor-stream - +
1760000000000-0
temperature
21
1760000000001-1
temperature
22
```

Read entries after a starting ID:

```console
$ mcli XREAD STREAMS sensor-stream 0-0
sensor-stream
1760000000000-0
temperature
21
1760000000001-1
temperature
22
```

## Blocking XREAD With Two Terminals

Paste the helper snippet into both terminals first.

**Terminal 1**

```console
$ mcli XREAD BLOCK 0 STREAMS live-stream 0-0
```

**Terminal 2**

```console
$ mcli XADD live-stream 500-1 temperature 23
500-1
```

**Back in Terminal 1**

```console
live-stream
500-1
temperature
23
```

## Sorted Sets

Add members with scores:

```console
$ mcli ZADD leaderboard 100 alice
1

$ mcli ZADD leaderboard 200 bob
1

$ mcli ZADD leaderboard 150 charlie
1
```

Inspect ordering:

```console
$ mcli ZRANGE leaderboard 0 -1
alice
charlie
bob

$ mcli ZSCORE leaderboard charlie
150

$ mcli ZRANK leaderboard charlie
1

$ mcli ZCARD leaderboard
3
```

Remove a member:

```console
$ mcli ZREM leaderboard alice
1

$ mcli ZRANGE leaderboard 0 -1
charlie
bob
```

Replication also works for sorted-set writes:

```console
$ mcli ZADD replicated-zset 1 alpha
1

$ mcli ZADD replicated-zset 2 bravo
1

$ r1cli ZRANGE replicated-zset 0 -1
alpha
bravo

$ r2cli ZRANGE replicated-zset 0 -1
alpha
bravo
```

## Pub/Sub With Two Terminals

Paste the helper snippet into both terminals first.

**Terminal 1**

Start an interactive subscriber:

```console
$ master_shell
```

Then paste:

```console
SUBSCRIBE events
```

Expected output:

```console
subscribe
events
1
```

**Terminal 2**

```console
$ mcli PUBLISH events hello
1
```

**Back in Terminal 1**

```console
message
events
hello
```

To unsubscribe in Terminal 1:

```console
UNSUBSCRIBE events
```

Expected output:

```console
unsubscribe
events
0
```

## Stop The Servers

```console
$ docker stop redis-master redis-replica-1 redis-replica-2
```

## Start Them Again Later

```console
$ docker start redis-master redis-replica-1 redis-replica-2
```

## Clean Up Everything

Remove the containers and the network:

```console
$ docker rm -f redis-master redis-replica-1 redis-replica-2
$ docker network rm codecrafters-redis-net
```

Optionally remove the built image too:

```console
$ docker image rm codecrafters-redis-csharp
```
