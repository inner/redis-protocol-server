# Known Issues

Last updated: 2026-11-16.

## Fixed

| Issue | Fix |
|-------|-----|
| Several commands in one TCP read were split with a regex, so values such as `*2\r\n` broke framing, and commands split across reads failed | `RespParser` / `RespReader` frame by declared lengths, buffer partial reads and support inline commands |
| A client closing its side of the connection made the server send `$-1` in an endless loop | End of stream closes the connection |
| Write commands (`LPUSH`, `RPUSH`, `DEL`, `ZADD`, `XADD`, ...) replied on their own inside `EXEC`, before the `EXEC` array | Every reply goes through `Base.Reply`, which stays silent inside `EXEC` and on the replication stream |
| Replicas applied transactions twice (`MULTI`, queued commands, the same commands again, `EXEC` were all propagated) | `EXEC` propagates one `MULTI ... EXEC` block; queued and rejected commands are not propagated |
| Replicas replied to their master for propagated writes, and counted commands from ordinary clients in the replication offset | Replies on the replication stream are suppressed; only master stream bytes advance the offset, after the command runs |
| Bitmap bytes `>= 0x80` were not returned byte-exact by `GET` (UTF-8 wire encoding) | The wire encoding is Latin-1: one char per byte, values are binary-safe |
| `GEOSEARCH` ignored its unit | Radius is converted from `m`, `km`, `mi`, `ft` |
| `GEODIST` ignored its unit and crashed for a missing member | Unit honoured; missing member returns a null reply |
| `AUTH <password>` (used by `redis-cli -a`) crashed | Logs in as `default` |
| A password on `default` only gated `ACL WHOAMI` | All commands except `AUTH` / `QUIT` need a login |
| One login per user: a second connection logging in as `default` logged out the first | Logins are tracked per connection |
| After `ACL SETUSER default >pw`, already open connections were logged out | Connections opened while `default` had no password stay logged in, as in Redis |
| No `masterauth`; replicas skipped auth against a protected master | `--masterauth <password>`; replicas `AUTH` during the handshake |
| Login, `WATCH` and pub/sub state was never removed when a connection closed | Removed in the connection handler's `finally` |
| Pub/sub subscriber lists were mutated from several threads without locking | Lists are locked; `PUBLISH` sends to a snapshot |
| `PSETEX` missing (StackExchange.Redis uses it for timed writes) | Implemented; `SET` also accepts `EX` |
| `COMMAND` only accepted `DOCS` | `COMMAND COUNT` and `COMMAND LIST` added |
| `LPOP` on a missing list sent no reply (client hung) | Replies with null |
| `KEYS` with no match sent two replies | Single empty array |
| `DEL` / `EXISTS` skipped keys starting with `$`; `EXISTS k k` returned 1 | Arguments read by position; duplicates counted |
| `SET` on a replica never replied to a normal client; unknown `SET` options were ignored | Replies; unknown options return `syntax error` |
| Nested `MULTI` returned `OK` | Returns `MULTI calls can not be nested` |
| A replica that went away could fail the client's write while propagating | Propagation errors are logged, not returned |
| `BLPOP`: one push woke every blocked client and all but one got a null reply; `BLPOP` with a timeout slept the full timeout even when data arrived | Each pushed element goes to one waiter, oldest first; others stay blocked; timed waits wake on push |
| `LPOP` of the last element left an empty list behind | Emptied lists are deleted |
| `WAIT` stalled with many replicas: blocking socket reads and a busy-wait loop starved the thread pool | Connections read asynchronously, replica fan-out is inline, `WAIT` yields while waiting |
| Container tests failed instead of skipping without Docker | `[DockerFact]` skips them |
| Test cluster treated replicas as ready before they listened (flaky tests) | Each node must answer `PING` first |
| StackExchange.Redis tests needed a reduced "proxy" configuration because of the pipelining bug | Tests use the client's default configuration |
| Unused `/tmp/redis-rdb` and Windows directory constants | Removed |

## Open

- Bare `COMMAND` and `COMMAND INFO` are not supported.
- A replica that cannot complete its handshake (e.g. wrong `--masterauth`) exits instead of retrying.
- `SET ... PX` / `PSETEX` are logged to the AOF with a relative TTL, so replayed keys live longer than intended (Redis logs `PXAT`).
- `BLPOP` inside `MULTI` blocks instead of behaving like `LPOP`; a client that disconnects while blocked in `BLPOP` can still take a pushed element.
- `SET` options `NX`, `XX`, `GET`, `KEEPTTL`, `EXAT`, `PXAT` are not supported.
- Inline commands do not support quoted arguments.
- `EXISTS` and `KEYS` count keys whose TTL has passed but which were not read since.
- `ServerInfo` is global static state, so master/replica scenarios need separate processes or containers in tests.
- `Redis/Sockets` holds an experimental, unused transport intended for a future lower-allocation refactor.
