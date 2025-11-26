# autodocx/config_loader.py
from __future__ import annotations
from pathlib import Path
from typing import Any, Dict
import os
import yaml


class ConfigError(RuntimeError):
    pass


def load_config(path: str | None = None) -> Dict[str, Any]:
    """
    Single source of truth: read config YAML only.
    No code defaults. If keys are missing, validation will raise.
    """
    cfg_path = Path(os.getenv("AUTODOCX_CONFIG") or (path or "autodocx.yaml")).resolve()
    if not cfg_path.exists():
        raise ConfigError(f"Config file not found: {cfg_path}. Create autodocx.yaml at project root.")
    try:
        with cfg_path.open("r", encoding="utf-8") as fh:
            cfg = yaml.safe_load(fh) or {}
    except Exception as e:
        raise ConfigError(f"Failed to parse YAML config: {cfg_path}: {e}") from e
    _validate_config(cfg, cfg_path)
    return cfg


def _validate_config(cfg: Dict[str, Any], path: Path) -> None:
    """
    Minimal, strict validation for keys we use. If missing, error early.
    """
    # LLM block
    llm = cfg.get("llm")
    if not isinstance(llm, dict):
        raise ConfigError(f"{path}: 'llm' section is required and must be a mapping.")
    required_llm = ["provider", "model", "max_output_tokens"]
    missing_llm = [k for k in required_llm if k not in llm]
    if missing_llm:
        raise ConfigError(f"{path}: llm missing required keys: {missing_llm}")

    # Structured outputs block
    so = llm.get("structured_outputs")
    if so is None or not isinstance(so, dict):
        raise ConfigError(f"{path}: llm.structured_outputs is required (mapping) "
                          f"with keys: enabled (bool), schema_name (str), strict (bool).")
    for key in ["enabled", "schema_name", "strict"]:
        if key not in so:
            raise ConfigError(f"{path}: llm.structured_outputs missing required key: {key}")

    # Out dir
    if "out_dir" not in cfg:
        raise ConfigError(f"{path}: 'out_dir' is required.")

    # Rollup thresholds
    rollup = cfg.get("rollup")
    if not isinstance(rollup, dict):
        raise ConfigError(f"{path}: 'rollup' section is required and must be a mapping.")
    for key in ["publish_threshold", "hypothesis_threshold"]:
        if key not in rollup:
            raise ConfigError(f"{path}: rollup missing required key: {key}")

    # Optional: temperature. If present must be number or null.
    if "temperature" in llm and llm["temperature"] is not None:
        if not isinstance(llm["temperature"], (int, float)):
            raise ConfigError(f"{path}: llm.temperature must be a number or null.")


def get_llm_settings() -> Dict[str, Any]:
    """
    Returns all LLM settings; no defaults here.
    """
    cfg = load_config()
    return cfg["llm"]


def get_all_settings() -> Dict[str, Any]:
    """
    Returns full config; use this for non-LLM settings (e.g., rollup, out_dir).
    """
    return load_config()
