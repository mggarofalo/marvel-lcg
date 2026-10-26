# Worthy search meaning

The actual Odinson row exposes a named search across the controller's deck and
discard pile. Its engine offer and pending choice now name Mjolnir, both areas,
the hand destination and the unconditional deck shuffle. Description generation
uses compiled selector/effect structure, never hidden candidates or deck order.

The row uses the established conditional search shape: matching choice adds the
selected physical card to hand and shuffles; no match also shuffles. Its own-area
selection and per-round limit are unchanged. This matches printed Worthy and lets
the existing pending-choice description retain the shuffle instruction. Only this
row is normalized; no whole-book serialization or product admission changes.

Three actual-row assertions failed on null description fields before the fix.
Fourteen focused Thor search, Core search and Captain America setup checks pass.
A compiled mutant describing only the deck fails all three Worthy cases; the
original source is restored, rebuilt nonincrementally, and the same14 pass.
Both physical choices, unrelated/foreign areas, no-match shuffle and form/phase
limit controls remain. Evidence is external: C:/Users/mggar/Source/thor-search-
{red,mutant,restored-build,restored}.log.

This proves engine meaning, not rendered Thor UI or whole-pack acceptance.
Independent review follows before integration. Existing Core search wording and
commitment checks remain part of the focused profile.
