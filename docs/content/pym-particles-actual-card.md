# Pym Particles actual card

The canonical `12006` row implements the Hero Response printed in
`datasets/cards/cards.json`: after spending this card, Giant heals two damage
from the hero and Tiny draws one card. The current form selects the effect.
Ruling `46eab153f9f2813b` places the response after paying all costs and before
the paid effect; its historical step numbers map to current initiating-abilities
steps 5 and 6. Ruling `6cb0530e539c7915` and `rr:cost.4.1` distinguish excess
resources from the physical card being spent. The reviewed spend-source
mechanism owns that timing and exact physical contributor receipt.

The previous 752 book objects and raw prefix are preserved; the book has 753
rows and 30 Ant-Man faces. The pack is still incomplete and publicly excluded.
`PymParticlesCardTests` exercises the actual row for Tiny, Giant and alter-ego
wholly excess payment, both physical copies spent before Stinger enters, and
ordinary discard plus optional decline. These are direct payment/entry fixtures,
not public Ant-Man game journeys. The original three form cases now use this
row; synthetic marker responses explicitly replace it in mechanism fixtures.
The separate nested and event-cancellation synthetic catalogs remain labeled.

Windows Release focused Content/data verification passes 61 tests, including all
seven actual Pym cases and the synthetic spend continuation tests. Public catalog
and admission verification passes 12 Server tests; the transport assertion
explicitly excludes Pym from the incomplete pack. Valid runtime JSON mutations
changing Giant healing from two to three and Tiny draw from one to two each
fail their two characteristic cases while five controls pass. The exact book
bytes were restored; a forced non-incremental rebuild and the same 61 tests pass.
These are authored-row mutations, not compiled engine mutations. Logs are under
`Source/evidence/pym-actual-{focused,heal-mutant,draw-mutant,restored-build,restored-tests,catalog}.log`.

Independent review and normal commit hooks remain pending. No complete-pack,
native or replay acceptance is claimed.
