# Hank Pym entry explanation

The printed authority is `datasets/cards/cards.json` row `12011`: Ant-Man gets +1 hit point per pym counter, and his optional entry interrupt places one per resource overpaid, maximum four. `rr:cost.4.1` identifies excess generated resources as not paid for the cost; `rr:ability.11` makes the interrupt optional.

The checked counter instruction and matching constant health grant provide the description. The entry payment receipt supplies the current count; this is an instruction preview, not a prediction of later prevention, other effects or survival. Declining this interrupt places no counters. The card-play offer also describes the optional interrupt before payment, with no predicted counter count or survival. The presentation uses the existing affordance label and description; gameplay and protocol schemas are unchanged.

Focused Content tests pass for one, two and capped excess, optional decline wording, a conditional health-grant negative control, and read-only description including RNG and pending work. A probed continuation matches an unprobed control. The View test passes for the actual row projected to the answering player. Removing the cap from the descriptor compiled and failed the capped case (six instead of four); a non-incremental restored build passed all five Content cases. Native evidence and independent review remain outstanding.

The prepayment description is omitted for unrelated cards and conditional entry instructions. Removing that condition guard compiled and failed its negative-control test; the restored non-incremental build passed all seven focused Content cases. Two focused View cases passed, including the card-play offer projection. The registered work item is MARVEL-890.
