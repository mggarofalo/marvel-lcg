# Defender arrow-cost prerequisite (MARVEL-743)

Candidate based on `350e6e19`, branch `feat/thor-defender-cost`. This packet
implements the engine prerequisite only. It does not author 06003 in the ability
book, open Thor admission, or change setup, protocol versions or UI.

Printed authority is `datasets/cards/cards.json` 06003. Both discard-until and
putting that physical minion into play engaged with you precede the arrow.
`rr:cost-arrow-icon` requires full payment; `rr:cost.6` prevents paying when no
legal effect target exists. `rr:encounter-deck.1` and `.2` bound the discard and
require immediate reset. The matching physical card is retained even when reset
shuffles it into the replacement deck.

Two card clarification relations are pinned in `datasets/rules-graph.json`:

- July 2023 `ruling:6e301a347a5392fb` explicitly updates earlier conflicting
  guidance, superseding `ruling:1fc1a3ff38a01231`: no discarded/engaged minion
  means no threat removal and no Confused removal. The deck resets, acceleration
  is added, and the event finishes.
- April 2026 `ruling:a1f4982cfebcd768` places effect initiation after cost
  payment. Patrol or a status acquired during cost can affect that initiation;
  it supersedes the old Zemo answer `ruling:4a7169aa3869c06c`.

The engine-selected DSL operation is
`cost: {"discardUntilMinionEngaged":"you"}`. Lowering accepts only that explicit
resolving-player binding. Offer generation examines available counts and effect
legality without examining hidden identities. The existing payment cursor retains
an entry receipt and resumes after the ordinary encounter-entry procedure,
Quickstrike and engagement responses. A no-match or blocked entry completes the
event without its effect. Post-cost label and target checks preserve the later
initiation boundary. No AbilityRunner or agenda timing-spine changes are needed.

Evidence on this candidate:

- Release Content project nonincremental build: zero warnings/errors.
- Focused Content tests: 61 passed, covering new primitive/lowering plus existing
  Thor engagement and deck arrow-cost regressions. New cases cover physical match
  through reset, no-match plus Confused, zero-threat admission, blocked unique
  minion entry, Quickstrike then Thor response then one threat removal, post-cost
  Patrol, and rejected nested answers preserving card digest, RNG and agenda.
- Rules relationship and rule citation tests: 4 passed.
- Mutant treating every completed entry as paid compiled and failed the no-match
  and blocked-entry tests (2 failures). A second mutant skipping post-cost target
  legality compiled and failed the Patrol case (1 failure). Each restoration was
  followed by a nonincremental rebuild; restored focused run: 61 passed.

Logs are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/thor-cost-*.log` (`restored-build`, `restored`,
`rules`, `mutant`, `patrol-mutant`). Normal commit hooks remain enabled.

The synthetic event fixture uses a main-scheme thwart body to isolate the engine
mechanism; it does not claim complete printed Defender content. Independent
review, complete row integration, multi-scheme target composition, saved-session
replay and broad integration gates remain with the Thor delivery lead.
