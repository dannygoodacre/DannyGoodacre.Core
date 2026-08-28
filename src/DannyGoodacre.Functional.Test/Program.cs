using System.Diagnostics;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Functional.Test;

class Program
{
    static void Main(string[] args)
    {
        var result = new DomainError("foo");

        Result<int> newResult = Test();

        if (newResult is Success<int> success)
        {
            Console.WriteLine(success.Value);
        }

        string message = newResult switch
        {
            Success<int> success => $"Calculated value: {success.Value}",
            Result<int>.NotFound => "Item not found.",
            Result<int>.Canceled => "Operation canceled.",
            Result<int>.Invalid(var inv) => $"Validation failed",
            Result<int>.DomainError(var err) => $"Domain error",
            Result<int>.Conflict(var err) => $"Conflict",
            Result<int>.InternalError(var err) => $"System error",

            _ => throw new UnreachableException()
        };
    }

    static int Square(int x) => x * x;

    static Result<int> Test()
    {
        return new DomainError("foo");
    }
}
