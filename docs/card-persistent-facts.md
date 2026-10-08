# Persistent card facts

`CardDescriptor.Persistent` describes one authorized in-play source. Its
`Source` retains the title and inspectable rules, with a current target id.
`BoardCardPresentation.Persistent` passes the same facts to renderers.
These descriptions are observations, not action offers or permissions.

`PersistentCardFacts` owns the physical relationship. The checked ability
program supplies the supported effect meanings through
`ICardPersistentAbilities`; it does not read printed prose to infer effects.
View applies the same source-authorization policy used for effective values,
then copies facts into View-owned transport records. No projection executes an
ability or changes card state, RNG, timing, or gameplay digest.

## Relationships and current contributions

`Relation.Kind` is `Attached`, `Controlled`, or `Shared`. `HostId` identifies a
readable attachment host; `Controller` identifies a controlling seat. A host
storage area alone does not establish textual attachment. Mark V Armor and Arc
Reactor are controlled upgrades. The DSL's identity-limit placement selector
implements play under another player's control; Combat Training is controlled
as well. Inspired is attached to its ally. Upgraded Drones is attached to the
Ultron Drones environment; its affected characters are separate recipients.

`Contributions` copies authorized calculation steps from effective values. Each
row contains `TargetId`, the printed attribute key, `Operation`, signed
`Amount`, and optional `Duration`. A recipient's displayed effective total
**already includes** these contributions. `HP` means maximum health; a +HP row
never describes healing. `DefineBase` defines an intrinsic base and is not
`Add`. Expired effects disappear on the next snapshot. Historical sources keep
their existing effective-value inspection record but do not become live source
rows or links to a returned physical copy.

## Supported ability descriptions

An ability contains `Trigger`, `Costs`, and ordered `Effects`. Trigger retains
the checked timing type, event, subject, actor, target, form, player relation,
and explicit `AnyPlayer` flag. The flag is authored metadata, not a complete
access or legality decision. Engine offers remain the authority for activation.

Targets use roles: `Source`, `Host`, `ActingIdentity`, `ActingPlayer`,
`TriggerActor`, `TriggerSubject`, `TriggerTarget`, and `ChosenScheme`.
The acting identity belongs to the player resolving the ability; it is not
necessarily the source's controller. This preserves Concussion Blasters'
hero-exhaust cost and Arc Reactor's source-exhaust cost as different facts.

Supported costs are `Exhaust`, `Discard`, `RemoveCounters`, and
`SpendResources`. Counter costs carry a positive `Amount` and `Counter` name.
Resource costs retain canonical resource letters and the `PrintedOnly`
restriction. Current counter quantities and damage remain on the source's
existing card-state/board fields.

Supported effects are `Ready`, `Exhaust`, `Discard`, `RedirectAllDamage`,
`RemoveThreat`, and `Grant`. Nullable `Amount` and `Field` retain the checked
verb's operands. `Until` bounds a grant's duration; `After` schedules later
execution. A `Condition` supplies a target, counter, and inclusive minimum
threshold, evaluated after earlier effects in the ordered list. Effect amounts
and thresholds are signed engine quantities, not a universal positive bonus.

For Armored Rhino Suit, `RedirectAllDamage` to `Source` precedes a conditional
`Discard` at source damage >=5. Five is a discard threshold checked after the
entire damage packet is redirected, not a capacity, extra HP, or prevention
with spillover. Charge's overkill grant has `Until=EndOfAttack`; its discard has
`After=WhenAttackEnds`. Spider-Tracer retains the host-defeat trigger and the
chosen-scheme target for removing three threat.

## Deliberate limits and visibility

This is a bounded description of existing checked operations, not a second
ability interpreter. A compound ability is described only when its whole
supported effect and cost are understood. Unhandled conditions, labels,
limits, choices, or verbs omit that whole ability and set
`HasUnresolvedAbilities`. The card's title and rules remain available for
inspection; a renderer must not substitute a plausible flat bonus. Constant
basic-stat modifiers and intrinsic definitions use the existing effective
calculation rather than a duplicate description. Entry/setup/boost effects do
not describe a currently persistent ability.

An unreadable source has no persistent facts. An unreadable recipient has no
contribution or host link. Replacement Drone identities never expose the
underlying printed player card through source titles, face ids, rules, or links.
Consumers use these filtered facts equally for visible labels, inspection,
highlights, and accessibility descriptions.

The runtime remains Core-only. Research expansion cards are not enabled by
these records. The public protocol compatibility/version decision and transport
validation belong to the integrated server/client change; the records do not
change saved game state or replay identity.
