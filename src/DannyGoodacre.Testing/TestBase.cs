using System.Reflection;
using DannyGoodacre.Primitives;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Testing;

public abstract class TestBase
{
    [TearDown]
    public void BaseTearDown() => VerifyAllAndNoOtherCalls();

    protected static void AssertSuccess(IResult result)
        => Assert.That(result is Success);

    protected static void AssertSuccess<T>(IResult<T> result, T expectedValue)
    {
        Assert.That(result, Is.InstanceOf<Success<T>>());

        Assert.That(((Success<T>)result).Value, Is.EqualTo(expectedValue).UsingPropertiesComparer());
    }

    protected static void AssertCanceled(IResult result)
        => Assert.That(result, Is.InstanceOf<Canceled>());

    protected static void AssertConflict(IResult result, string error)
    {
        Assert.That(result, Is.InstanceOf<Conflict>());

        Assert.That(((Conflict)result).Message, Is.EqualTo(error));
    }

    protected static void AssertConflict<T>(IResult<T> result, string error)
    {
        Assert.That(result, Is.InstanceOf<Conflict<T>>());

        Assert.That(((Conflict<T>)result).Message, Is.EqualTo(error));
    }

    protected static void AssertDomainError(IResult result, string error)
    {
        Assert.That(result, Is.InstanceOf<DomainError>());

        Assert.That(((DomainError)result).Message, Is.EqualTo(error));
    }

    protected static void AssertDomainError<T>(IResult<T> result, string error)
    {
        Assert.That(result, Is.InstanceOf<DomainError<T>>());

        Assert.That(((DomainError<T>)result).Message, Is.EqualTo(error));
    }

    protected static void AssertInternalError(IResult result, Error error)
    {
        Assert.That(result, Is.InstanceOf<InternalError>());

        Assert.That(((InternalError)result).Error, Is.EqualTo(error));
    }

    protected static void AssertInternalError<T>(IResult<T> result, Error error)
    {
        Assert.That(result, Is.InstanceOf<InternalError<T>>());

        Assert.That(((InternalError<T>)result).Error, Is.EqualTo(error));
    }

    protected static void AssertInvalid(IResult result, ValidationState validationState)
    {
        Assert.That(result, Is.InstanceOf<Invalid>());

        Assert.That(((Invalid)result).ValidationState, Is.EqualTo(validationState).UsingPropertiesComparer());
    }

    protected static void AssertInvalid<T>(IResult<T> result, ValidationState validationState)
    {
        Assert.That(result, Is.InstanceOf<Invalid<T>>());

        Assert.That(((Invalid<T>)result).ValidationState, Is.EqualTo(validationState).UsingPropertiesComparer());
    }

    protected static void AssertNotFound(IResult result)
        => Assert.That(result is NotFound, Is.True);

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
