using System.Reflection;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Testing;

public abstract class TestBase
{
    [TearDown]
    public void BaseTearDown() => VerifyAllAndNoOtherCalls();

    protected static void AssertSuccess(Result result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);

            Assert.That(result.Status, Is.EqualTo(Status.Success));
        }
    }

    protected static void AssertSuccess<T>(Result<T> result, T expectedValue)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);

            Assert.That(result.Status, Is.EqualTo(Status.Success));

            Assert.That(result.Value, Is.EqualTo(expectedValue).UsingPropertiesComparer());
        }
    }

    protected static void AssertInvalid(Result result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.Invalid));
        }
    }

    protected static void AssertDomainError(Result result, string error)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.DomainError));

            Assert.That(result.Error, Is.EqualTo(error));
        }
    }

    protected static void AssertConflict(Result result, string error)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.Conflict));

            Assert.That(result.Error, Is.EqualTo(error));
        }
    }

    protected static void AssertCanceled(Result result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.Canceled));
        }
    }

    protected static void AssertNotFound(Result result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.NotFound));
        }
    }

    protected static void AssertInternalError(Result result, string error)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.InternalError));

            Assert.That(result.Error, Is.EqualTo(error));
        }
    }

    protected static void AssertInternalError(Result result, Exception exception)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);

            Assert.That(result.Status, Is.EqualTo(Status.InternalError));

            Assert.That(result.Exception, Is.EqualTo(exception));
        }
    }

    private void VerifyAllAndNoOtherCalls()
    {
        var mocks = GetAllMocksInHierarchy(this);

        foreach (Mock mock in mocks)
        {
            Type type = mock.GetType();

            MethodInfo? verifyAllMethod = type.GetMethod("VerifyAll", Type.EmptyTypes);

            verifyAllMethod?.Invoke(mock, null);

            MethodInfo? verifyNoOtherCallsMethod = type.GetMethod("VerifyNoOtherCalls", Type.EmptyTypes);

            verifyNoOtherCallsMethod?.Invoke(mock, null);
        }
    }

    private static List<dynamic> GetAllMocksInHierarchy(object instance)
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
