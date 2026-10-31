using Marvel.Rules.Play;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>What one complete Kang game reached.</summary>
internal sealed record KangGameRecord(Outcome Outcome, int Round, int Decisions, bool Split, int Joins, bool Reunited,
    bool FinalKang, long RngWords);
