# Hulk customization cards — MARVEL-850

Candidate on 4dcd8afb: three actual research faces10030 Inspiring Presence,
10031 Electrostatic Armor and10032 Resourceful. The book has712 rows and27/33
Hulk faces. All709 predecessor raw JSON objects were compared as exact UTF-8
slices and are byte-identical. Initial CRLF conversion was corrected before
final verification. Unknown90001 and10015,10025–10029 remain absent.

Printed authority is the joined generated datasets/cards/cards.json. Each of
these three IDs was queried in the vendored MarvelCDB FAQ and has no entry.
The local rulings corpus also discusses Electrostatic Armor in
[`ruling:71b0ff212c394895`](../../datasets/rulings/rulings.json) (RR 1.6,
April 2025 Quicksilver timing) and
[`ruling:fa66b7fa4df62850`](../../datasets/rulings/rulings.json) (pre-1.5
Shadowcat/Powerful Punch). Both place its response alongside after-defense
responses. Current RR 1.8 `rr:defend-defense.7` expressly places those abilities
after the attack ends; `rr:defend-defense.4` includes defense-labeled abilities,
so the historical basic-defense example does not impose a basic-defense-only
restriction. No matching local ruling text was found for Inspiring Presence or
Resourceful.
Current RR1.8 governs: rr:and.1 says individual effects connected by “and” are
not dependent on each other; rr:defend-defense.7 places after-defense abilities
after the attack ends; rr:you-your.6 identifies “you” with the identity;
rr:ownership-and-control.2.1 assigns control to the recipient of an upgrade.

Inspiring Presence checks current identity Avenger and hero form, chooses any
ally, and independently heals1 and readies that same ally. Actual paid-action
tests cover undamaged/exhausted, damaged/ready and damaged/exhausted allies in
another player's area. A granted-Avenger Banner still cannot use the hero action.
Electrostatic Armor uses the completed attack's hero-defense receipt and causal
attacker. Full minion attacks with zero and positive damage, undefended attacks,
non-first controller, recipient placement and maximum are covered. Resourceful
works exhausted and in either form for its controller; using it discards its
physical source and generates exactly one wild toward an actual one-cost play.

Verification: all203 affected tests passed, including entire20 prior inventory
classes, the actual authored-set class, AbilityData classes, rule citations,
ThreatMovePairChoiceTests and ThreatMovePairReviewTests, and the two new classes.
The final restored candidate was rebuilt non-incrementally, then the same
203-test selection passed. Logs are outside the repository under
C:/Users/mggar/AppData/Local/Temp/hulk-customization-*.log.

Behaviorally killed data mutants: remove Avenger condition; remove hero-form
guard (after adding granted-trait alter-ego control); invent a healing prerequisite;
replace causal attacker with current defender; remove hero-defense receipt;
generate mental instead of wild; replace source discard with exhaust. Logs record
compiled test assemblies and intended assertion failures. An initial malformed
THEN mutant was discarded. An earlier valid THEN experiment reported a survivor,
but its log contains apphost copy-lock warnings from another running test
process; that contaminated result is excluded. The independent serial rerun
below kills the exact valid mutant.
The direct healing-prerequisite mutant distinguishes the prohibited dependency.
An accidentally overlapping resource-cost check was discarded and rerun serially.

Beat Cop10029 is deferred: AbilityThreatMoveChoice.Pairs accepts only scheme
destinations, so the actual first action has no offer for its support destination.
The preserved draft and failing actual probe are in the external Temp files
beat-cop-draft-row.json and BeatCopDraftProbe.cs. There is no workaround or broken
Beat Cop row in this packet. No engine, setup, public admission or UI changes.

## Independent review of 3c6bb5bc

The review compared the actual three rows with generated printed text, RR 1.8,
the empty card-ID FAQ results and the two local Armor rulings above. No
executable card-row defect was found. The ruling-search statement was corrected;
the earlier assertion that the local corpus had no relevant interpretation was
inaccurate. Historical ruling scopes do not override current after-attack timing.

Eight additional cases in `HulkCustomizationReviewTests` prove that a granted
Avenger trait belongs to the current identity rather than an ally; Armor follows
its controlling identity defending a different seat and does not respond to that
controller's ally defense; and Resourceful can declare physical, energy, mental
or wild while paying actual Drop Kick. Only the physical declaration earns the
printed physical-only bonus. The exhausted source is discarded to its owner's
pile even when controlled by another seat. These are prepared nonpublic engine
scenes, with no product admission or native-client claim.

Two serial data mutations distinguish the added observations. Fixed physical
generation passed the physical control and failed the other three declarations
with `cannot produce it`. Removing the hero-defense receipt passed the identity
control and failed the ally-defense case when Armor was incorrectly offered.
A first Armor mutation script failed before changing the book; that run is
excluded from mutation evidence. Each valid mutation used the compiled review
assembly, and the original book was restored in `finally` before the next run.
The exact valid THEN mutation was rerun serially as
`{"then":{"effect":{"heal":{"card":"chosen","amount":1}},"then":{"ready":"chosen"}}}`
inside the existing ally choice. It failed the undamaged/exhausted case because
the action was no longer offered; both damaged controls passed (**1 failure,
2 passes**). This is a killed mutant, not an equivalent one. The prior reported
survivor is invalid evidence because its build log records a competing test
process locking the apphost. The serial run used the current compiled review
assembly and the live mutated book, with no other mutation or test in this
worktree.

The result agrees with the current contracts: `AbilityStructuralLowering`
lowers this syntax to a dependent effect with `OnFull = true`;
`AbilityDamageAndThreatExecution.Heal` records actual healed damage through
`AbilityCompletion.Amount`, which returns `None` for zero healing; and
`AbilityStructuralFlowExecution.AfterDependentLeaf` requires `Full` before the
continuation runs. RR 1.8 `rr:then.2` prohibits a post-THEN effect when its
predecessor does not fully resolve. Inspiring Presence instead prints AND, and
`rr:and.1` requires independent effects. Its existing zero-damage paid-action
case distinguishes this decision without inventing a healing prerequisite.

The restored candidate was rebuilt with `dotnet build
tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
--no-incremental`: zero warnings and errors. The complete affected class and
inventory selection, ability-data classes, rule citations, threat-pair classes
and new review class then passed **211/211**. The exact filter and full logs are
outside the repository at
`C:/Users/mggar/AppData/Local/Temp/hulk-custom-review-filter.txt`,
`hulk-custom-review-restored-build.log`, `hulk-custom-review-combined.log`,
`hulk-custom-review-fixedphysical-mutant.log` and
`hulk-custom-review-ally-mutant.log` in that same directory.
An independent raw JSON slice comparison against 4dcd8afb confirmed all 709
predecessor objects exactly preserved and precisely 10030–10032 appended.
The serial THEN log is `hulk-custom-review-valid-then.log` in the same directory.
After that mutation was restored byte-for-byte, the nonincremental Release
build again had zero warnings/errors and the same complete selection again
passed **211/211** (`hulk-custom-review-then-restored-build.log` and
`hulk-custom-review-then-restored-tests.log`).
