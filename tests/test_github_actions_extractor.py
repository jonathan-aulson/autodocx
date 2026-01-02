from pathlib import Path

from autodocx.extractors.github_actions import GitHubActionsExtractor


WORKFLOW_YAML = """name: CI
on:
  push:
    branches:
      - main
jobs:
  build:
    runs-on: ubuntu-latest
    environment: prod
    steps:
      - name: Checkout
        uses: actions/checkout@v3
      - name: Upload reports
        uses: actions/upload-artifact@v4
        with:
          name: reports
          path: coverage
"""


def test_github_actions_extractor_emits_steps(tmp_path: Path) -> None:
    workflow = tmp_path / ".github" / "workflows" / "ci.yml"
    workflow.parent.mkdir(parents=True, exist_ok=True)
    workflow.write_text(WORKFLOW_YAML, encoding="utf-8")

    extractor = GitHubActionsExtractor()
    signals = list(extractor.extract(workflow))
    assert signals, "Expected workflow signal"
    props = signals[0].props
    steps = props.get("steps") or []
    assert any(step.get("connector") == "actions/checkout@v3" for step in steps)
    assert "prod" in (props.get("service_dependencies") or [])
    assert "reports" in (props.get("datasource_tables") or [])
