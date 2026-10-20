# Scheme-defeat regression journals

`scheme-defeat-10.jsonl.gz` and `scheme-defeat-13.jsonl.gz` record complete legal
Crossbones/Spider-Man games from implementation checkpoint
`93ad1744` for MARVEL-637. Their game seeds are 10 and 13; research-policy seeds
are 276 and 279. Surveillance Team defeats Crossbones' Assault at decision 49
in the first game; an ally basic thwart defeats it at decision 88 in the second.
The games contain 90 and 89 decisions respectively.

The `campaign-research` v1 policy selected the decisions. The acceptance test
replays those recorded decisions through the production engine and journal
comparisons; it does not regenerate or certify that research policy. The JSON
records are retained verbatim from the run logs, with LF separators and
deterministic gzip compression. They are regression evidence, not rules
authorities. Authority-derived focused tests in `SchemeDefeatResponsibilityTests`
and `AllyControllerPowerTests` establish the intended attribution.

Do not replace recorded outcomes to accommodate a changed engine. Investigate
divergence, preserve the original evidence, and make any compatibility decision
explicit. Running tests reads these files without modifying them.
