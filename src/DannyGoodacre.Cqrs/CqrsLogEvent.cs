namespace DannyGoodacre.Cqrs;

/// <summary>
/// Strongly-typed event identifiers for CQRS pipeline logging.
/// </summary>
public enum CqrsLogEvent
{
    // Request Lifecycle (1000 - 1999)
    RequestStarted = 1001,
    RequestSucceeded = 1002,
    RequestCanceledPreExecution = 1003,
    RequestFailed = 1004,
    RequestCanceledMidExecution = 1005,
    RequestFaulted = 1006,
    RequestValidationFailed = 1007,

    // Persistence / SaveChanges (2000 - 2999)
    SaveChangesCanceled = 2001,
    SaveChangesFaulted = 2002,

    // Post-Commit Hooks (3000 - 3999)
    PostCommitHookCanceledPreExecution = 3001,
    PostCommitHookCanceledMidExecution = 3002,
    PostCommitFaulted = 3003,

    // Transactions (4000 - 4999)
    TransactionCanceled = 4001,
    TransactionFaulted = 4002
}
