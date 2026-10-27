# Encounter reveal authority refresh

Base `4d95d05c`, acquired 2026-10-26. This is a scoped source and relationship
update for MARVEL-828, not acceptance of newly vendored behavior.

The complete four-page acquisition is retained outside the repository at
`Source/evidence/hoh-2026-10-26-reveal`. The committed candidate updates only
`post-rrg-1-7.html`; the three older page files retain their exact prior bytes.
Offline generation produces 1,113 records: 13 added, zero revised, zero removed.
Every one of the 1,100 predecessor IDs retains its content hash. No authority
text, historical answer or generated card text is edited by hand.

The [September 14 official answer](https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-7/)
is indexed as `ruling:7075a85e02f2203e`. It distinguishes cards first dealt to a
player, even when originally in a player deck, from direct reveals in other
game zones. The reviewed interpretation is pinned in `rules-graph.json` to the
matching trigger text of 01004 Enhanced Spider-Sense, 01075 Black Widow and
09007 Protective Ward. It supersedes only the earlier blanket equivalence to
any reveal. The older dealt-card interpretation remains consistent; Return the
Favor's separate discard-then-reveal instruction is not adjudicated by this edge.
That future instruction needs its own authority-derived execution context.

The operation can therefore retain its immediate source and whether it is
resolving a dealt encounter card. A persistent historical-origin property on
Card is unnecessary for this distinction. MARVEL-828 owns implementation and
independent review; actual card rows and compatible continuation proof remain
required. The two admitted Core cancellation rows also need the shared trigger
restriction tested when the new condition is applied; this source change alone
does not correct them.

## Added-record routing

| Ruling suffix | Subject | Delivery context |
|---|---|---|
| 0a7e7b33c1e95fa3 | Echo discount and Make the Call costs | Fear No Evil 617; preserve ordinary Core cost controls |
| 0e81e8420f565f88 | Rogue lasting copied traits/stats | Rogue 595 and affected ally printing; reconcile older contrary answers |
| 3afe400fe54de60a | Unflappable excludes separate ability damage | Current 09020 evidence already distinguishes attack and separate boost damage; Luke Cage 619 |
| 54c5c3949c0791ab | Elektra boost timing and distinct Venom cards | Elektra 620 |
| 6b756402a689cade | Surge timing and planned non-surge errata | Ward 828 cancellation controls; Fear No Evil 617. Planned errata is not substituted for printed text |
| 7075a85e02f2203e | Dealt versus directly revealed cards | Current scoped relationship and MARVEL-828 |
| 8030652dc5113609 | Echo nemesis Kingpin damage restriction | Fear No Evil 617 |
| 9196106259a1f1a4 | Exactly defend uses modified DEF/ATK | Hercules 616 and Luke Cage 619 |
| 95f5bab9a8a768fd / a956304c470dac6c | Shadow of the Past unique nemesis outcome | MARVEL-825 must verify the current supported unique-entry path before Strange acceptance |
| c7901bd3e9f887e9 | Hero Iron Fist heals himself | Iron Fist 621; not ally 09014 |
| c82f28717ddc2679 | Metal Bracer reduces dealt damage | Luke Cage 619 |
| d7c29493c75f9d8a | King's Gambit independent effects and then | Fear No Evil 617 |

These routes are context for implementation, not claims that all thirteen
interpretations have executable coverage. Independent review must challenge the
new relationship and current-pack impact before this packet is integrated.

## Verification

- Complete and scoped candidate comparisons: 13 added, zero revised/removed.
- Committed-page manifest and offline ruling check: four hashes verified,
  1,113 records, zero differences.
- Focused whole harvest, card-relationship, citation and index/graph classes:
  25 passed, no failures or skips.
- Raw acquisition, generated candidate, comparison and test logs live under
  `Source/evidence/reveal-authority-*` and the acquisition directories above.

Normal hooks and independent review are recorded with their resulting revisions.
No production engine, ability book, public admission, RNG or digest is changed.

## Combined integration and behavioral catalog

Source `0712bd87`, authority `eae9f8a6` and independent review `f235e7fd`
are combined on the accepted Black Widow baseline. Integrated Content selection
passes 302 cases and hosted nested/reveal/restart classes pass six. The initial
broader authority selection caught missing adjudications for the 13 added sources.
The catalog now explicitly includes all 2,667 sources and 4,432 obligations,
retaining every prior adjudication unchanged. Core cost, reveal-trigger and surge
propositions point to existing executable obligations; named expansion interactions
retain explicit outside-Core dispositions. Planned errata remains outside the
current printed authority. All 48 authority/catalog/citation cases pass after the
inventory reconciliation. The catalog addition needs its own independent review;
prior review of the harvest/graph alone did not cover it.

Logs: `Source/evidence/strange-reveal-integrated-content.log`,
`strange-reveal-integrated-server.log`, and
`strange-reveal-catalog-reconciled.log`. The six affected future canonical pack
parents (595, 616, 617, 619, 620, 621) now carry the added-record routing context
in Plane; MARVEL-825 retains the current Shadow of the Past obligation.
The executable Core transcript gate passes 684 behavior cases and rejects its intentional quarantine. Log: `Source/evidence/strange-reveal-core-transcripts.log`.
