# Multi-face identity setup

`pack:mc12:ant-man-hero-pack#foldable-cards` describes Scott Lang/Ant-Man as one foldable identity with one alter-ego and two hero sides. The generated card catalog identifies `12001a` and `12001c` as hero faces and `12001b` as alter-ego. `rr:appendix-ii-setup.step.1` says each player places the alter-ego face up.

`IdentityFaces.StartingSpec` accepts distinct identity faces with exactly one alter-ego and at least one hero; it rejects repeated, missing and nonidentity faces. It places the alter-ego first and preserves the relative order of hero faces. The latter ordering is an engine format choice. For the printed input `12001a,12001b,12001c`, the starting specification is `12001b,12001a,12001c`. Two-face identities preserve their existing orientation.

Focused Rules tests use kinds read from the generated card catalog, exercise both hero orders, ordinary two-face identities, and malformed controls. Removing duplicate validation caused the duplicate-hero control to fail. This is a setup prerequisite, not public admission of Ant-Man or evidence of a complete game.

Verification: `IdentityFacesTests` passed 13 cases after restoration; `SetupAdmissionTests` passed 11 existing cases. Logs are `C:/Users/mggar/Source/multiform-setup-restored.log`, `multiform-setup-mutant.log`, and `multiform-setup-admission.log`.
