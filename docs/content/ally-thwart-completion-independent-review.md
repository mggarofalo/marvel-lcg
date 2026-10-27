# Independent ally power completion review (MARVEL-872)

Reviewed source: `7ba814c24489cf87ea17f2f86452735368f56e0a` against
`cd18a9b0f23cdd7541cc7b8a35a99730490b63b2`.

## Scope and authority

This is an independent engine review of the exact prerequisite diff. It covers
the general thwart-completion condition, response-boundary scheduling,
consequential damage and performer incarnation. It does not author Power Gloves
or admit an expansion product.

The vendored RR 1.8 `rr:thwart.2` treats resolving a thwart ability as thwarting.
`rr:consequential-damage` and `rr:ally.3` require consequential damage after ally
attacks and thwarts; their wording does not restrict this requirement to basic
powers. `rr:consequential-damage.1` puts triggered abilities first.
`rr:consequential-damage.2` and `.2.1` distinguish an aborted basic power from a
completed one, while `rr:ally.3` exempts confused/stunned attempts.
`rr:leaves-play.1` makes a returned card a new copy.

The general completion condition is additive: the basic-only condition remains
available for basic-power responses. The retained occurrence supplies the exact
ally performer, its incarnation and controller; attachment ownership does not
replace these facts. The consequential step and outer continuation are retained
behind the exact child response window. The continuation permit depends on both
the response owner and consequential prerequisite. The prerequisite's exclusion
from ordinary owned-work repositioning preserves this order.

## Independent adversarial evidence

`AllyPowerIndependentReviewTests` adds seven synthetic cases:

- Labeled ally attack and thwart, each accepting and declining a response:
  the ally remains alive during the response; lethal consequential damage and
  attachment cleanup finish before the following outer choice is offered.
- An attached-upgrade response containing a second labeled thwart: the host
  performs both, the second response window is offered and declined, both
  consequential payments finish, and the outer choice resumes.
- Labeled attack and thwart whose performer leaves and reenters during the
  response window: consequential damage does not follow the new copy, and the
  already initiated outer choice resumes.

The response bodies in the independent fixture are deliberately synthetic and
do not represent printed Power Gloves. A separate exact-card fixture in the
source covers its actor and owner/controller binding, unavailable/blanked
sources, response target choice, confused cancellation, target departure and
initiated response survival through host departure/reentry. Existing basic-use
regressions remain part of the requested focused verification.

The first compiled run exposed three incorrect test assumptions: the attached
upgrade's nested thwart belongs to its host and has its own response window;
direct movement before the scheduler creates an occurrence is not a legal
in-window departure. Those probes were corrected as described above. The first
four lethal-ordering cases passed unchanged. This initial log is retained as
`C:/Users/mggar/Source/ally-thwart-independent-focused.log`.

The final nonincremental Release build completed with zero warnings/errors. The
focused run passed all 72 cases: the independent class, source completion class,
basic-power response, card-power continuation, ally-controller power,
attack-trigger binding, both basic-use-ally classes and rule citations. Logs:
`C:/Users/mggar/Source/ally-thwart-independent-final-build.log` and
`C:/Users/mggar/Source/ally-thwart-independent-final-focused.log`.

An independent compiled mutant omitted the labeled performer's incarnation from
its consequential operation. Both new labeled attack/thwart reentry cases failed
with one damage incorrectly placed on the returned copy. The source was restored
in `finally`; a fresh nonincremental build again had zero warnings/errors and the
same 72-case filter passed. Logs use the same external prefix with suffixes
`mutant-build.log`, `mutant.log`, `restored-build.log` and `restored-focused.log`.

Verdict: accepted for the reviewed engine prerequisite, with no blocking source
finding. This accepts the exact mechanism diff and its focused regression
evidence; the integration limits below remain open. Normal pre-commit hooks are
required for the independent review commit.

## Integration limits

Replay-boundary MARVEL-870 must cover the changed labeled ally attack behavior
as well as labeled thwart completion/response decisions and basic ally
departure/reentry. Digest and protocol shape stability alone do not make old
decision traces compatible with these changed outcomes.

Complete production Power Gloves authoring, attachment restrictions, public
gameplay, full managed/native certification and product acceptance remain
outside this mechanism review. No production code or ability-book row is changed
by the independent review.
