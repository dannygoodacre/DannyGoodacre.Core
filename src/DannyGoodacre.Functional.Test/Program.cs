using DannyGoodacre.Primitives;

namespace DannyGoodacre.Functional.Test;

class Program
{
    static void Main(string[] args)
    {
        Result<int> result = Test();

        if (result is DomainError<int> domainError)
        {
            Console.WriteLine(domainError.Error);
        }
    }

    static Result<int> Test()
    {
        return new DomainError<int>("Error message");
    }
}
