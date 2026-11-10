namespace DannyGoodacre.Cqrs;

/// <summary>
/// Strongly-typed event identifiers for CQRS pipeline logging.
/// </summary>
public enum CqrsLogEvent
{
    // Request Lifecycle (1000 - 1999)

    /// <summary>The request entered the pipeline. Logged at <c>Information</c>.</summary>
    RequestStarted = 1001,

    /// <summary>The request returned a success result. Logged at <c>Information</c>.</summary>
    RequestSucceeded = 1002,

    /// <summary>The cancellation token was canceled before the request started. Logged at <c>Warning</c>.</summary>
    RequestCanceledPreExecution = 1003,

    /// <summary>The request returned a failure result. Logged at <c>Warning</c> with the result.</summary>
    RequestFailed = 1004,

    /// <summary>The request was canceled while it ran. Logged at <c>Warning</c>.</summary>
    RequestCanceledMidExecution = 1005,

    /// <summary>The request threw an exception. Logged at <c>Error</c> with the exception.</summary>
    RequestFaulted = 1006,

    /// <summary>A validator reported errors. Logged at <c>Warning</c> with the validation state.</summary>
    RequestValidationFailed = 1007,

    // Persistence / SaveChanges (2000 - 2999)

    /// <summary>The save of a state command was canceled. Logged at <c>Warning</c>.</summary>
    SaveChangesCanceled = 2001,

    /// <summary>The save of a state command threw an exception. Logged at <c>Error</c> with the exception.</summary>
    SaveChangesFaulted = 2002,

    // Post-Commit Hooks (3000 - 3999)

    /// <summary>A post-commit hook threw an <see cref="OperationCanceledException"/>. Logged at <c>Warning</c>.</summary>
    PostCommitHookCanceledMidExecution = 3001,

    /// <summary>A post-commit hook threw an exception. Logged at <c>Error</c> with the exception.</summary>
    PostCommitFaulted = 3002,

    /// <summary>
    /// Post-commit hooks ran inside a transaction that the pipeline did not start, so they ran before that
    /// transaction commits. Logged at <c>Warning</c>.
    /// </summary>
    PostCommitHookInUnmanagedTransaction = 3003,

    // Transactions (4000 - 4999)

    /// <summary>The transaction of a transacted state command was canceled. Logged at <c>Warning</c>.</summary>
    TransactionCanceled = 4001,

    /// <summary>The transaction of a transacted state command threw an exception. Logged at <c>Error</c> with the exception.</summary>
    TransactionFaulted = 4002
}
