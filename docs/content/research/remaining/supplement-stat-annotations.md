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
