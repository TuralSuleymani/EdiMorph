using EdiMorph.Abstractions.Tests.Factories;
using EdiMorph.Rules;

namespace EdiMorph.Abstractions.Tests;

public sealed class RuleRegistryTests
{
    private readonly RuleRegistry _sut = new();

    [Fact]
    public void Register_WhenRuleIdAlreadyExists_ShouldThrow()
    {
        // Arrange
        var descriptor = RuleHandlerDescriptorFactory.Create();
        _sut.Register(descriptor);

        // Act
        var secondRegistration = () => _sut.Register(RuleHandlerDescriptorFactory.Create());

        // Assert
        secondRegistration.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{TestDefaults.EnvelopeIntegrityRuleId}'*");
    }

    [Fact]
    public void TryGet_WhenHandlerWasRegistered_ShouldReturnThatDescriptor()
    {
        // Arrange
        var descriptor = RuleHandlerDescriptorFactory.Create(
            TestDefaults.SegmentSyntaxRuleId,
            RuleScope.Segment);
        _sut.Register(descriptor);

        // Act
        var found = _sut.TryGet(TestDefaults.SegmentSyntaxRuleId, out var registered);

        // Assert
        found.Should().BeTrue();
        registered.Should().BeSameAs(descriptor);
        registered!.Scope.Should().Be(RuleScope.Segment);
    }

    [Fact]
    public void Register_WhenHandlerIsNew_ShouldChangeRevision()
    {
        // Arrange
        var revisionBeforeRegistration = _sut.Revision;

        // Act
        _sut.Register(RuleHandlerDescriptorFactory.Create(TestDefaults.SegmentSyntaxRuleId));

        // Assert
        _sut.Revision.Should().NotBe(revisionBeforeRegistration);
    }
}
