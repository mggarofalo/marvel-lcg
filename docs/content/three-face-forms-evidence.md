# Explicit three-face identity selection — MARVEL-861

Packet base: `bb1ec14e00a8658cdb5ab5a54bb6d6360180e5de`.
Branch: `feat/three-face-forms`. This record accompanies the commit containing it.
Nonpublic engine prerequisite for MARVEL-569/570; neither pack is admitted.

## Authority and result

Printed identity faces are generated `datasets/cards/cards.json` rows
`12001a/b/c` and `13001a/b/c`: Tiny hero, alter-ego, Giant hero, respectively.
The reviewed physical inventory is
`docs/content/research/remaining/569-574-inventory.md`; each trio is one physical
identity, not three cards. Research context: `569-574.md` in the same directory.

`pack:mc12:ant-man-hero-pack#rules-clarifications` and
`pack:mc13:wasp-hero-pack#rules-clarifications` explicitly permit alter-ego to
either hero, either hero to alter-ego, and one hero to the other.
RR 1.8 `rr:flip.1` counts each change of face as a flip;
`rr:form-change-form.1-.3` establish the voluntary round limit, preserved card
state, and ability-induced exception. These current clauses govern the packet.
Local Taskmaster ruling `9218c9f1b9506cdd` (pre-1.5) explicitly confirms that
Tiny → Giant triggers Taskmaster. Its answer remains consistent with the current
form/flip clauses and Taskmaster's printed response; it is corroboration rather
than an override of RR 1.8.

The engine offers two distinct actions from every actual three-face identity
face, and accepting one changes to exactly its selected destination. One change
spends the voluntary receipt. Direct nonvoluntary changes preserve that receipt
even when already spent and schedule the actual `FormChanged` lifecycle; the
authored Taskmaster response consumes its boost and deals the printed amount
after Tiny → Giant. Explicit no-op, foreign and nonidentity destinations reject
before mutation. Live turn admission rechecks prohibition, receipt and current
destination before clearing information or advancing the prompt.

Engine choice: three-face offers reuse `Label` as the destination face id in the
existing semantic replay address. Each has its own opaque handle and supplied
`DisplayLabel`/`CommitLabel` with printed title/traits. This introduces no new
protocol, digest or RNG field. Two-face offers retain their semantic label,
allocation and toggle behavior. Complete server/archive replay and socket
journeys for a three-face product remain unclaimed pending admission.

## Verification

Logs are outside the repository at `C:/Users/mggar/Source/three-face-*.log`.

- `Marvel.Content.Tests`, filter `ThreeFaceFormTests|FormDataTests|PlayerPhaseTests|TaskmasterAbilityTests|AllTiedUpProhibitionTests`: **86 passed**, including **27 new cases**.
- `Marvel.Rules.Tests`, filter `FormsTests|LifecycleOccurrenceTests`: **17 passed**.
- Chosen-face mutant: replaced `TurnTo(destination)` with the first other face;
  compiled and **6 of 12 transition cases failed** (`mutant-destination.log`).
- Voluntary-limit mutant: disabled both live and offer receipt checks;
  compiled and **13 of 27 cases failed** (`mutant-limit.log`).
- Restored sources were rebuilt with `dotnet build ... -c Release --no-incremental`:
  **zero warnings/errors**, followed by **86 passing** affected content tests
  (`restored-build.log`, `restored.log`).
- Full architecture suite: **129 passed** (`final-architecture.log`), without
  any baseline changes. The ordinary complexity hook identified an eleven-branch
  validation method; live legality is a separate cohesive predicate. Final normal
  commit-hook results and the tested commit are recorded in the handoff.

Tests use actual printed identities on nonpublic manually constructed engine
boards. They do not claim played Ant-Man/Wasp games or author their abilities.
Taskmaster is an actual authored response, reached through the scheduled form
procedure; the initiating form change is a direct nonvoluntary Rules call.

## Separate interpreter and client work

The current DSL `changeForm.to` admits only `hero`/`alter-ego`. Its admission,
basic-power projections and effect traces model that binary distinction. It
cannot express Resize's explicit Tiny/Giant destination. Those contracts need a
separate reviewed packet before authored Ant-Man/Wasp abilities or admission.
This packet does not make a generic ambiguous hero destination choose a face.

The existing immediate DSL form-change path also emits `CardsFlipped` without
scheduling `FormChanged`. Executed reproducer using currently authored content:

1. World with Jennifer Walters `01019a,01019b`, showing `01019b`; Taskmaster
   `04093` in villain area; encounter deck has `01104` then `04064` on top.
2. Put actual Split Personality `01025` and three `01087` payments in hand;
   place eight `01087` in the player deck. Use `AuthoredCards.Runner()`.
3. Select the actual Split Personality action, `ExecuteAction` with those three
   payments, then `Sequence.Finish`.
4. Identity becomes She-Hulk `01019a`, but Taskmaster deals **0**, expected **3**
   from `04064`, and its boost is not consumed.

The isolated diagnostic test fails at damage expected 3/actual 0. Its complete
source is `C:/Users/mggar/Source/ThreeFaceTimingProbe.cs`; execution log is
`three-face-existing-timing-probe.log`. The diagnostic was removed from this
passing packet. The interpreter source is untouched by this diff. Root owns a
separate engine implementation/review assignment for this supported-content
timing defect, including continuation order and binary projection extension.

Clients must render both explicit engine offers with their supplied destination
wording and commit the chosen handle. Any client assumption that `Change_Form`
has exactly one match must be addressed in a separate boundary/UI packet.
Native form selection, View/Decisions semantics, protocol/replay journeys and
materially new interaction review remain outside this engine packet. Public
three-face admission must wait for those dependencies and complete pack data.
