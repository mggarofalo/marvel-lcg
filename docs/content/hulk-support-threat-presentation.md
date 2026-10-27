# Support threat presentation

The complete Hulk candidate exposes Beat Cop's live threat through the existing
`CardStateDescriptor.Threat`, but the board formatter restricted its display to
schemes. The formatter now displays supplied live threat on other card kinds as
well. It does not infer counters from printed text or calculate an ability result.
The existing suppression of unrelated raw non-scheme `k_threat` fields remains;
the typed live state is the authority for those cards.

Four distinguishing tests cover zero and three live threat, with and without a
duplicate raw field. They require exactly one displayed value. The original
formatter compiles and fails all four, while both existing scheme cases pass.
After exact restoration and a non-incremental build, all 430 View tests pass.
Evidence: `C:/Users/mggar/Source/evidence/hulk-support-threat-view*.log`.

This is managed presentation evidence. Independent review and a rendered actual
Beat Cop interaction remain required before claiming the native path verified.
