using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A bounded kind of concealed information observed while resolving.</summary>
public enum InformationKind
{
    /// <summary>A concealed card identity became readable.</summary>
    Reveal,

    /// <summary>A game effect inspected a concealed search area.</summary>
    Search,
}
