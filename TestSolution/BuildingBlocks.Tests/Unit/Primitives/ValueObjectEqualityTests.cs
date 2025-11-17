using BuildingBlocks.Domain.Primitives;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.Primitives;

public sealed class ValueObjectEqualityTests
{
    private sealed class TestValueObject : ValueObject
    {
        private readonly int _value;
        private readonly string? _text;

        public TestValueObject(int value, string? text)
        {
            _value = value;
            _text = text;
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return _value;
            yield return _text;
        }
    }

    [Test]
    public void Equals_ShouldReturnTrue_WhenAtomicValuesMatch()
    {
        var left = new TestValueObject(1, "test");
        var right = new TestValueObject(1, "test");

        left.ShouldBe(right);
        (left == right).ShouldBeTrue();
    }

    [Test]
    public void Equals_ShouldReturnFalse_WhenAtomicValuesDiffer()
    {
        var left = new TestValueObject(1, "test");
        var right = new TestValueObject(2, "test");

        left.ShouldNotBe(right);
        (left != right).ShouldBeTrue();
    }

    [Test]
    public void Equals_ShouldReturnFalse_WhenOtherIsNull()
    {
        var valueObject = new TestValueObject(1, null);

        valueObject.Equals(null).ShouldBeFalse();
    }

    [Test]
    public void GetHashCode_ShouldMatch_WhenAtomicValuesMatch()
    {
        var left = new TestValueObject(1, "test");
        var right = new TestValueObject(1, "test");

        left.GetHashCode().ShouldBe(right.GetHashCode());
    }
}
