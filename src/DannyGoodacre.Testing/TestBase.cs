using System.Reflection;
using DannyGoodacre.Primitives;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Testing;

public abstract class TestBase
{
    [TearDown]
    public void BaseTearDown() => VerifyAllAndNoOtherCalls();

    protected static void AssertCanceled(IResult result)
        => Assert.That(result is Canceled, Is.True);

    protected static void AssertConflict(IResult result, string error)
        => Assert.That(result, Is.EqualTo(new Conflict(error)));

    protected static void AssertConflict<T>(IResult<T> result, string error)
        => Assert.That(result, Is.EqualTo(new Conflict<T>(error)));

    protected static void AssertDomainError(IResult result, Error error)
        => Assert.That(result, Is.EqualTo(new DomainError(error)));

    protected static void AssertDomainError<T>(IResult<T> result, Error error)
        => Assert.That(result, Is.EqualTo(new DomainError<T>(error)));

    protected static void AssertInternalError(IResult result, Error error)
        => Assert.That(result, Is.EqualTo(new InternalError(error)));

    protected static void AssertInternalError<T>(IResult<T> result, Error error)
        => Assert.That(result, Is.EqualTo(new InternalError<T>(error)));

    protected static void AssertInvalid(IResult result, ValidationState validationState)
        => Assert.That(result, Is.EqualTo(new Invalid(validationState)));

    protected static void AssertNotFound(IResult result)
        => Assert.That(result is NotFound, Is.True);

    protected static void AssertSuccess(IResult result)
        => Assert.That(result is Success, Is.True);

    protected static void AssertSuccess<T>(IResult<T> result, T expectedValue)
        => Assert.That(result, Is.EqualTo(new Success<T>(expectedValue)));

    private void VerifyAllAndNoOtherCalls()
    {
        List<dynamic> mocks = GetAllMocks(this);

        foreach (Mock mock in mocks)
        {
            Type type = mock.GetType();

            MethodInfo? verifyAllMethod = type.GetMethod("VerifyAll", Type.EmptyTypes);

            verifyAllMethod?.Invoke(mock, null);

            MethodInfo? verifyNoOtherCallsMethod = type.GetMethod("VerifyNoOtherCalls", Type.EmptyTypes);

            verifyNoOtherCallsMethod?.Invoke(mock, null);
        }
    }

    private static List<dynamic> GetAllMocks(object instance)
    {
        List<dynamic> mocks = [];

        Type? currentType = instance.GetType();

        while (currentType is not null && currentType != typeof(object))
        {
            const BindingFlags flags = BindingFlags.Instance
                                       | BindingFlags.Public
                                       | BindingFlags.NonPublic
                                       | BindingFlags.DeclaredOnly;

            foreach (FieldInfo field in currentType.GetFields(flags))
            {
                object? value = field.GetValue(instance);

                if (value is Mock)
                {
                    mocks.Add((dynamic)value);
                }
            }

            foreach (PropertyInfo property in currentType.GetProperties(flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;

                object? value = property.GetValue(instance);

                if (value is Mock)
                {
                    mocks.Add((dynamic)value);
                }
            }

            currentType = currentType.BaseType;
        }

        return mocks;
    }
}
