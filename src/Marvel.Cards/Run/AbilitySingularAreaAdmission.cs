using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Preflight may refuse a single-card read if prior effects or payment can
// change its candidates. This capability admits only an area-set query; it
// exposes no runner, effect execution or continuation operation to the evaluator.
internal delegate bool AbilitySingularAreaAdmission(IReadOnlySet<DeckType> areas);
