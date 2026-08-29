using DannyGoodacre.Primitives;

namespace DannyGoodacre.Functional.Test;

class Program
{
    static void Main(string[] args)
    {
        IResult<int> res1 = Result.Success(123);

        IResult<int> result = Test();

        IResult<int> res2 = Result.Canceled<int>();

        Console.WriteLine(res2);

        IResult<string> res3 = res2.MapFailure<string>();

        Console.WriteLine(res3);

        IResult res4 = res2;

        Console.WriteLine(res4);

        IResult res5 = Result.Canceled<int>();

        // switch (result)
        // {
        //     case Success<int> success:
        //         Console.WriteLine(success.Value);
        //         break;
        //
        //     case DomainError domainError:
        //         Console.WriteLine(domainError.Error);
        //         break;
        // }
    }

    static IResult<int> Test()
    {
        // return Result.Success(123);
        return Result.DomainError<int>("foo");
    }
}
