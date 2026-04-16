# Portfolio Wording

## Short CV Title

`RESP-compatible in-memory data server in C#`

Alternative:

`Redis-inspired RESP server in C#`

## CV Bullets

- Implemented a Redis-inspired wire protocol over raw TCP sockets in C#.
- Built RESP parsing, command dispatch, and stream-oriented connection handling.
- Added master/replica replication behavior, synchronization flow, and replica ACK handling.
- Iterated on command execution architecture to improve correctness across master and replica paths.

## Short Project Summary

This project is a Redis-inspired in-memory data server written in C#. It focuses on systems-oriented concerns rather than framework-heavy application code, including network protocol implementation, low-level socket handling, stream parsing, replication behavior, and correctness across distributed command flows.

## Interview Framing

If asked what makes this project relevant for systems-oriented roles, emphasize:

- network protocol implementation
- low-level socket handling
- stream parsing
- replication
- correctness under distributed behavior

## Notes On Positioning

- Prefer `Redis-inspired` or `RESP-compatible` over `Redis from scratch`.
- Avoid implying feature parity with Redis unless you can defend that in detail.
- Emphasize the protocol, execution model, replication behavior, and architecture tradeoffs.
