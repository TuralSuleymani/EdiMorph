namespace EdiMorph.Rules;

/// <summary>
/// How much of a document a handler is allowed to see.
/// </summary>
public enum RuleScope
{
    Syntax = 0,
    Version = 1,
    Envelope = 2,
    Segment = 3,
    Loop = 4,
    Transaction = 5,
    Provider = 6
}
