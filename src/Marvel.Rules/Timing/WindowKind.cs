using System.Text.Json.Serialization;

namespace Marvel.Rules.Timing;

/// <summary>Which of an occurrence's two windows is open.</summary>
public enum WindowKind
{
    /// <summary>Before the occurrence resolves — <c>rr:interrupt.3</c>.</summary>
    Interrupt,

    /// <summary>After it has resolved — <c>rr:response</c>.</summary>
    Response,
}
