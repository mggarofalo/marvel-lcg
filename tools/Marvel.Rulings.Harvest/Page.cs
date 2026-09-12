using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Marvel.Rulings.Harvest;

public sealed record Page(
    string Name,
    string FileName,
    string Via,
    string RulesReferenceScope,
    PageShape Shape);
