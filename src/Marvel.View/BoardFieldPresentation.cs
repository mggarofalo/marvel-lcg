using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One live public field rendered on a readable card.</summary>
public sealed record BoardFieldPresentation(string Name, string Value);
