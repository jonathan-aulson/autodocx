from pathlib import Path

from autodocx.utils.provenance import build_provenance_entries


def test_build_provenance_entries_parses_ranges(tmp_path: Path) -> None:
    repo = tmp_path / "repo"
    repo.mkdir()
    target = repo / "src" / "orders.py"
    target.parent.mkdir(parents=True)
    target.write_text("print('ok')\n", encoding="utf-8")

    entries = build_provenance_entries(repo, ["src/orders.py:10-20"], None)
    assert entries[0]["path"] == "src/orders.py"
    assert entries[0]["start_line"] == 10
    assert entries[0]["end_line"] == 20


def test_build_provenance_entries_fallback(tmp_path: Path) -> None:
    repo = tmp_path / "repo"
    repo.mkdir()
    target = repo / "src" / "orders.py"
    target.parent.mkdir(parents=True)
    target.write_text("print('ok')\n", encoding="utf-8")
    entries = build_provenance_entries(repo, [], "src/orders.py")
    assert entries[0]["path"] == "src/orders.py"
