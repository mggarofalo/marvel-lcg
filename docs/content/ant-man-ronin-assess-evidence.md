# Ronin and Assess the Situation — MARVEL-569

This packet appends exactly two actual faces, Ronin `12013` and Assess the
Situation `12033`, to baseline `a45dff783576c0ff3976f82fe2ff3c9619ccda10`.
The authored book has 731 rows. Its original 729 rows retain their exact UTF-8/LF
bytes: the 738108-byte prefix through the final original row hashes to
`bb077c7f2fed44384b1248abe23df80fde3fadf607442d7040773968384a526b` (SHA-256).
Only the comma and appended rows precede the original array/object closing text.
The independent expected count and explicit authored-face inventory include
these two faces. No engine, setup, public product admission or other card changes
are included.

## Authority and interpretation

Printed authority is `datasets/cards/cards.json`. Ronin gets +1 THW and +1 ATK
while an upgrade is attached; existence grants the bonus once regardless of
upgrade count. `upgradesOnThis` is the already reviewed engine selector, including
effective kind, current host/incarnation and deterministic physical-id order.
RR 1.8 `rr:upgrade.3` distinguishes an attached upgrade from encounter Attachment
cards (`rr:attachment`). Blanking an attached upgrade leaves its kind and
attachment intact; blanking Ronin suppresses Ronin's constant. The actual book
tests distinguish Inspired's independent contribution from Ronin's bonus.

Assess is an unrestricted **Action**, costs zero, and grants its player +1 hand
size **until the end of the phase**. It is usable in hero and alter-ego forms.
RR 1.8 `rr:end-of-player-phase.step.2` draws before step 4 expires phase effects;
ending a player's turn does not expire this bonus. Copies stack. Changing form
retains the effect on the same physical identity.

The vendored MarvelCDB acquisition records `none` for both card ids. Iron Man's
global total hand-size maximum remains 7 under rulings
`ruling:4bf1b4ba524db1d6` and `ruling:39c26b6b8d77edfd`; the latter confirms it
in scope 1.7–1.8. Existing Iron Man tests now use the actual Assess row instead of
appending a duplicate test-local row. Generic projected hand-size checks remain
explicitly synthetic: this packet establishes no played-card projection consumer.

## Verification

The initial Release build and focused run passed 30 cases, including both actual
card classes, all Iron Man maximum cases and rule-citation validation. Source:
`C:/Users/mggar/Source/evidence/ant-ronin-assess-focused.log`.

Actual-book Ronin checks cover zero, one and several upgrades, an unrelated host,
an encounter Attachment, live removal, blanking either source, real paid
cross-player Reinforced Suit attachment, retained ownership and control transfer
of the hosted tree. Actual Assess checks cover both forms, sequential Actions,
stacking, the other player's unchanged hand size, event disposal, survival through
turn end, real phase-end draw, expiration and form change. Existing Iron Man
checks supply the actual capped draw and ordinary-hero uncapped control.

Two authored-data mutations are run independently after successful
nonincremental Release builds: Ronin's grants become +2, then Assess's duration
becomes EndOfRound. Each must fail its actual-card tests at the distinguishing
stat or phase-expiration assertion. The original bytes are restored in a finally
block, followed by a nonincremental Release build and the same checks with broader
Cards, Ronin selector, Ant-Man supporting/reprint and rule-citation regressions.
Logs use prefix `C:/Users/mggar/Source/evidence/ant-ronin-assess-` and suffixes
`ronin-mutant-build.log`, `ronin-mutant.log`, `assess-mutant-build.log`,
`assess-mutant.log`, `restored-build.log`, `restored.log` and `hooks.log`.

Both mutant builds succeeded. The Ronin mutation failed both actual tests
(expected ATK 3, actual 4); the Assess mutation failed all three actual cases
at phase expiration. The first restored build succeeded and its run passed 216
of 217 cases; the remaining failure identified the separate Ant-Man reprint
test's explicit pack-face inventory. That inventory now includes exactly the
two appended faces while retaining its historical reprint-position assertions
and unsupported-identity/customization negative controls.

After that inventory correction, the final nonincremental Release build passed
with zero warnings/errors and all 217 restored regression cases passed. See
`ant-ronin-assess-final-build.log` and `ant-ronin-assess-final.log` beside the
other logs. Normal commit hooks run with this final source and remain enabled.

These are nonpublic engine tests of two actual rows. They do not establish
complete Ant-Man coverage, public setup, full-game outcomes, native UI acceptance
or independent review. No composition gap was observed in the exercised paths.
