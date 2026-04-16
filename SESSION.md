## Deferred Notes

- The `COMMAND` command currently only accepts the `DOCS` subcommand and returns `Invalid command` for anything else, even though the command name suggests broader Redis `COMMAND` introspection support.
- The repository contains an unused `Redis/Sockets` transport built around `SocketAsyncEventArgs`, `ValueTask`, and pipes, while the active server path still uses synchronous `TcpListener` / `NetworkStream` / string-based request handling.
- `dotnet test` is not runnable in the current environment because the testhost expects `Microsoft.NETCore.App 8.0.0`, while only `Microsoft.NETCore.App 10.0.4` is installed.
