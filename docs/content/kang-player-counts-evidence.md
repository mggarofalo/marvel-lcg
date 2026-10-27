# Kang participating-player counts: prepared checkpoint

MARVEL-858 under MARVEL-568. Base `71aaa40bbe87a75f6ad2c70ba7941ca69298bac2`.
The complete pack inventory remains 58 playable faces / 73 physical cards. This
bounded nonpublic packet changes no production ability rows, admission, recipes,
protocol, UI or persistence. It extends the accepted split/reunion mechanism to
one, three and four participating players alongside two.

Authority is the actual printed `11001`–`11013a/b`, expert `11034`–`11039`, and
`pack:mc11:game-areas` / `pack:mc11:rules-clarifications`. Stage 2A chooses a
random 3A for each player in turn order and removes unused schemes; 2B permits
central reunion only after other areas are gone; 4A reveals III and stored
Dominions; 4B puts each physical associated nemesis minion into play and shuffles.
Seeded allocation order and bounded draw consumption remain engine choices.

Prepared evidence covers all counts in normal/expert split tests, seed 42's exact
assignments without replacement, consumed RNG words (1/3/4/5), all other seats'
isolation, flat local HP and starting threat, one dealt encounter per seat and
unused-stage removal after local reveals. Sequential reunion tests cover solo's
direct central return, growing whole groups, every offered other destination,
explicit chosen joins, nonzero first-player ownership, rejected destination
atomicity, retained ending-phase work, final 4A/4B and physical nemesis entry.
Normal-mode local scheme completions exercise stored Dominion revelation at the
new counts. Two same-input reconstructions compare events, digest, topology,
pending work and complete RNG state; this is not persisted replay. A prepared
public `Game` loop tests the real player-phase join suspension without opening
production admission.

Windows Release verification on the candidate based on the revision above uses
`MSBUILDDISABLENODEREUSE=1`, serial `-m:1`, and a nonincremental Content build,
then `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
--no-build -m:1 --filter 'FullyQualifiedName~Kang'`. The first build passed;
the first focused run passed 130/136. Six fixture failures exposed the test's
incorrect end-turn command and absent test-local Dominion reveal admission.
After fixing those, 133/136 passed; the three remaining Game cases incorrectly
assumed the host phase enum must remain outside VillainPhase. The real contract
is its retained typed EndPlayerPhase frame and unchanged local threat while the
join is pending. That distinguishing assertion passed all 136 cases.

Three individually compiled mutants were killed: restricting the split to two
players failed six of ten count cases (four two-seat controls passed); removing
both final-group count/seat membership guards failed a new malformed-topology
control because no exception was thrown; removing local-departure elimination
refusal failed all three two/three/four-seat negative cases. The detached-seat
control is synthetic malformed topology, not a legal game transcript. Mutation
sources were restored before a fresh nonincremental build and the expanded
137-case Kang regression passed. Measured logs are in
`C:/Users/mggar/Source/evidence/kang-player-counts-*`: initial/corrected/phase-owner
build and focused logs, `mutant-{count,membership,elimination}{-build,}.log`,
and `restored-{build,focused}.log`. Final hooks and independent review are
reported separately; this packet does not claim those gates from test success.

Uncovered scope remains simultaneous/co-due departure ordering, competing delayed
phase-end effects and local-stage completion caused by eliminating its players.
Those phase-end paths refuse rather than choose an ordering or guess a survivor
policy. Existing central nemesis-entry elimination handling is retained. Complete
Kang production rows, public setup/session/replay, native interaction, Linux and
full managed gates are outside this bounded packet. The next prerequisite is
local elimination completion and explicit simultaneous departure ordering.


The first normal commit attempt passed all 66 architecture-policy checks and
failed the unchanged complexity threshold: `KangPhaseTransitions.Preflight` had
CCN 11 (limit 10). Participant and captured-group validation are now one cohesive
helper, retaining elimination refusal before captured membership validation and
all later central/delayed-work conditions. The three mutant results above apply
to the pre-extraction sources; no post-extraction mutation sensitivity is claimed.
Fresh post-extraction nonincremental Release Content build passed with zero
warnings/errors, and all 137 Kang cases passed. Logs are
`kang-player-counts-extracted-{build,focused}.log`; the normal-hook attempt is
recorded separately in `kang-player-counts-extracted-commit.log`.
