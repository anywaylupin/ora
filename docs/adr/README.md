# Architecture decision records

Each record captures one decision, the context that forced it, and what it costs.
Records are immutable once accepted; a later record supersedes an earlier one instead of editing it.

| Number                                     | Decision                                                     | Status   |
| ------------------------------------------ | ------------------------------------------------------------ | -------- |
| [0001](0001-hot-chocolate-and-relay.md)    | Use Hot Chocolate on the server and Relay on the client      | Accepted |
| [0002](0002-global-ids-and-connections.md) | Expose global object IDs and cursor connections              | Accepted |
| [0003](0003-bearer-tokens.md)              | Authenticate with bearer tokens instead of cookies           | Accepted |
| [0004](0004-integer-minutes.md)            | Store durations as integer minutes                           | Accepted |
| [0005](0005-workspace-isolation.md)        | Enforce workspace isolation in resolvers and in the database | Accepted |
