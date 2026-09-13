using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Harvest;

/// <summary>One clause of an entry, and its qualifications.</summary>
/// <param name="Number">Its position, which is its id.</param>
/// <param name="Text">The clause.</param>
/// <param name="Qualifications">The bullets under it.</param>
public sealed record Clause(int Number, string Text, IReadOnlyList<string> Qualifications);
