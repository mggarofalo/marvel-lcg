"""Packaging chores for the Python engine (MARVEL-55).

Two commands that build a release artefact: bump the build number, and zip the
card scripts. Both **mutate the working tree**, and `bump` commits, so they are
run deliberately and never by anything automated.

They used to live in `unit_test/test_task.py` as `test_IncreaseVersion` and
`test_zip_cards` -- the upstream author's chores, named `test_*` so `unittest`
would run them on demand. That made every run of the suite rewrite `build.py`
and commit it, which counted test runs instead of packages and left stray
commits on whatever branch happened to be checked out. Parallel worktree agents
made it worse: two agents running the suite edit the same `BUILD` line and
collide at merge time.

Nothing under `unit_test/` may import this package. `test_package_tools.py`
asserts that, which is what keeps the chores from drifting back into the suite.

Run from `py_src/`:

    python -m tools.package.bump
    python -m tools.package.zip_cards
"""
