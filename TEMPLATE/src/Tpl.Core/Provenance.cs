using System;

namespace Tpl
{
    /// <summary>The number was measured or read from a named source. Say which, specifically.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class VerifiedAttribute : Attribute
    {
        public string Source { get; }
        public VerifiedAttribute(string source) { Source = source; }
    }

    /// <summary>The number is a placeholder or a guess. Say why it was picked. List it in Docs/MEASURE.md.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnverifiedAttribute : Attribute
    {
        public string Reason { get; }
        public UnverifiedAttribute(string reason) { Reason = reason; }
    }
}
