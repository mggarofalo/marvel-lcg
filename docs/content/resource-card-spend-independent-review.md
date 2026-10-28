# Resource-card spend independent review

Reviewed `8c6c04df` against `4cf4f030`. The synthetic review cases exercise a
response whose payment opens a nested response before the outer effect and card
entry, two simultaneous contributors when the initiating player is not first,
and a spent physical card entering and leaving play before its original window
closes. Together with the source cases, all 15 focused tests passed after a
non-incremental restored build. Removing both incarnation checks compiled and
caused the reentry case to fail by offering the new copy; source was restored.

This evidence covers the Pym-style response on the spent card. It does not
establish support for an in-play ability observing another card's spend: the
`you` player matcher currently requires the response source itself to appear in
the physical-spend receipt. The review does not admit a Pym ability row.

The inherited Power regression and architecture failure recorded in
`resource-card-spend-response.md` remain separate from this review. This review
did not rerun full Content or integrate the separate Power repair.
