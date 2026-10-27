# Supplemental card stat annotations

Full-card supplemental entries preserve `stat_annotations` through the existing
reader and generated-card writer. Numeric engine attributes remain independent
of special stars, star-only values, boost stars and per-player display facts.
This is an acquisition-format repair required by the independently proofread
Fear No Evil Bullseye family; it does not import or admit that family.

The pure `Supplement.Read(JsonElement)` entry point shares the file reader's
implementation. Tests use friend access to the extractor assembly, with an
explicit reference alias to avoid exposing its linked RepositoryPaths helper
alongside the test project's own linked helper. No production types are made
public solely for tests.

Verification: 11 focused extraction/correction tests pass; offline `-- check`
confirms committed cards.json and search.json remain byte-identical. Replacing
the annotation reader with an empty dictionary compiles and fails the positive
extraction test while the no-annotation control passes. Exact source restoration,
a forced nonincremental build, and all 11 focused cases pass afterward.
Logs: `C:/Users/mggar/Source/evidence/supplement-annotations-*.log`.

The initial test access attempt failed compilation before execution; it is not
mutation evidence. No dataset, printed text, setup or ability row is changed.
Independent review is required before integration.

## Independent review

Reviewed commit `8cf73d300bb53c7961cb2fba6455573941366e34` in a separate,
detached worktree on 2026-10-27. **Verdict: accept this tooling repair.** No
blocking finding remains for the supplemental full-card path.

The file reader delegates to the same pure reader exercised by the tests, and
the returned cards retain strings and typed values rather than document-owned
JSON elements. The existing annotation reader preserves arbitrary attribute
keys, including attachment modifier keys, symbolic values and explicit
`per_player: false`; its ordinal dictionary keeps generated ordering stable.
Numeric attributes, linked-card handling, correction-row merging and dropped
cards retain their existing paths. This change does not add annotation merging
to correction rows.

Friend access keeps the extractor types internal. The explicit assembly alias
also keeps its linked `Marvel.Tests.RepositoryPaths` out of the test project's
unqualified lookup. A fresh Release build of the complete content test assembly
and its 11 focused extraction/correction tests passed independently. The offline
generator check independently confirmed byte-identical cards.json and
search.json; the worktree remained clean after both runs. Evidence is in
`C:/Users/mggar/Source/evidence/supplement-annotations-independent-tests.log` and
`supplement-annotations-independent-check.log` in the same directory.
Independent pre-commit gates also passed: all 66 architecture-policy cases,
complexity and file length. Their log is
`C:/Users/mggar/Source/evidence/supplement-annotations-independent-hooks.log`.

This review accepts acquisition-format preservation only. It does not import
the Bullseye candidate, certify its source inventory, admit content, or establish
runtime support.
