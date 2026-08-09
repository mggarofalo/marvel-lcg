"""Tooling for card coverage -- what a replay corpus actually exercised.

The bot runner writes one report per run, automatically. This package is for the
question a single run cannot answer: what does the corpus reach *in total*, once
there are dozens of runs across different scenarios, heroes, player counts and
difficulties. `report.py` merges run artefacts and re-ranks against the card
dataset.

The measurement itself lives in `engine/profile/card_coverage.py`, and the
ranking in `engine/profile/coverage_report.py`. Same split as the state digest:
the contract is engine code, the command line is a thin shell over it.
"""
