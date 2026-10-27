# Fear No Evil: independent Electro proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository revision
`086153452abc6647a78e42af1166444bbca89aea` and its
[transcription packet](fne-electro-transcription.md), independently of the
candidate author's inspection flags.

**Verdict: accept the printed transcription candidate.** Every captured printed
field matches the 14 original scans viewed individually at original resolution,
including all repeated copies. The inventory is **10 gameplay faces on 14
physical cards**. This verdict establishes neither dataset admission nor
executable support.

## Exact reviewed inputs

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/electro-candidate.json`,
  SHA-256 `989ab97390703aa08c129bef258a47f4beea760c2b0c2830e6d88e940beb00e4`.
- Original manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
  Each original below was opened separately from its external `scans/` directory.
  Its full SHA-256, byte length and acquisition URL match the manifest and
  candidate.
- Independent findings:
  `C:/Users/mggar/Source/evidence/fne-acquisition/electro-proofread-findings.json`.
  These record every viewed filename, complete hash, face, printed number,
  footer position and acceptance result. The separate structural output is
  `electro-proofread-structural-findings.json` in the same directory.

The comparison covered printed title/capitalization, absence of subtitle, type,
traits, uniqueness, every rules word and punctuation mark, ability labels,
italic reminder, flavor, resource glyphs, arrows, choice boundaries, numeric
stats, stat and clause stars, stage, HP/player symbols, counter/player symbols,
amplify and boost icons, absence of boost abilities, illustrator/copyright
credits, card number and set footer. Filenames and author flags did not establish
printed identity or quantity. Source-storage and licensing limits remain those
of the [acquisition packet](fne-acquisition-candidate.md).

## Face and quantity findings

Every row accepts the candidate fields. Every face has an empty `subname`.
Titles below use the documented title-case normalization; `name_printed`
preserves capitals. All footers print ELECTRO with denominator 14.

| Face / original file(s) | Title; type; traits | Printed number / footer position(s) | Quantity and distinguishing verified facts |
|---|---|---|---|
| 60076 / `60076.jpg` | Electro; Villain; CRIMINAL, UNDERLING | 76 / 1 | 1; unique; Roman I; SCH 1 with star; ATK 1 without star; HP 15 per player. Find and attach Electric Charge, placing 2 per-player counters; starred response places a fixed 1 counter after scheming. No amplify or boost field. |
| 60077 / `60077.jpg` | Electro; Villain; CRIMINAL, UNDERLING | 77 / 2 | 1; unique; Roman II; SCH 1 with star; ATK 2 without star; HP 17 per player. Both rules clauses match stage I, including 2 per-player counters and fixed 1 counter. No amplify or boost field. |
| 60078 / `60078.jpg` | Electro; Villain; CRIMINAL, UNDERLING | 78 / 3 | 1; unique; Roman III; SCH 2 with star; ATK 2 without star; HP 20 per player. When Revealed places 3 per-player counters on Electric Charge; starred response places a fixed 2 counters after scheming. One amplify icon; no boost field. |
| 60079 / `60079.jpg` | Electric Charge; Attachment; CONDITION | 79 / 4 | 1; Permanent; ATK modifier box contains only a star. Starred interrupt removes 1 counter before the arrow, gives an additional boost card for this attack, then grants overkill. Hero Action spends an energy resource or 2 resources of any type before the arrow to remove 1 counter. No boost field. |
| 60080 / `60080.jpg` | Drained of Power; Attachment; CONDITION | 80 / 5 | 1; Uses (3 drain counters); 2 boost icons. Attaches to your identity and exhausts each upgrade you control. Readying interrupt's optional energy-or-2-resource payment, drain-counter removal, italic readying reminder and otherwise-does-not-ready sentence all match. |
| 60081 / `60081.jpg` | Whiplash; Minion; CRIMINAL | 81 / 6 | 1; unique; SCH 2; ATK 2; fixed HP 5; 3 boost icons. When attacked, remove 1 charge counter from Electric Charge before the arrow, discard the encounter deck's top card, then deal 1 damage to the attacker per boost icon on that card. Printed quotation matches the separate flavor field. |
| 60082 / `60082.jpg`, `60082_2.jpg` | Charging Up; Side Scheme; no traits | 82 / 7, 8 respectively | 2; each prints fixed starting threat 6, one amplify icon and 3 boost icons. When Defeated places a fixed 3 counters on Electric Charge. Neither number has a player symbol. Italic power-grid narrative matches the separate flavor field. |
| 60083 / `60083.jpg`, `60083_2.jpg` | Chain Lightning; Treachery; no traits | 83 / 9, 10 respectively | 2; each prints 1 boost icon. For each controlled character, choose exhaustion or the complete damage-and-remove-counter bullet. The second bullet deals 1 damage, or 3 instead if any charge counters are on Electric Charge, then removes 1 charge counter. |
| 60084 / `60084.jpg`, `60084_2.jpg` | Energy Overload; Treachery; no traits | 84 / 11, 12 respectively | 2; each prints 2 boost icons. No cards with printed energy in hand grants surge; otherwise choose separately for each such card: discard that card or stun a character you control. Both printed energy glyphs and both choice boundaries match. |
| 60085 / `60085.jpg`, `60085_2.jpg` | Shocking Revelation; Treachery; no traits | 85 / 13, 14 respectively | 2; each prints 2 boost icons. Discard the top card of your deck for each card you control; for each discarded card with printed energy, choose 1 indirect damage or 1 charge counter on Electric Charge. Both choice boundaries match. |

Every repeated copy was read in full; their differences are the printed footer
numerators. The unique physical footer positions are exactly **1–14/14**, each
assigned to one face, with quantities **1, 1, 1, 1, 1, 1, 2, 2, 2, 2**. The
numbered originals and footer positions supply no paired gameplay reverse: every
`reverse_face: null` is accepted in that limited sense, without a claim about
generic card-back artwork. No packaging or marketing count is required.

## Stars, symbols and normalization

No Electro face prints a boost star, boost separator or boost ability. Special
SCH stars on the villains and the clause stars are distinct from boost icons and
player symbols. Electric Charge's star-only ATK box correctly uses
`stat_annotations.ATK+` with `value: "★"` and `special_star: true`; it contains
no printed numeric zero or modifier value to invent. Whiplash's SCH and ATK have
no reminder stars.

The villain reveal clauses use per-player charge quantities; their scheming
responses use fixed quantities. Charging Up's 6 starting threat and 3 counters
are fixed. `[energy]` accurately identifies the lightning-bolt resource glyph on
Electric Charge, Drained of Power, Energy Overload and Shocking Revelation.
Whiplash's inline `[boost]` is the three-pronged boost glyph, and `[amplify]`
accurately preserves the large plus-and-boost icon on Electro III and Charging Up.

Layout-only wrapping, title case, trait-array spelling, HTML ability/reminder
markup and named glyph tokens are documented editorial normalization choices.
The arrow and choice bullets remain explicit. Chain Lightning's counter removal
stays inside its second bullet; Energy Overload's and Shocking Revelation's
per-card choices retain their printed scopes. Flavor stays separate from rules
text. Illustrator credits are Joey Vazquez on all three villains and Charging
Up, Ivan Fiorelli on Drained of Power, and absent on the other faces. The printed
copyright line matches throughout. No unresolved word, numeral or glyph remains.

## Verification and limits

The external acquisition `verify.py` passed offline for all 201 pinned scans and
the primary acquisition inputs. The existing Electro structural validator also
passed; it was evaluated with only its output filename redirected in memory to
the separate proofread structural findings, leaving its source and the original
transcription findings unchanged. That validator's semantic-review-pending status
describes the validator's own limited role; this review supplies the independent
visual comparison.

Additional independent assertions passed for the exact candidate and manifest
hashes; 10 unique expected face IDs; the exact 14-scan Electro manifest section;
original byte lengths and hashes; expected footer/face assignments, quantities
and coverage metadata; field-source pointer resolution and every copy's
hash/number/footer identity; absence of supplied gameplay reverse pairs; boost
metadata; and `Printed.Plain`-compatible normalization. Structural checks do not
substitute for the original-scan comparison recorded above.

No transcription correction or unresolved printed-field conflict was found.
The candidate, originals, acquisition inputs, source scripts and transcription
findings remain unchanged. No dataset, supplement, generated output, extractor,
runtime, setup or ability file was edited. Deliberate acquisition handling of
research metadata and quantity remains a separate task; acceptance of this
transcription is not a drop-in import claim. Build/unit lanes are unnecessary for
this documentation-only proofread; normal commit hooks supply repository
documentation checks.
