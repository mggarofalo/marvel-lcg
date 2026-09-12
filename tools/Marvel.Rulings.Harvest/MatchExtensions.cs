using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

internal static class MatchExtensions
{
    public static int End(this Match match) => match.Index + match.Length;
}
