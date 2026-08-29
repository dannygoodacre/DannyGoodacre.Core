using DannyGoodacre.Primitives;

namespace DannyGoodacre.Functional.Test;

class Program
{
    static void Main(string[] args)
    {
        IResult result = Result.InternalError("Error");

        result.TapFailure(() => Console.WriteLine("failure"));
    }

    static IResult<int> Test()
    {
        // return Result.Success(123);
        return Result.DomainError<int>("foo");
    }
}
