## Deferred Notes

- The `COMMAND` command currently only accepts the `DOCS` subcommand and returns `Invalid command` for anything else, even though the command name suggests broader Redis `COMMAND` introspection support.
- The repository contains an unused `Redis/Sockets` transport built around `SocketAsyncEventArgs`, `ValueTask`, and pipes, while the active server path still uses synchronous `TcpListener` / `NetworkStream` / string-based request handling.
- `dotnet test` depends on Docker socket access for `ReplicationClusterTests`; in restricted environments it fails with `DockerUnavailableException` / `Permission denied` instead of skipping the container-backed test.
- Multi-node tests are harder than they should be because `ServerInfo` is global static process state; master/replica scenarios currently need separate processes or containers instead of an in-process harness.
- The request parser appears to mishandle multiple RESP commands delivered in one TCP read, which forces `StackExchange.Redis` tests to disable most handshake/topology probing instead of using the default client configuration.
- StackExchange.Redis uses `PSETEX` for timed `StringSetAsync` writes, but the server only supports expiry via `SET ... PX`, so client-compatibility tests need raw `SET` execution until `PSETEX` is implemented.
- `BLPOP` waiter wake-up behavior was inconsistent across list writes because `RPUSH` notified blocked consumers but `LPUSH` initially did not; blocking list behavior depends on both push paths waking waiters.
- `XREAD BLOCK 0 STREAMS <key> 0-0` currently returns a null array instead of waiting for a later `XADD`, so blocking stream reads need further debugging before they can be covered with a stable integration test.
