import os

from autodocx.registry import load_extractors


def _names():
    return {e.__class__.__name__ for e in load_extractors()}


def test_repo_inventory_disabled_by_default(monkeypatch):
    monkeypatch.delenv("AUTODOCX_ENABLE_REPO_INVENTORY", raising=False)
    monkeypatch.delenv("AUTODOCX_EXTRACTORS_INCLUDE", raising=False)
    monkeypatch.delenv("AUTODOCX_EXTRACTORS_EXCLUDE", raising=False)
    names = _names()
    assert "RepoInventoryExtractor" not in names


def test_repo_inventory_can_be_enabled(monkeypatch):
    monkeypatch.setenv("AUTODOCX_ENABLE_REPO_INVENTORY", "1")
    monkeypatch.delenv("AUTODOCX_EXTRACTORS_INCLUDE", raising=False)
    monkeypatch.delenv("AUTODOCX_EXTRACTORS_EXCLUDE", raising=False)
    names = _names()
    assert "RepoInventoryExtractor" in names
