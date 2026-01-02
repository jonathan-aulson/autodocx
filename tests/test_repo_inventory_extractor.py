import json
from pathlib import Path

from autodocx.extractors.repo_inventory import RepoInventoryExtractor


def test_repo_inventory_emits_signals(tmp_path: Path) -> None:
    repo = tmp_path / "repo"
    (repo / ".git").mkdir(parents=True)
    src = repo / "orders" / "src"
    tests_dir = repo / "orders" / "tests"
    infra = repo / "orders" / "infra"
    src.mkdir(parents=True)
    tests_dir.mkdir(parents=True)
    infra.mkdir(parents=True)
    (src / "app.py").write_text("print('hello')\n", encoding="utf-8")
    (tests_dir / "test_app.py").write_text("def test_it():\n    assert True\n", encoding="utf-8")
    (infra / "main.bicep").write_text("resource storage '...' = {}\n", encoding="utf-8")
    zone_file = src / "app.py:Zone.Identifier"
    zone_file.write_text("ZoneId=3\n", encoding="utf-8")
    whitesource = repo / ".whitesource"
    whitesource.write_text("{}", encoding="utf-8")

    extractor = RepoInventoryExtractor()
    assert extractor.detect(repo)
    candidates = list(extractor.discover(repo))
    assert (src / "app.py") in candidates
    assert zone_file not in candidates
    assert whitesource not in candidates

    signal = extractor.extract(src / "app.py")[0]
    assert signal.kind == "repo_artifact"
    assert signal.props["artifact_type"] == "code"
    assert signal.props["component_hint"] == "orders"

    test_signal = extractor.extract(tests_dir / "test_app.py")[0]
    assert test_signal.props["artifact_type"] == "test"

    infra_signal = extractor.extract(infra / "main.bicep")[0]
    assert infra_signal.props["artifact_type"] == "infra"
