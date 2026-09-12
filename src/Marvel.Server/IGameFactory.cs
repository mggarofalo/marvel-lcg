using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Creates a fresh game without deciding where its content bytes came from.</summary>
public interface IGameFactory
{
    /// <summary>Deals and opens one game.</summary>
    OpenedGame Create(GameSpecification specification);
}
