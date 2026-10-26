# Actual Lockjaw face

05018 adds the ordinary own-discard permission, bringing the authored book to
575 faces and 33 of 34 Ms. Marvel faces. Printed authority is the joined card
text: play from your discard pile during your turn, paying the resource cost.
The reviewed prerequisite preserves ordinary cost, discounts, uniqueness,
ownership, ally limit and entry timing; the discarded ally is not a resource.

DiscardPlayDecisionTests now uses the full ability book for the printed
permission. Both second-seat payment variants and four published-offer rejection
controls run against the actual row; the synthetic entry-response variants stay
isolated because Lockjaw has no printed entry response. Existing prerequisite
reviews independently prove live payment purity and single commitment, including
actual Core Nick Fury entry after the ally-limit choice.

The actual-row/permission/card-data filter passes 192 cases. Removing only
Lockjaw's authored permission produces six expected failures in the actual
second-seat and rejection cases; all four synthetic entry controls still pass.
Original dataset bytes are restored and all 192 cases pass again. Logs are
`C:/Users/mggar/Source/lockjaw-actual-{mutant,final}.log`.

Independent actual-row review remains; public Ms. Marvel admission is closed.
