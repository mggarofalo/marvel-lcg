# Captain America and Wrecking Crew delivery

Candidate implementation: `edc6d13e` on `feat/wave-wrecking-engine`, including
main `80ef568b`. Owner acceptance is pending. This record supersedes the admission
status in earlier implementation checkpoints, without expanding their evidence.

## Delivered scope

- Captain America: all 35 playable faces, printed starter, obligation and nemesis.
  MARVEL-629 is complete under the owner's explicit scope: complete playable
  content and the verified starter; divider and packaging counts are not gates.
- Wrecking Crew: all 60 faces, four independent encounter groups, Standard,
  Expert, Extreme and all 16 custom A/B recipes. Public creation uses the same
  scenario preparation as the verified research games.
- Whole-pack admission rejects missing faces or incomplete/wrong recipes.
  Adjacent unsupported packs remain excluded; custom player decks remain possible.
- Escaped Convict uses the captured causal attack origin through nested schemes;
  a standalone scheme supplies no attack. Replay version 22, protocol 37 and
  save version 7 are explicit compatibility boundaries.
- The later expansion inventory is tracked as 64 pack issues in 11 waves,
  MARVEL-559–622, through Fear No Evil, covering hero, scenario and campaign packs.

## Verification and revision scope

Windows local evidence is under `%TEMP%`; it is not a claim of Linux CI or a
published signed artifact. Logs are retained outside the repository.

| Revision | Check | Result / log |
|---|---|---|
| `69c0a621` | Release build; unit solution; acceptance project | Clean build, 5,750 unit and 122 acceptance passes; `wave-admitted-{build,unit,acceptance}.log` |
| `3527d68c` | Public creation regressions | 19 recipes × 1/2/4 seats, opening/save/restart; ordinary Core/RoRS digest and full RNG compatibility |
| `7d88eb29` | Integration solution | Server 435, Godot 959, Architecture 129, Release 36 pass; one existing native skip; `wave-final-integration.log` |
| `7d88eb29` | Godot and presentation walls | Forbidden configurations rejected and allowed contracts compile; `wave-final-{godot,presentation}-wall.log` |
| `edc6d13e` | Debug build and two live native manifest cases | Both pass; `wave-integrated-final-native/results.jsonl` |

The live native cases exercise real viewport input through public setup: Captain
America's opening hand, form change and attack on inactive Bulldozer; and custom
B/A/B/A selection through explicit Start, exact stages/HP, revision zero and the
pending opening-hand decision. They belong to representative and exhaustive CI.
Earlier rendered evidence for the live attack is in `wave-native-rendered-final`;
the custom selector check is headless, one player, 100%, reduced motion.

Eight Captain games and eight Wrecking games have per-decision replay evidence.
The Wrecking games end in defeat; they do not establish victory or Extreme
promotion. Those mechanics have focused behavioral checks. Independent review
and distinguishing mutants cover the authored mechanics, admission boundaries,
selected-discard precommit safety and captured attack ancestry.

The genuine Expert predecessor archive is unchanged (SHA-256
`1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B`).
Managed migration comparisons retain all attempts and proofs. Native inspect,
decline and fresh-process restart preserve history and the original backup;
restart compares parsed JSON serialization, not archive byte identity. Native
recover is not claimed. Genuinely incompatible older traces still reject.

## Acceptance

CI must verify the published PR revision on its configured platforms. The owner
can explore Captain America and Wrecking Crew from ordinary New Game setup,
including custom stages, and identify missing behavior or refine requirements.
Automated regression work is not assigned to the owner. Acceptance and release
remain separate from the passing implementation evidence above.
