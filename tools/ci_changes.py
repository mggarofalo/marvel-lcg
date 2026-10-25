"""Skip routine CI only for a verified, nonempty Markdown-only Git diff."""

import json
import os
import re
import subprocess


def requires_ci(event_name, event, exhaustive=False, repository="."):
    if exhaustive or event_name not in ("push", "pull_request"):
        return True
    if event_name == "pull_request":
        pull = event.get("pull_request", {})
        base = pull.get("base", {}).get("sha", "")
        head = pull.get("head", {}).get("sha", "")
        separator = "..."
    else:
        base, head = event.get("before", ""), event.get("after", "")
        separator = ".."
    if not all(re.fullmatch(r"[0-9a-fA-F]{40}", sha) and int(sha, 16)
               for sha in (base, head)):
        return True
    result = subprocess.run(
        ["git", "diff", "--name-only", "--no-renames", "-z", base + separator + head],
        cwd=repository, capture_output=True, check=False)
    if result.returncode:
        return True
    paths = result.stdout.split(b"\0")[:-1]
    # Treat renames as delete/add so moving code to .md cannot hide code removal.
    return not paths or any(not path.lower().endswith(b".md") for path in paths)


if __name__ == "__main__":
    with open(os.environ["GITHUB_EVENT_PATH"], encoding="utf-8") as source:
        event = json.load(source)
    run = requires_ci(os.environ["GITHUB_EVENT_NAME"], event,
                      os.environ.get("CI_EXHAUSTIVE") == "true")
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write(f"run={'true' if run else 'false'}\n")
    print("Full CI required" if run else "Markdown-only diff: routine CI jobs skipped")
