# Mutagen Formula Standard public victory

The accompanying test packet adds an opening-to-terminal **PlayersWin** proof to
base revision `08784f97`. It preserves the existing Risky Business victory and
Green Goblin defeat/restart tests. No production code, ability book, setup data,
card data, deck order, RNG state, card state or admission gate is changed.

| Scenario | Identity/deck | Seed | Outcome | Round | Decisions | Villain stages |
|---|---|---:|---|---:|---:|---|
| Mutagen Formula Standard | Captain America, custom Leadership | 3 | PlayersWin | 13 | 130 | I (02014), II (02015) |

The production `DatasetGameFactory` assesses the complete 40-card recipe as valid
before creating two independent games from the same specification. The recipe
contains all 15 identity cards, 15 Leadership cards and 10 Basic cards, with its
exact card IDs and quantities in `MutagenVictoryTests`. These are ordinary
admitted custom-deck choices, not changes to the factory's starter or catalogs.

At every prompt, the strategy submits only actual engine offers, targets and
resource sources. It reads the answering hand and public in-play areas; it never
reads the encounter/player deck order or RNG. Before each strategy query, the
test captures the digest, RNG word count, outcome, phase, round and complete
prompt and asserts that choosing an answer changes none of them. A second game
receives that exact answer, and every resulting observation, event and information
receipt must match. The final assertions require PlayersWin, both actual villain
stages, an Over phase and a finished replay. Loss is still a failing result.

## Why these choices

Printed authority is `datasets/cards/cards.json`, identified here by face ID.
The ordering and scores in the test policy are authored tactics, not game rules;
`Game.Resolve` and `ResourcePayment.Allocate` retain legality/payment authority.

| Visible problem | Card authority and tactical response |
|---|---|
| Goblin damage also raises main-scheme threat | Green Goblin I/II (02014/02015) add threat after attacking and damaging the player. Prefer ally defenses or Shield Block (03005); install the Shield (03009), whose +1 DEF and retaliate reduce damage and pressure minions. |
| Defending exhausts Captain America | The identity (03001a) can discard one hand card to ready once per round. Use remaining basic actions and then the ready; preserve the Shield when choosing a discard. |
| Repeated resource pressure | Install Super-Soldier Serum (03010). Preserve an affordable Heroic Strike rather than automatically consuming it to install a second Serum. Avoid spending a double-resource card on a one-resource cost when an offered single-resource payment is available. |
| Villain activations and minion accumulation | Heroic Strike (03004) deals six and stuns when paid with physical resources. Prefer the villain for that large attack when offered; otherwise prefer a large minion. Serum supplies physical resources. Shield Toss (03006) can hit several enemies; discard low-value hand cards and reinstall the returned Shield. Basic attacks prefer a minion closer to defeat. |
| Small hand and incoming attrition | Fearless Determination (03003) draws and improves THW. Agent 13 (03002), Maria Hill (01067), Squirrel Girl (03013), Mockingbird (03020), Hawkeye (03012), Falcon (03011) and Nick Fury (01084) have useful offered entry/response effects. Prefer Nick Fury's three-card draw. Three First Aids (01086) provide two healing each and help stay in hero form. |
| Main-scheme and side-scheme pressure | Unleashing the Mutagen (02017b) starts at two and advances at seven per player; Mutagen Cloud (02018b) loses at eleven and scales escalation with Goblin enemies. Prefer thwart at three main threat, retaining headroom, or when side schemes remain without engaged minions. Scheme targets prefer a high-threat main scheme, otherwise a side scheme. Re-enter hero form when offered after recovery. |

These preferences use the current board and offered effects. The seed remains
three across both failed starter and successful custom-deck probes; no seed search
was used in this packet. The starter-defense attempt lost by scheme in round 8
(stage II), and is retained as a failed probe rather than called a victory.
The winning path used nine ally defenses and one basic recovery. It still has
heuristics and is not claimed as a generally strong player for other deals.

## Reproduction and retained artifacts

```powershell
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter 'FullyQualifiedName~MutagenVictoryTests|FullyQualifiedName~GreenGoblinVictoryTests' --logger 'console;verbosity=detailed'
```

Final focused result: **2/2 passed** (9.352 seconds), including the unchanged
Risky Business Standard victory. Every Mutagen decision is printed as complete
serialized JSON, including resources, values and allocations, plus its public
board/hand context. The independent replay runs inside the same test.

External evidence directory:
`C:/Users/mggar/Source/mutagen-strategy-evidence/`.

- `final-victories.log`: complete focused output and all 130 winning decisions.
- `mutagen-standard-seed3-decisions.json`: the 130 exact JSON decisions extracted
  from that successful output, in opening-to-terminal order.
- `starter-defense.log`: unsuccessful starter-deck probe.
- `leadership-sustain.log`: first successful Leadership probe before cleanup.

Transcript SHA-256: `f3baf3b3f1e6c2d588316694a61e8ed3eb47e3f76936a9684daaf8ff549e10a9`.

The following source fingerprints identify the final focused run independently
of later integration/cherry-pick commit IDs:

| Source | SHA-256 |
|---|---|
| `tests/Marvel.Acceptance.Tests/Play/MutagenVictoryTests.cs` | `9ef2656298fa393396cacd42959f6848221f1c6e182bdff6201164a0ef34e66d` |
| `tests/Marvel.Acceptance.Tests/Play/MutagenTacticalPolicy.cs` | `55157f95100a75d43c8e7c6a1a20edc67339c11365090b698880a2bf74f706ac` |
| `tests/Marvel.Acceptance.Tests/Play/MutagenPolicyPayment.cs` | `6100e1ffa9bb733b5634dddc0d751aa6d4c8e207ec5c57898322de2137892f22` |

Independent review, broader acceptance/integration suites, native UI, Expert
victory and cross-platform CI are outside this focused evidence claim.
