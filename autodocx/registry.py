# autodocx/registry.py
from __future__ import annotations
from typing import List, Iterable, Callable
import os
import inspect
import importlib
import pkgutil

_EXTRACTORS_PKG = "autodocx.extractors"

def _debug(msg: str) -> None:
    if os.getenv("AUTODOCX_DEBUG_EXTRACTORS", "0") == "1":
        print(f"[extractors] {msg}")

def _iter_builtin_modules() -> Iterable[str]:
    """
    Yield module names inside autodocx.extractors (excluding __init__ and base).
    """
    try:
        pkg = importlib.import_module(_EXTRACTORS_PKG)
        pkg_path = pkg.__path__  # type: ignore[attr-defined]
    except Exception as e:
        _debug(f"failed to import {_EXTRACTORS_PKG}: {e}")
        return []
    for m in pkgutil.iter_modules(pkg_path):
        name = m.name
        if name in {"__init__", "base"} or name.startswith("_"):
            continue
        yield f"{_EXTRACTORS_PKG}.{name}"

def _looks_like_extractor(cls: type) -> bool:
    """
    Duck-typing check: has name, patterns, and callables detect/discover/extract.
    """
    try:
        has_name = isinstance(getattr(cls, "name", None), str)
        has_patterns = isinstance(getattr(cls, "patterns", None), list)
        has_detect = callable(getattr(cls, "detect", None))
        has_discover = callable(getattr(cls, "discover", None))
        has_extract = callable(getattr(cls, "extract", None))
        return has_name and has_patterns and has_detect and has_discover and has_extract
    except Exception:
        return False

def _iter_builtin_extractors() -> Iterable[object]:
    """
    Import each module and yield classes that duck-type as extractors.
    """
    for modname in _iter_builtin_modules():
        try:
            module = importlib.import_module(modname)
        except Exception as e:
            _debug(f"failed to import module {modname}: {e}")
            continue
        for _, obj in inspect.getmembers(module, inspect.isclass):
            if _looks_like_extractor(obj):
                try:
                    inst = obj()  # zero-arg constructor
                    yield inst
                except Exception as e:
                    _debug(f"failed to instantiate {obj} from {modname}: {e}")

def _iter_entry_point_extractors() -> Iterable[object]:
    """
    Yield extractor instances from entry points named 'autodocx.extractors'.
    Supports importlib.metadata and pkg_resources.
    """
    # importlib.metadata path
    try:
        from importlib.metadata import entry_points
        eps = entry_points()
        if hasattr(eps, "select"):
            group = eps.select(group="autodocx.extractors")
        elif isinstance(eps, dict):
            group = eps.get("autodocx.extractors", [])
        else:
            group = []
        for ep in group:
            try:
                cls = ep.load()
                inst = cls()
                yield inst
            except Exception as e:
                print(f"[warn] Failed to load extractor {getattr(ep, 'name', '?')}: {e}")
    except Exception as e:
        _debug(f"importlib.metadata entry_points failed: {e}")

    # pkg_resources path
    try:
        pkg_resources = importlib.import_module("pkg_resources")
    except ModuleNotFoundError:
        _debug("pkg_resources not installed; skipping entry point load")
    except Exception as e:
        _debug(f"pkg_resources import failed: {e}")
    else:
        try:
            for ep in pkg_resources.iter_entry_points("autodocx.extractors"):
                try:
                    cls = ep.load()
                    inst = cls()
                    yield inst
                except Exception as e:
                    print(f"[warn] Failed to load extractor via pkg_resources {getattr(ep, 'name', '?')}: {e}")
        except Exception as e:
            _debug(f"pkg_resources entry point load failed: {e}")

def _unique_by_class(instances: Iterable[object]) -> List[object]:
    """
    De-duplicate by fully-qualified class name.
    """
    seen = set()
    out: List[object] = []
    for inst in instances:
        try:
            fqcn = f"{inst.__class__.__module__}.{inst.__class__.__name__}"
        except Exception:
            fqcn = repr(inst.__class__)
        if fqcn not in seen:
            seen.add(fqcn)
            out.append(inst)
    return out

def _parse_list_env(varname: str) -> List[str]:
    v = os.getenv(varname, "")
    if not v:
        return []
    return [x.strip() for x in v.split(",") if x.strip()]

def _filter_instances(instances: List[object]) -> List[object]:
    """
    Optional filtering via env:
      - AUTODOCX_EXTRACTORS_INCLUDE: comma-separated class names OR module.Class
      - AUTODOCX_EXTRACTORS_EXCLUDE: comma-separated class names OR module.Class
    """
    include = set(_parse_list_env("AUTODOCX_EXTRACTORS_INCLUDE"))
    exclude = set(_parse_list_env("AUTODOCX_EXTRACTORS_EXCLUDE"))
    disable_repo_inventory = os.getenv("AUTODOCX_ENABLE_REPO_INVENTORY", "0").lower() not in {"1", "true", "yes"}

    def keys(inst: object) -> List[str]:
        mod = inst.__class__.__module__
        cls = inst.__class__.__name__
        return [cls, f"{mod}.{cls}"]

    if include:
        instances = [inst for inst in instances if any(k in include for k in keys(inst))]
    if exclude:
        instances = [inst for inst in instances if not any(k in exclude for k in keys(inst))]
    if disable_repo_inventory and not include:
        instances = [inst for inst in instances if inst.__class__.__name__ != "RepoInventoryExtractor"]
    return instances

def load_extractors() -> List[object]:
    """
    Load extractors in this order:
      1) Auto-discovered built-ins (modules under autodocx.extractors)
      2) Entry-point plugins (optional)
    Then de-duplicate and apply optional include/exclude filters.
    """
    instances: List[object] = []
    builtin = list(_iter_builtin_extractors())
    _debug(f"builtin discovered: {[f'{e.__class__.__module__}.{e.__class__.__name__}' for e in builtin]}")
    instances.extend(builtin)

    plugins = list(_iter_entry_point_extractors())
    _debug(f"plugins discovered: {[f'{e.__class__.__module__}.{e.__class__.__name__}' for e in plugins]}")
    instances.extend(plugins)

    instances = _unique_by_class(instances)
    instances = _filter_instances(instances)

    _debug(f"loaded extractors: {[f'{e.__class__.__module__}.{e.__class__.__name__}' for e in instances]}")
    return instances
