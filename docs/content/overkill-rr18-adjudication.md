# Overkill authority adjudication (MARVEL-756)

Reviewed 2026-10-26 against engine `d7fcbf707ac89711484ebf1fe7e6bfe9d51201a0`.
The Rules Reference 1.8 requires actual defeat before overkill transfers excess
damage. Loki healing instead of defeat produces **zero** overkill damage.
The proposed restoration of pre-defeat spill is an authority-version error.
No human rules judgment is needed to resolve this point.

## Current authority

[FFG, Mission Updates](https://www.fantasyflightgames.com/mission-updates/),
the official explanation of Rules Reference 1.8, explicitly describes a change:
excess damage is transferred after defeat, and the initial target must be
defeated. Its Dragon/Relentless Assault example places damage on Dragon,
defeats Dragon, then deals the excess to the villain. This is a changed rule,
not merely clearer wording for the earlier simultaneous-damage rule.

The committed authorities agree:

| Record | Version | Pinned record hash |
|---|---|---|
| `rr:overkill` | 1.8 | `sha256:d25e9cea48a3fd329e294514289e4bcd994e08d528c7d90527be4ae3a2c3b9a7` |
| `rr:damage` | 1.8 | `sha256:8c9ec00426c16b32f72b95431ce694cbc8024325041f0e5230e68e7586561be2` |

`rr:overkill.1`: “If this attack defeats a minion, deal the damage beyond its
hit points to the villain.” `rr:damage` places damage at step 5, would-be-defeated
interrupts at step 6, when-defeated interrupts at step 7, discarding a defeated
character at step 8, and after-damage/defeat responses at step 9. Loki's printed
06028 text and mandatory Forced Interrupt erratum replace defeat with healing
when the discarded card is a treachery. Placed lethal damage is not itself a
completed defeat. Overkill and defeat benefits therefore cannot be inferred
from damage arithmetic before resolving Loki's interrupt.

## Earlier rulings and affected findings

| Vendored ruling | Recorded scope / hash | Present applicability |
|---|---|---|
| `ruling:8fe224a8f1e012b2` | pre-1.5 / `sha256:0b273b05e6bb8a809b12a062380c33f331f07af10fc479d9f84817040aae4a8ea` | Its Loki spill-before-revival answer describes the old overkill rule; RR1.8 supersedes that conclusion. |
| `ruling:304a4798e0b453b4` | 1.6, April 2025 / `sha256:bb5187d7cf855759c8b7096b8b09c43add104837beec00ff304d37ac022228b57` | Its question and answer expressly assume simultaneous overkill damage. They cannot justify a RR1.8 pre-defeat spill or synchronized initial-target/villain defeat sequence. |
| `ruling:c55100b5139ab3a5` | pre-1.5 / `sha256:0362d28b07618f0aae2ad4535ff3d9ff8e208a198e1f6269cea84f3c955d6f36d` | The explanation that old overkill did not require defeat is superseded; the Stealth Strike conclusion that healed Loki was not defeated remains consistent with printed text and RR1.8. |

The MARVEL-756 finding and rejected candidates `8265a038` and `90174f85`
must not restore that older behavior. The supplied external Loki row's note
claiming spill before healing is not current authority. The original external
`ThorLokiCardTests.cs` also does not establish complete resolution merely by
inspecting an intermediate attack state; the focused replacement tests require
completed results, an empty agenda, and the post-resolution board.

The separate simultaneous direct-damage E3 work is not overturned here.
`docs/timing.md` currently attributes its shared defeat-stage rule to
`ruling:304a4798e0b453b4`; that overkill-specific ruling is insufficient authority
for a general RR1.8 direct-damage rule. Review that attribution against current
simultaneous rules independently. Genuine simultaneous effects remain subject
to current damage/timing rules; this adjudication changes no such implementation.

No frozen historical fixture, vendored ruling, rules graph, or production ability
book is changed. The graph has no suitable relationship for recording a general
RR1.8 rule superseding only part of a historical card answer; adding a modification
that makes the old answer override RR1.8 would reverse the authority direction.

## Focused evidence and limits

`LokiRr18Fixture` loads actual printed Loki's mandatory interrupt as a test-local
row. `LokiRr18OverkillTests` exercises the current engine after pending work has
finished:

- Six attack damage: treachery heals Loki, leaves its incarnation in play, and
  gives zero villain damage and zero completed excess; a non-treachery defeats
  Loki and deals two excess damage. The event stream places Loki's departure
  before the villain's damage.
- Actual Relentless Assault with physical payment: healing gives zero spill
  and no Hall of Heroes response; defeat gives one spill and one glory.

Full Stealth Strike execution remains unverified. A trial test-local row with
the scheme choice inside the labeled power raises the engine's explicit
unsupported-suspension exception. Moving the choice outside that power did not
retain its defeat result. These trial rows/tests are not retained or counted as
passing evidence; the Black Widow author must implement and verify the actual
choice/receipt path separately. This is not a reason to change overkill timing.

Command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~LokiRr18OverkillTests`.
Logs live outside the repository under `C:/Users/mggar/Source/evidence/overkill-rr18-*`.
The deliberate mutant removes the actual-defeat guard in `AttackDamageResolution.BeginSpill`;
the healed-Loki direct and Relentless cases reject its erroneous two/one villain
damage. Restored verification requires a non-incremental Release rebuild.
These are nonpublic research-context tests, not pack admission or broad integration acceptance.
