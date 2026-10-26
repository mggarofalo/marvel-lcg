# Photonic Blast and attached defeat interrupts (MARVEL-730)

Baseline: `2423ace6e4500f345d58d25d09dc449f5fb35b64`.

The preserved public multiplayer reproduction uses Spider-Man and Captain Marvel,
Mutagen Formula standard, seed 8. Decision 52 selects offer 107 from Photonic
Blast's target prompt, defeating Goblin Thrall (02024). The refusing projected
window observes **Spider-Tracer (01007), Forced Interrupt**, attached to that
Thrall. Thrall's Guard and boost entry are not defeat abilities. The baseline
passes the other three public scenario/mode cases and fails this one with
`labelled-power continuation reads a defeat-triggered ability`.

Authority is the generated printed text in `datasets/cards/cards.json`: Photonic
Blast deals 5 damage and draws one card if paid with energy; Spider-Tracer removes
3 threat from a scheme when its attached minion is defeated. Rules Reference
`rr:damage.step.7` resolves abilities that trigger “when [character] is defeated”;
step 8 then discards the character. The vendored MarvelCDB FAQ queried 01007,
01013 and 02024 and contains no entries for those faces.

The projection now defers cost-free, unconditional forced interrupts attached to
the defeated subject, alongside its own When Defeated abilities. Live resolution
owns their effects and choices. The existing `DefeatWindowDeferred` continuation
and damage-batch guards continue to refuse board-dependent reads. Missing authored
defeat text and unsupported observers still raise. No printed text, ability-book
row, admission boundary, wire format or exception swallowing is involved.

Focused evidence:

- A nonpublic two-player engine fixture pays Photonic Blast from Captain Marvel's
  hand and defeats a Thrall carrying Spider-Man's Tracer. Spider-Man receives the
  scheme prompt before the minion leaves and before Captain Marvel draws. Threat
  changes from 4 to 1; Tracer and Thrall depart before the draw. Energy and
  nonenergy payments distinguish the conditional draw and its recipient.
- A synthetic multiple-recipient damage instruction still refuses before
  exhaustion or damage after encountering the attached defeat window.
- The existing external-interrupt board-read test preserves its pre-cost refusal;
  its reason now identifies the unprojected defeat window.
- A compiled mutant restricting deferral to the subject card fails all three new
  cases. Restoring the source and forcing a nonincremental Release build passes
  the same cases and the focused continuation regression selection: **886 passed**.
- The unchanged public multiplayer acceptance theory passes **all four modes**,
  comparing state, RNG, prompt, events and information after every decision.

External logs are retained in `C:/Users/mggar/Source/photonic-*.log`: baseline,
observer diagnosis, mutant, restored build, restored focused, public and hooks.
These are focused managed checks; no broad suite or native client claim is made.
