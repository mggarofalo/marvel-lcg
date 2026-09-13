using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>Presentation-only table priority; it does not change area visibility.</summary>
public enum BoardAreaProminence
{
    /// <summary>No card currently occupies the area.</summary>
    Empty,

    /// <summary>The area contains cards but is not part of the round-to-round tableau.</summary>
    Supporting,

    /// <summary>The area contains live state players commonly monitor each round.</summary>
    Live,
}
