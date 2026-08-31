using FluentAssertions;
using PropertyOS.Application.Common.Numbering;

namespace PropertyOS.Tests.Unit.Application.Common;

public sealed class IdentifierSequenceTests
{
    [Fact]
    public void FirstValue_StartsAtOne() =>
        IdentifierSequence.NextSuffix([], "BLD-").Should().Be(1);

    [Fact]
    public void ExistingValues_UseMaximumSuffixRatherThanRowCount() =>
        IdentifierSequence.NextSuffix(["BLD-001", "BLD-002", "BLD-007"], "BLD-").Should().Be(8);

    [Fact]
    public void DifferentYearsAndCustomValues_DoNotAffectContractSequence() =>
        IdentifierSequence.NextSuffix(["LSE-2025-099", "CUSTOM-12", "LSE-2026-003"], "LSE-2026-").Should().Be(4);

    [Fact]
    public void SeparateCompanyValueSets_RemainIndependent()
    {
        IdentifierSequence.NextSuffix(["BLD-009"], "BLD-").Should().Be(10);
        IdentifierSequence.NextSuffix(["BLD-002"], "BLD-").Should().Be(3);
    }
}
