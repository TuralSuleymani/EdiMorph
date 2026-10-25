namespace EdiMorph;

/// <summary>
/// How a provider profile is allowed to differ from the base standard.
/// </summary>
public enum ComplianceMode
{
    /// <summary>The profile may only tighten the standard.</summary>
    StandardSubset = 0,

    /// <summary>A named departure from the standard is allowed, and the result is not plain X12.</summary>
    PartnerDialect = 1
}
