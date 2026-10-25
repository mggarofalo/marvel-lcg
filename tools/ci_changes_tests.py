"""Behavioral controls run against disposable Git repositories, never the checkout."""

import pathlib
import subprocess
import tempfile
import unittest

from ci_changes import requires_ci


class ChangeSelectionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = pathlib.Path(self.temp.name)
        self.git("init", "-q")
        self.git("config", "user.name", "CI test")
        self.git("config", "user.email", "ci@example.invalid")
        self.write("README.md", "initial")
        self.write("engine.cs", "initial")
        self.base = self.commit()

    def git(self, *args):
        return subprocess.check_output(["git", *args], cwd=self.root).decode().strip()

    def write(self, name, text):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def commit(self):
        self.git("add", "--all")
        self.git("commit", "-qm", "fixture")
        return self.git("rev-parse", "HEAD")

    def selected(self, head, event_name="push", exhaustive=False, base=None):
        base = base or self.base
        event = {"before": base, "after": head, "pull_request": {
            "base": {"sha": base}, "head": {"sha": head}}}
        return requires_ci(event_name, event, exhaustive, self.root)

    def test_markdown_changes_skip_but_explicit_runs_do_not(self):
        self.write("docs/space name.MD", "new")
        (self.root / "README.md").unlink()
        head = self.commit()
        self.assertFalse(self.selected(head))
        self.assertFalse(self.selected(head, "pull_request"))
        self.assertTrue(self.selected(head, "workflow_dispatch"))
        self.assertTrue(self.selected(head, "workflow_call"))
        self.assertTrue(self.selected(head, exhaustive=True))

    def test_mixed_changes_and_code_renamed_to_markdown_run(self):
        self.write("README.md", "changed")
        (self.root / "engine.cs").rename(self.root / "engine.md")
        head = self.commit()
        self.assertTrue(self.selected(head))
        self.assertTrue(self.selected(head, "pull_request"))

    def test_non_markdown_change_after_many_docs_still_runs(self):
        for index in range(310):
            self.write(f"docs/{index}.md", "new")
        self.write("workflow.yml", "changed workflow")
        self.assertTrue(self.selected(self.commit()))

    def test_pr_uses_merge_base_and_push_uses_entire_range(self):
        self.git("checkout", "-qb", "feature")
        self.write("docs/new.md", "docs")
        head = self.commit()
        self.git("checkout", "-q", self.base)
        self.write("engine.cs", "base advanced")
        advanced = self.commit()
        self.assertFalse(self.selected(head, "pull_request", base=advanced))
        self.assertTrue(self.selected(head, base=advanced))
        self.write("README.md", "later docs")
        self.assertTrue(self.selected(self.commit()))

    def test_unknown_empty_or_unavailable_comparison_runs(self):
        self.assertTrue(self.selected(self.base))
        self.assertTrue(self.selected(self.base, base="0" * 40))
        self.assertTrue(self.selected(self.base, base="f" * 40))
        self.assertTrue(requires_ci("push", {}, repository=self.root))


if __name__ == "__main__":
    unittest.main()
