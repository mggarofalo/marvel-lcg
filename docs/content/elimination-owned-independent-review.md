# Elimination ownership independent review

Reviewed `8a35a6784c3d35ca3ed9c8039383a7dccf013d10` for MARVEL-860 on
2026-10-27 in an isolated worktree. No production findings remain in the reviewed
scope; this review adds two synthetic behavioral controls and no production code.

`rr:player-elimination.step.2` transfers engaged minions while retaining hosted
cards. Step 4 subsequently places "each card owned by the eliminated player" in
their discard pile. The ordered wording supplies no exception for an owned card
transferred in step 2 or located in another player's or the villain's play area.
Step 5 then removes the eliminated play area. `rr:discard.4` requires top-first
deck discards without changing their order; other pile ordering is a documented
engine choice. Shared layout membership and delegated card ownership facts
keep the live and projected departure sets aligned.

The independent controls place an eliminated-owned upgrade under a minion in a
surviving player's play area, with nested surviving-owned and encounter-owned
attachments. One case transfers the minion first. The owned host is removed,
each nested attachment discards and detaches exactly once, and an unrelated
surviving-owned sibling remains hosted on the minion. Query checks include the
canonical state, RNG state/index/consumption and outstanding agenda.

Independent parsing of all four old/new canonical artifacts verified 32, 34, 40
and 32 changed fields respectively. Every changed field is an index of a card in
RemovedArea. Removing those indices produces equal complete canonical objects;
the SHA-256 of each original canonical string matches its recorded digest. The
new digests match the four updated transcript pins. This supports the stated
step-4/step-5 ordering explanation without treating changed hashes as their own
justification.

Executed Release evidence:

- Elimination-focused rules: 29 passed, including both independent controls.
- Elimination, actual Highway Robbery, projection, same-ability completion and
  Core elimination pins: 29 content checks passed.
- Original Ronan public games: all four passed, including strict session replay.
- Frozen-source named upgrades, recorded-fact rejection, exact-source admission,
  v27 rejection and runtime guards: 76 server checks passed.

Logs and Ronan TRX are outside the repository at
`C:/Users/mggar/Source/evidence/elimination-independent-*`. Canonical input
artifacts are `elimination-owned-canonical-old` and
`elimination-owned-canonical-new` in the same directory. This is focused Windows
review evidence; aggregate integration and both-platform certification remain
separate gates. Replay semantics belong to the unpublished v28 candidate; this
review does not admit v27 history.
