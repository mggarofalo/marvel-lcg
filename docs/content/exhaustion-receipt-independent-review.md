# Independent exhaustion receipt review

The delivery lead reviewed worker implementation 2431592a without changing its
production code. Accept the bounded committed-exhaustion receipt. It records
only ready-to-exhausted transitions, overwrites the result even with zero, and
keeps effect exhaustion distinct from payment receipts. Closed expression
traversal finds arithmetic/conditional uses; unsupported area and labelled-power
projection is explicitly refused rather than reading an old live receipt.
No actual Harvest row or public admission is included.

Added an independent paid action: Aunt May exhausts for its cost, then global
Persona exhaustion commits only the other two supports, healing 2. A later
resolution refreshes only the cost source and heals 0. The invocation counter
proves both executions completed. 18 focused receipt/projection/cost cases pass.
A compiled mutant counting already-exhausted selected cards fails the new test
at expected remaining damage 3/actual 2. Exact source restoration and forced
nonincremental build precede the final passing run.

Logs: C:/Users/mggar/Source/harvest-lead-review.log,
harvest-lead-mutant{,-build}.log and harvest-lead-restored-build.log.
Initial review fixture used a nonexistent ReadyCard method (corrected to
Refresh), and incorrectly expected automatic exhaust-this cost in the separate
exhaustChosen receipt. The final assertion tests actual committed effect count;
no production behavior was changed to accommodate either fixture mistake.
