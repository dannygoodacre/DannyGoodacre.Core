# TransactionCommandHandler

This handler executes business logic that persists changes to an underlying data store within an explicit transaction boundary in the same standardized manner as the [CommandHandler](./CommandHandler.md).

It also provides an optional mechanism for ensuring data integrity, rolling back changes when the number of changes deviates from what is expected.

The interface `ITransaction` acts as a handle for an active database operation,

## Signature
