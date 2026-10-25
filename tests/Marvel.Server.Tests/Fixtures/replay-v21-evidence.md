# Replay-v21 predecessor evidence

`replay-v20-9097da07.zip` was produced using unmodified runtime commit
`9097da07`. SHA-256:
`76abf6271e0c8b1552a2ea220ca3a7710e9d0ee6765bdfba0476933062c2e511`.
The embedded producer and README record the complete process. Production v20
CampaignHost migrated a copy of the actual 910 played-campaign fixture, restarted
all 13 attempts and exported it. Every original gameplay proof and authority was
preserved. The resulting source-bound v20 aggregate contains 786 decisions,
two completed scenarios and an active Taskmaster attempt. Its v19 backup is retained.
Production v20 EngineHost also independently recorded and restarted all 30 legal
Black Panther/Rhino standard seed700 decisions from the pinned predecessor trace.

`retaliation-v19-91098022.zip` contains the same 30-decision Black Panther line
published and restarted through unmodified runtime `91098022`'s EngineHost.
SHA-256: `2077dda898d36a1c55ec9285557e59aa7d0f3a2f7669e3324df9013f6d09547e`.
Its producer checks the actual runtime is v19; the console's v20 label is an
inaccurate label only and has no role in saved compatibility or verification.

Tests extract copies and never replace these archives. Under replay v22,
PlayedCampaignReplayUpgradeTests proves the genuine v19/v20 aggregates diverge
on nested reveal ordering and are rejected on restore and portable import, with
source files unchanged through restart. The authentic five-decision Taskmaster
attempt upgrades and passes strict replay independently. This is positive played
attempt evidence; a compatible complete predecessor campaign aggregate remains
outstanding. These capabilities belong only to generated test games.

RetaliationReplayUpgradeRejectionTests proves both genuine predecessor Black
Panther histories fail strict original-event verification and remain byte-identical
through two production-host restore attempts. Evidence is standard-mode only and
does not establish expert or nonzero-delay campaign coverage.
