# Transient public reveal independent review

Reviewed source: `eb6f428dad5560c0d61e89f3a51aa717ac0de109`.
Verdict: reject pending correction of captured-face presentation below.
Closure verdict: accepted for the captured-text correction at
`0d55160ca3f778dccc6c7e1ddada7881d0eae3d3`; see independent closure below.
This is a View/event/history contract review, not native UI, owner acceptance,
complete-game evidence, or acceptance of the whole Doctor Strange pack.

## Finding

`src/Marvel.View/EventFlipPresentation.cs:23` obtains reveal rules text from the
final card descriptor. `EventSubjectDescriptions.Card` now correctly prefers
the occurrence face for the title. Together they can describe one historical
face using a later face's rules text. A synthetic public reveal of `01001b`
(Peter Parker), followed by turning that same physical card to `01001a`, emits
`Revealed Peter Parker: Spider-Sense ...` instead of the captured Scientist
resource text. The current Invocation producer is single-faced; that does not
make the generic disclosure contract correct.

`PublicRevealIsolationTests.HistoricalRevealUsesItsCapturedFaceTextAfterLaterFormChange`
preserves the failing observation. The exact authorized printed text is already
available at `WorldDescriptor.EventFaces[faceId].RulesText` on
`BoardCardPresentation`, indexed by the flip's `SubjectFaces[objectId]`.
The new test also distinguishes the captured title from the later snapshot.

## Retained boundaries

Three independent synthetic cases cover spectator, owner and other-seat scopes.
An earlier grant does not authorize a later ungranted flip, a movement, a reorder,
or a new incarnation of the same object. The final concealed deck exposes
neither that object's id nor an unrelated hidden object. Only the captured face
enters `EventFaces`, and export removes `PublicFaces`.

Source inspection confirms the grant producer is solely
`InvocationDeck.RefreshTop`. `PublicRevealProjection` intersects grant keys with
that flip's cards and rejects facedown grants. It grants no addressability to
later events. Existing adversarial tests retain unrelated-grant, private-hand,
both physical owner positions, permissive/restricted/spectator and query-purity
coverage.

Replay inspection is reconstructed, not trusted from raw journal metadata:
`SessionReplayVerification.ResolveReplayDecision` resolves the saved decision,
checks its exact JSON events, RNG count and fingerprint, and returns
`resolved.Events`. Those regenerated events flow through
`ReplayHistory.ObserveEvents` into `HistoryUnitInspection`.
The `[JsonIgnore]` occurrence authority does not enter serialized event JSON;
the existing direct comparison and completed fresh-host public regression pass.
No schema, protocol, digest or RNG changes are present in the reviewed patch.
The existing coarse actor/scope history restriction remains in force.

## Verification

All executions used the Windows PTY path. Logs are outside the repository:

- `C:/Users/mggar/Source/invocation-transient-review-initial.log`: source plus
  new tests, 3 passed and the captured-text test failed with the mixed title/text.
- `C:/Users/mggar/Source/invocation-transient-review-view.log`: complete View
  suite excluding that explicitly reported failing case, 423 passed.
- `C:/Users/mggar/Source/invocation-transient-review-server.log`: selected Server
  history/privacy/public Doctor Strange restart classes, 38 passed.

A temporary correction used captured `EventFaces` text. Its compiled View run
passed the new regression but failed two existing literal `face up` assertions
because it also expanded concealed-history summaries to include printed text
(422 passed, 2 failed). The production edit was restored; its exact diff is
preserved separately at
`C:/Users/mggar/Source/invocation-transient-review-suggested-fix.patch`, and its
log is `C:/Users/mggar/Source/invocation-transient-review-view-fixed.log`.
The lead owns the correction and separate closure review. No production change
or assertion relaxation is included in this review branch.

## Lead captured-text correction

The integrated correction keeps expanded rules text limited to currently readable
objects, preserving concise summaries for concealed historical reveals. When an
authorized occurrence face is present, expanded text comes from that captured
EventFaces presentation, matching the historical title rather than the later form.
The independent failing form-change case now passes without relaxing its assertions
or the concealed-reveal wording controls. All 426 integrated View cases and 89
selected whole Server history/restart/compatibility cases pass. Evidence:
`Source/evidence/strange-transient-captured-text-integrated-view.log` and
`strange-transient-compat-integrated-server.log`. Independent closure review
remains required for this lead correction.

## Independent captured-text closure

Reviewed integrated source: `0d55160ca3f778dccc6c7e1ddada7881d0eae3d3`.
Verdict: accept the narrow `EventFlipPresentation` correction. The original
captured-title/later-text finding is closed. This supplements the independent
review at `bec69d6b`; it does not repeat or broaden that review's acceptance.

The correction resolves both the historical title and expanded rules text from
the flip's `SubjectFaces` entry and the corresponding authorized `EventFaces`
presentation. The existing form-change regression asserts the captured title
and captured rules, first proves the later face has different rules, and rejects
those later rules in the summary. It distinguishes the correction from the
reviewed faulty implementation; no duplicate test or temporary mutation was
needed for this closure.

The enclosing enumeration still requires a matching card in the visible world,
and `RevealedRules` returns no rules when that card has no readable face.
Concealed historical reveals therefore retain their concise `face up` wording.
Neither absent nor face-down current cards acquire expanded text from an
occurrence reference. With no authorized occurrence presentation, the existing
currently readable face supplies the text. Face-down and boost summaries still
return before rules expansion. The correction adds no disclosure grant, live
card lookup, engine behavior, serialization, save, digest or RNG change.

Source inspection retained the existing authorization path:
`WorldProjection` builds `EventFaces` only from visibility-filtered events;
`PublicRevealProjection` intersects public grants with that flip's own cards
and rejects face-down grants; `VisibilityEventFilter` strips `PublicFaces` on
export. The focused suite covers spectator and both restricted seat scopes,
concealed private hands, unrelated grants, later ungranted events and another
incarnation, as well as query purity and unchanged event JSON. No guard was
widened by this correction.

Independent Windows Release verification passed all 84 selected cases, with
zero failures or skips, against the reviewed revision. The filter included the
whole `PublicRevealIsolationTests`, `InvocationTransientRevealTests`,
`EventResultMeaningTests`, `EventCueCopyTests`, `OccurrenceFaceTests`, and
`EventPresentation*` classes. Existing controls verify current-text fallback,
boost wording, face-down wording, and concise concealed-history reveal wording.
All shell execution used PTY with `login:false`; the test run left the worktree
clean. Logs and TRX remain outside the repository:

- `C:/Users/mggar/Source/evidence/transient-captured-text-closure.log`
- `C:/Users/mggar/Source/evidence/transient-captured-text-closure/transient-captured-text.trx`

No new Server boundary changed in the narrow correction, so this closure did not
repeat the lead's already recorded 89 Server cases or the full View suite.
Native evidence refresh, owner acceptance, complete-game evidence, pack-wide
acceptance and cross-platform certification remain outside this closure.
