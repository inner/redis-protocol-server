## Build The Image

```console
$ docker build -t codecrafters-redis-csharp .
```

## Run Locally Without Docker

```console
$ ./your_program.sh --port 6380
$ ./your_program.sh --port 6381 --replicaof "localhost 6380"
```

## Start A Master And Two Replicas

```console
$ docker network create codecrafters-redis-net
$ docker run -d --name redis-master --network codecrafters-redis-net -p 6380:6380 codecrafters-redis-csharp
$ docker run -d --name redis-replica-1 --network codecrafters-redis-net -p 6381:6380 codecrafters-redis-csharp --replicaof "redis-master 6380"
$ docker run -d --name redis-replica-2 --network codecrafters-redis-net -p 6382:6380 codecrafters-redis-csharp --replicaof "redis-master 6380"
```

## Shell Helpers

### bash / zsh

```console
$ source scripts/redis-helpers.sh
```

### fish

```console
$ source scripts/redis-helpers.fish
```

### Logs

```console
$ master_logs
$ replica1_logs
$ replica2_logs
```

## Smoke Test

```console
$ mcli PING
PONG

$ mcli ECHO hello
hello

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

$ mcli STRLEN greeting
5

$ mcli INCR page-views
1

$ mcli INCR page-views
2

$ mcli SET temp-key short PX 200
OK

$ mcli GET temp-key
short
```

```console
$ mcli GET temp-key

```

```console
$ mcli DEL greeting
1

$ mcli EXISTS greeting
0
```

```console
$ mcli SET demo:key:one 1
OK

$ mcli SET demo:key:two 2
OK

$ mcli KEYS "demo:key:*"
demo:key:one
demo:key:two
```

## Bitmaps

```console
$ mcli SETBIT flags 7 1
0

$ mcli SETBIT flags 0 1
0

$ mcli GETBIT flags 7
1

$ mcli GETBIT flags 3
0

$ mcli STRLEN flags
1

$ mcli BITCOUNT flags
2
```

```console
$ mcli SET bits-a abc
OK

$ mcli SET bits-b abd
OK

$ mcli BITCOUNT bits-a
10

$ mcli BITCOUNT bits-a 1 -1
7

$ mcli BITOP AND bits-and bits-a bits-b
3

$ mcli GET bits-and
ab`

$ mcli BITOP OR bits-or bits-a bits-b
3

$ mcli GET bits-or
abg

$ mcli BITOP XOR bits-xor bits-a bits-b
3

$ mcli BITCOUNT bits-xor
3

$ mcli BITOP NOT bits-not bits-a
3

$ mcli BITCOUNT bits-not
14
```

```console
$ r1cli GETBIT flags 7
1

$ r1cli GET bits-and
ab`
```

## Replication And WAIT

```console
$ mcli SET shared-key replicated
OK

$ r1cli GET shared-key
replicated

$ r2cli GET shared-key
replicated
```

```console
$ mcli WAIT 2 5000
2
```

## Lists

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

```console
$ mcli RPUSH queue one two three
3

$ mcli LRANGE queue 0 -1
one
two
three
```

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

**Terminal 1**

```console
$ mcli BLPOP jobs 0
```

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

```console
$ master_shell
```

```console
MULTI
SET tx-counter 1
INCR tx-counter
EXEC
GET tx-counter
```

```console
OK
QUEUED
QUEUED
OK
2
2
```

```console
MULTI
SET tx-discarded value
DISCARD
GET tx-discarded
```

```console
OK
QUEUED
OK

```

## WATCH With Two Terminals

```console
$ mcli SET balance 100
OK
```

**Terminal 1**

```console
$ master_shell
```

```console
WATCH balance
MULTI
INCR balance
```

```console
OK
OK
QUEUED
```

**Terminal 2**

```console
$ mcli SET balance 500
OK
```

**Back in Terminal 1**

```console
EXEC
GET balance
```

```console

500
```

```console
WATCH balance
UNWATCH
MULTI
INCR balance
EXEC
```

```console
OK
OK
OK
QUEUED
501
```

## Streams

```console
$ mcli XADD sensor-stream "*" temperature 21
1760000000000-0

$ mcli XADD sensor-stream "1760000000001-*" temperature 22
1760000000001-1
```

```console
$ mcli XRANGE sensor-stream - +
1760000000000-0
temperature
21
1760000000001-1
temperature
22
```

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

```console
$ mcli ZADD leaderboard 100 alice
1

$ mcli ZADD leaderboard 200 bob
1

$ mcli ZADD leaderboard 150 charlie
1
```

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

```console
$ mcli ZREM leaderboard alice
1

$ mcli ZRANGE leaderboard 0 -1
charlie
bob
```

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

## Geospatial

```console
$ mcli GEOADD places 13.361389 38.115556 Palermo
1

$ mcli GEOADD places 15.087269 37.502669 Catania
1

$ mcli GEODIST places Palermo Catania
166274.1516

$ mcli GEOPOS places Palermo
13.361389338970184
38.1155563954963

$ mcli GEOSEARCH places FROMLONLAT 15 37 BYRADIUS 100 km
Catania

$ mcli GEOSEARCH places FROMLONLAT 15 37 BYRADIUS 200 km
Palermo
Catania

$ mcli GEOSEARCH places FROMLONLAT 15 37 BYRADIUS 200000 ft
Catania
```

## Pub/Sub With Two Terminals

**Terminal 1**

```console
$ master_shell
```

```console
SUBSCRIBE events
```

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

```console
UNSUBSCRIBE events
```

```console
unsubscribe
events
0
```

## Configuration

```console
$ mcli CONFIG GET dir
dir
/app

$ mcli CONFIG GET dbfilename
dbfilename
master6380.rdb

$ mcli CONFIG GET appendonly
appendonly
no

$ mcli CONFIG GET appenddirname
appenddirname
appendonlydir

$ mcli CONFIG GET appendfilename
appendfilename
appendonly.aof

$ mcli CONFIG GET appendfsync
appendfsync
everysec
```

## Command Docs

```console
$ mcli COMMAND DOCS SET
SET
summary
Sets the string value of a key, ignoring its type. The key is created if it doesn't exist.
usage #1
redis-cli SET key1 val1
usage #2
redis-cli SET key1 val1 PX 5000
```

```console
$ mcli COMMAND DOCS
```

## ACL And AUTH

**Terminal 1**

```console
$ master_shell
```

```console
ACL WHOAMI
ACL GETUSER default
ACL SETUSER default >s3cret
ACL GETUSER default
```

```console
default
flags
nopass
passwords

OK
flags

passwords
1ec1c26b50d5d3c58d9583181af8076655fe00756bf7285940ba3670f99fcba0
```

**Terminal 2**

```console
$ mcli ACL WHOAMI
NOAUTH Authentication required.

$ mcli GET greeting
NOAUTH Authentication required.

$ mcli AUTH default wrong
WRONGPASS invalid username-password pair or user is disabled.

$ mcli -a s3cret --no-auth-warning ACL WHOAMI
default
```

```console
$ master_shell
```

```console
AUTH default s3cret
ACL WHOAMI
```

```console
OK
default
```

## AOF Persistence

```console
$ docker volume create redis-aof-data
$ docker run -d --name redis-aof --network codecrafters-redis-net -v redis-aof-data:/data codecrafters-redis-csharp --dir /data --appendonly yes
```

```console
$ rcli -h redis-aof -p 6380 CONFIG GET appendonly
appendonly
yes

$ rcli -h redis-aof -p 6380 SET persisted-key survives-restart
OK

$ rcli -h redis-aof -p 6380 RPUSH persisted-list a b c
3

$ rcli -h redis-aof -p 6380 INCR persisted-counter
1

$ docker exec redis-aof ls /data/appendonlydir
appendonly.aof.1.incr.aof
appendonly.aof.manifest

$ docker exec redis-aof cat /data/appendonlydir/appendonly.aof.manifest
file appendonly.aof.1.incr.aof seq 1 type i
```

```console
$ docker restart redis-aof

$ rcli -h redis-aof -p 6380 GET persisted-key
survives-restart

$ rcli -h redis-aof -p 6380 LRANGE persisted-list 0 -1
a
b
c

$ rcli -h redis-aof -p 6380 GET persisted-counter
1
```

```console
$ docker run -d --name redis-aof-always --network codecrafters-redis-net codecrafters-redis-csharp --appendonly yes --appendfsync always --appenddirname my-aof --appendfilename data.aof

$ rcli -h redis-aof-always -p 6380 CONFIG GET appendfsync
appendfsync
always

$ rcli -h redis-aof-always -p 6380 SET k v
OK

$ docker exec redis-aof-always ls my-aof
data.aof.1.incr.aof
data.aof.manifest
```

## Load An RDB File

```console
$ docker volume create redis-rdb-data
$ docker run --rm -v redis-rdb-data:/data redis:7-alpine sh -c 'redis-server --dir /data --dbfilename dump.rdb --daemonize yes && sleep 1 && redis-cli SET rdb-key from-rdb && redis-cli SAVE && redis-cli SHUTDOWN NOSAVE'
$ docker run -d --name redis-rdb --network codecrafters-redis-net -v redis-rdb-data:/data codecrafters-redis-csharp --dir /data --dbfilename dump.rdb
```

```console
$ rcli -h redis-rdb -p 6380 KEYS "*"
rdb-key

$ rcli -h redis-rdb -p 6380 GET rdb-key
from-rdb
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

```console
$ docker rm -f redis-master redis-replica-1 redis-replica-2 redis-aof redis-aof-always redis-rdb
$ docker volume rm redis-aof-data redis-rdb-data
$ docker network rm codecrafters-redis-net
```

```console
$ docker image rm codecrafters-redis-csharp
```
