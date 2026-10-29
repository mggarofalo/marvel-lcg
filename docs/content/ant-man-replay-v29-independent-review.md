# Replay v29 independent review

Reviewed `9994b4f61e4a91d10e162eeb1a1d865ce5c9729e` and the accompanying
v29 guard tests. The named admission matrix retains historical destinations and
admits only the frozen v19, v20, v21 and same-v29 sources to v29. V22–v28
sources have no v29 route. Runtime compatibility still checks application
downgrade, RNG and digest before opening a game; source fingerprints and strict
complete replay remain required. The v20 and v21 positive traces are verified
derivatives of the genuine v19 capture, not independent historical captures.

The external comparison archive has SHA-256
`945e23150acbbef5583c0e03855555e4730fe8139209962e384b32dba7169be8`.
All ten files in the minimal v28 rejection fixture match the corresponding
archive entries byte for byte. The fixture hash is
`1a27eb1c902eacc982f8e6da5cae4602061269d7143b6712271b7efcbd64003d`.
The production-host rejection tests authenticate the captured synthetic owners,
restart twice, and compare all durable non-lock files and upgrade publications.
The archive also contains a nonempty compatible diagnostic control; it is not
evidence of a production v28 migration, inactive redo, or campaign acceptance.

The played and expert campaign recordings now fail strict replay at earlier
Power of Justice prompts. The removed elimination and confused-thwart probes
therefore no longer have verified current prefixes; their later historical
causal observations are no longer executable evidence. Whole-archive and import
rejection remain tested. Existing elimination placement and confused-thwart
tests retain the underlying rule coverage without rewriting the recordings.

Independent Release execution passed 105 focused server cases, including the
new v29 identity and runtime guards, original-fact controls, genuine v28
quarantine, campaign rejection, and `PredecessorProcessContentUpgradeTests`.
The latter compares the genuine frozen Core predecessor continuation's complete
authorized response, adjusting only its protocol version. Eight focused release
policy cases also passed. This review found no production defect in the stated
replay boundary; it does not certify untested v28 migrations or broader product
readiness.
