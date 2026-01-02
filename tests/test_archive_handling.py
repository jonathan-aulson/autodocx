from __future__ import annotations

from pathlib import Path

import pytest

from autodocx_cli.__main__ import run_scan


def test_run_scan_fails_when_archive_extraction_emits_warnings(monkeypatch, tmp_path):
    repo = tmp_path / "repo"
    repo.mkdir()
    out_dir = tmp_path / "out"
    out_dir.mkdir()

    def fake_collect_scan_roots(repo_path: Path, out_path: Path, include_archives: bool):
        assert include_archives is True
        return [repo_path], {"archives": [], "warnings": ["Failed to extract archive.zip: boom"]}

    monkeypatch.setattr("autodocx_cli.__main__.collect_scan_roots", fake_collect_scan_roots)

    with pytest.raises(RuntimeError) as excinfo:
        run_scan(repo, out_dir, include_archives=True)

    assert "Archive extraction failed" in str(excinfo.value)
