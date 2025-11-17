using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;
using System.Reflection;

namespace BuildingBlocks.Tests.Unit.Primitives;

public sealed class StronglyTypedIdTests
{
    private sealed class SampleId : StronglyTypedId<SampleId>
    {
        public SampleId(Guid value)
            : base(value)
        {
        }
    }

    [Test]
    public void Create_ShouldReturnIdWithProvidedValue()
    {
        var guid = Guid.NewGuid();

        var id = StronglyTypedId<SampleId>.Create(guid);

        id.Value.ShouldBe(guid);
    }

    [Test]
    public void New_ShouldReturnUniqueValue()
    {
        var first = StronglyTypedId<SampleId>.New();
        var second = StronglyTypedId<SampleId>.New();

        first.Value.ShouldNotBe(Guid.Empty);
        second.Value.ShouldNotBe(Guid.Empty);
        first.ShouldNotBe(second);
    }

    [Test]
    public void Create_ShouldThrow_WhenGuidIsEmpty()
    {
        var ex = Should.Throw<TargetInvocationException>(
            () => StronglyTypedId<SampleId>.Create(Guid.Empty));

        ex.InnerException.ShouldBeOfType<ArgumentException>();
        ex.InnerException!.Message.ShouldContain("The provided value is invalid.");
    }
}
