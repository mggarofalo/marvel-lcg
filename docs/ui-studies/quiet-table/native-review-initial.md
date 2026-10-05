# Initial independent native review

Reviewed 2026-09-29, running Debug Godot engine-backed solo Spider-Man / Rhino. HEAD 29bbae99b2b5ced2d5f2cf0ebd2b53a6d3359e22 plus in-progress UI working tree changes. This initial running build predates Complete choices and pending title/spacing/208px dock changes; the current working diff is newer than the running build, so this is not final-build certification. Requested viewport1920x1080, scale100%; native window zoom was needed because macOS capture showed stale setup image while real clicks advanced game. Actual capture then1308x768; do not extrapolate supported-profile rendering from this interim review.

Reviewer received game goals, no click sequence; no source inspected before tasks. Goals attempted: choose opening hand, reverse mulligan selection, confirm replacement, change form, stage/commit basic attack, compose/cancel/play support, progress player phase, inspect incoming attack and stage defense. Native CUA UI actions only.

Passing observations: body clicks pin inspect distinctly from direct selectors; mulligan staged1 states selected card and Discard1andredraw; cancel/reopen returns0selected without mutation; hero attack sole target stages without submitting; named AttackRhino commit; accurate RhinoHP14to12; quarter-turn exhausted hero with upright EXHAUSTED caption; payment explicit discard names and canonical icons; selected2reports Paymentready; cancel preserves6hand/board; PayandplaySurveillanceTeam creates support, discard2, hand3; receipt names payment sources; incoming Rhinoattack identifies answeringSpiderMan, knownATK2 beforeunknownboost; causal attackcontext persists through SpiderSense and defense.

Required fixes found:
1 Generic Pass does not explain whether it declines a response, ends actions, or leaves attack undefended. Ordinary Chooseanaction lacks phase/turn context.
2 EndPhase choose0to3 Selecttarget buttons do not explain optional hand discards/draw/ready consequences.
3 Attack and defense stage No paymentrequired without explaining exhaustion; defense does not explain knownDEF3 effect.
4 SpiderSense selected action has no known draw1 effect description.
5 Prior damage receipt vanishes over time/during a payment composition/cancel flow. History must remain optional.
6 Cancelling required mulligan hides current composition behind tiny hero-local Choosecardstodisca button and generic Choosecontinue message.
7 Hand titles and source-action labels are heavily truncated despite broad emptytable regions.
8 Cause and draft columns separated by large blank region; small scrolling body hides relevant costs/context.
9 Payment modal currently dominated by TARGETS1CHOSENREQUIRED1COMPLETE/automaticSpiderMan bookkeeping; visual panel appears flat black/unframed, although payment operability works.

Stopped UI at staged Defense-SpiderMan, Enemyattack Step2of6 Declaredefender; no defense committed. Unreviewed: defense resolution, encounter reveal, nested treachery choice, grouped/repeated/search/variable fallback, keyboard-only flow, drag, multiplayer, dense board, supportedscale150 and final revised build. Verdict: fixes required for comprehension; cannot approve complete redesign from this increment.
