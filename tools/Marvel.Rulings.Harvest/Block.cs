using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

internal sealed record Block(string Tag, string Body);
