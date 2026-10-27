# Fear No Evil: pinned acquisition candidate

Research for MARVEL-697, 2026-10-27, repository baseline
`f199e5fe201b82eed8061baf4126a98daad614b9`. This resolves the next acquisition
decision for MC60 only. It does not admit card text, setup or runtime behavior.
The [existing inventory](617-622-inventory.md) remains the joined catalog ledger.

**Use the complete local scan candidate for a separate transcription and review
change.** Neither the current MarvelSDB source nor the live MarvelCDB pack API
supplies any of the missing scenario range. This is a result for the sources
inspected below, not a claim that no other transcription exists anywhere.

## Machine-readable sources inspected

| Source | Exact retrieval pin | Result |
|---|---|---|
| [MarvelSDB JSON source](https://github.com/zzorba/marvelsdb-json-data/tree/44b7f2c9b01faa50f5a96cb40a9c0ecbbace7776) | Master resolved to `44b7f2c9b01faa50f5a96cb40a9c0ecbbace7776`, committed 2026-09-23. Recursive tree and commit response retained locally. | `pack/fne.json`: 58 rows; `pack/fne_encounter.json`: 10 rows. Same 68 codes as the repository snapshot, ending at `60064`; zero rows in `60065`–`60210`. Encounter file is byte-identical to the vendored file. The sole changed player record is `60018.text`, requiring separate authority review if acquired. |
| [MarvelCDB pack API](https://marvelcdb.com/api/public/cards/fne.json) | Response SHA-256 `74c8e0c81b14a006aa62588462bdadc924ddd3213205e97306a266caeb0c3c1a` | 66 top-level rows, ending at `60064`, zero scenario rows. The two identity backs `60001b` and `60037b` are absent as top-level rows; do not interpret this API shape as complete face coverage. |
| [marvelcdb CLI](https://github.com/mggarofalo/marvelcdb-cli/tree/9e97e7b3005f04653c7dd6a49363bfb5e19dc253) | README at commit `9e97e7b3005f04653c7dd6a49363bfb5e19dc253` | The CLI was absent from PATH and the inspected `uv tool list`. Its README describes fetching/caching MarvelCDB's corpus and filtering locally; it supplies no independent transcription. Inspected capability without installing or warming a mutable cache. |

The pinned upstream recursive tree contains no license, copying or copyright
file. No new redistribution grant is established. Printed cards remain FFG/Marvel
copyright; the scan sample visibly carries Marvel 2026 / FFG notices. Local
research storage does not establish a license to distribute scans.

## Complete local source candidate

Evidence root: `C:/Users/mggar/Source/evidence/fne-acquisition/`.

The [Hall of Heroes released-card page](https://hallofheroeslcg.com/fear-no-evil/)
was downloaded as `hoh-fne.html`. Its scenario image elements yield **201 distinct
URLs/files, 171 filename-derived face IDs, and every one of the 146 card numbers
60065–60210**. All 201 images were downloaded into `scans/`, SHA-256 pinned and
successfully decoded as images. `scan-manifest.json` records the exact source
URL, filename, section, inferred face ID, byte length and hash for every file.
`scan-locator.json` preserves the parsed locator; `coverage-summary.json` preserves
the following accounting. These are acquisition metadata, not printed-text rows.

| Page section | Number range | Files | Candidate faces | Provisional physical slots |
|---|---|---:|---:|---:|
| Bullseye | 60065–60075 | 15 | 12 | 14 |
| Electro | 60076–60085 | 14 | 10 | 14 |
| Hammerhead | 60086–60096 | 14 | 11 | 14 |
| Purple Man | 60097–60109 | 15 | 13 | 15 |
| Typhoid Mary | 60110–60120 | 18 | 14 | 15 |
| Kingpin | 60159–60176 | 27 | 23 | 22 |
| Art Museum Heist | 60121–60127 | 10 | 8 | 9 |
| The Getaway | 60128–60133 | 10 | 8 | 8 |
| Protection Racket | 60134–60141 | 14 | 13 | 9 |
| The Raft Breakout | 60142–60150 | 10 | 10 | 9 |
| Stop The Presses | 60151–60158 | 10 | 9 | 9 |
| Disasters | 60177–60181 | 5 | 5 | 5 |
| Cops | 60182–60185 | 5 | 4 | 5 |
| Drive | 60186–60190 | 5 | 5 | 5 |
| The Owl (page label “The Owel”) | 60191–60194 | 5 | 4 | 5 |
| Tombstone | 60195–60199 | 5 | 5 | 5 |
| Tracksuit Mafia | 60200–60204 | 7 | 5 | 7 |
| Campaign | 60205–60210 | 12 | 12 | 6 |
| Total | 60065–60210 | 201 | 171 | 176 |

The provisional column subtracts one opposite-side scan for each of 25 a/b
groups from the image count. Thirty additional images repeat a filename-derived
face. Neither rule proves physical quantities without reading the printed set
positions. There are no missing card numbers or unmatched a/b groups in this
locator, but image names alone cannot prove there are no omitted variants.

The official 28-page insert was copied from the previously inspected local PDF,
not downloaded from legacy game code or interpreted from that game's behavior.
`mc60_rulebook-web.pdf` is 40,688,066 bytes with SHA-256
`18b29a608418f41b5b690109bf43de29f31c2b6567c8e4dd5c2fee71c3b78b2c`, matching
`datasets/rules-packs/sources.manifest.json`. Its published document is also
[linked from Hall of Heroes](https://hallofheroeslcg.com/wp-content/uploads/2026/08/mc60_rulebook-web.pdf).
Page 2 was rendered and inspected: its component breakdown is 89 player cards,
16 villain cards and 171 encounter cards. Page 13 was rendered and inspected
against the Protection Racket sample below.

**Playable quantity reconciliation remains open.** Existing upstream face-row
quantities sum to 103; removing the two identity back rows gives a provisional
101 physical cards. Adding 176 scan-derived slots gives 277, one above the
insert's 276. This arithmetic identifies a review question, not an extra card to
delete or an assertion that the insert is wrong. Check printed set-position
coverage and repeated scans before assigning quantities. Divider, token and
marketing reconciliation is outside this task.

## Bounded visual sample

`sample/` holds 12 original scan files; `sample-contact.png` is the inspected
contact sheet. Ten distinct faces across six numbered components were read:

| Scans | Verified printed fact |
|---|---|
| `60065-1.jpg` | Bullseye, villain stage 0; footer position 1/14. |
| `60068a-1.jpg`, `60068b-1.jpg` | Adamantium-Laced Spine / Damaged Spine; both footer position 4/14; printed numbers 68A / 68B. |
| `60071-1.jpg`, `60071_2-1.jpg`, `60071_3-1.jpg` | Incessant Pursuit; same printed card number 71, consecutive footer positions 7/14, 8/14 and 9/14. These establish three copies for this sampled face. |
| `60110a.jpg`, `60110b.jpg` | Typhoid Mary / Bloody Mary, villain faces; both footer position 1/15, printed 110A / 110B. |
| `60135a.jpg`, `60135b.jpg` | Protection Racket 1A / Bull in a China Shop 1B; both footer position 2/9. Insert p.13 independently requires five Protection Racket main schemes and the named Disasters/Tracksuit Mafia modules. |
| `60205a.jpg`, `60205b.jpg` | Art Museum Heist campaign environment, completed / failed sides; both footer position 1/6. |

This validates the acquisition route and representative copy/side distinctions.
It is not a review of all gameplay text. All **171 faces still need structured
transcription and independent proofread**; 161 have not received semantic visual
review in this packet. Every physical quantity needs printed-footer accounting,
with the three-copy 60071 sample as a verified starting point.

## Reproduce and continue

`provenance.json` pins source URLs, upstream commits, retrieval timestamp, license
findings and all primary local inputs. Its SHA-256 is
`49825e18cf4f83db51a2946cfab971501f8854cdb405a7a47dfa197540ae2fee`.
The upstream FNE player/encounter file hashes are respectively
`1bf8c64b2461081a7398cefd390881a57cff4e2b52d9106c090795aa593a03ab` and
`08e8cee130f1e413a0626ac7f13376a856e78f90d83825654e8c1230093a97a8`.

Offline verification requires only Python's standard library:

```powershell
python C:/Users/mggar/Source/evidence/fne-acquisition/verify.py
```

It verifies every primary input and all 201 scan hashes, asserts exact number
coverage and the 171-face count, and confirms both machine-readable sources have
zero scenario rows. With the local manifest preserved, missing scans can be
reacquired by exact URL and accepted only if their hash matches:

```powershell
python C:/Users/mggar/Source/evidence/fne-acquisition/verify.py --download
```

The upstream candidate can independently be reacquired at its immutable commit:

```powershell
$candidateRoot = 'C:/Users/mggar/Source/evidence/fne-acquisition'
$sourcePin = '44b7f2c9b01faa50f5a96cb40a9c0ecbbace7776'
Invoke-WebRequest "https://raw.githubusercontent.com/zzorba/marvelsdb-json-data/$sourcePin/pack/fne_encounter.json" -OutFile "$candidateRoot/upstream/pack/fne_encounter.json"
python "$candidateRoot/verify.py"
```

Next bounded implementation: transcribe one complete encounter family including
each printed stat/icon, trait, ability, boost text, side relationship and copy
position; independently compare it with these pinned scans. Repeat by family to
complete all 171 faces and reconcile playable quantities. Then submit a deliberate
offline-input acquisition change using the existing vendored/supplement mechanism,
with source attribution for every correction/addition, regenerate the joined
catalog and run extract `diff`, `write`, and `check`. Generator schema work, if
needed for an observed printed fact, belongs to that separate change. No dataset,
ability book, setup record or runtime file was changed by this packet.

The downloaded scans/PDF, pinned upstream bytes, manifests and verification script
are usable offline now. Structured card text is not yet regenerable because the
missing transcription has not been authored; no build should fetch these URLs.
