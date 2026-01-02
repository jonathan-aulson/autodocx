# autodocx/config_loader.py
from __future__ import annotations
from pathlib import Path
from typing import Any, Dict
import os
import yaml


class ConfigError(RuntimeError):
    pass

DEFAULT_PIPELINE_SWITCHES = {
    "build_candidates": True,
    "qc_candidates": True,
    "enrich_index": True,
    "push_qdrant": False,
    "auto_anchor_fallback": True,
    "guard_append_only": True,
    "require_doc_hints": True,
    "include_archives": True,
    "doc_plan_refresh": True,
    "doc_plan_fulfill": True,
    "rag_docs": False,
    "llm_rollup": False,
}


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
    _apply_env_overrides(cfg)
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


def _env_bool(name: str) -> bool | None:
    val = os.getenv(name)
    if val is None:
        return None
    return str(val).strip().lower() in {"1", "true", "yes", "on"}


def _apply_env_overrides(cfg: Dict[str, Any]) -> None:
    llm = cfg.get("llm") or {}
    provider = os.getenv("AUTODOCX_LLM_PROVIDER")
    if provider:
        llm["provider"] = provider.strip()
    model = os.getenv("AUTODOCX_LLM_MODEL")
    if model:
        llm["model"] = model.strip()
    max_tokens = os.getenv("AUTODOCX_LLM_MAX_OUTPUT_TOKENS")
    if max_tokens:
        try:
            llm["max_output_tokens"] = int(max_tokens)
        except ValueError:
            pass
    temperature = os.getenv("AUTODOCX_LLM_TEMPERATURE")
    if temperature is not None:
        try:
            llm["temperature"] = float(temperature)
        except ValueError:
            pass
    cfg["llm"] = llm

    out_dir_env = os.getenv("AUTODOCX_OUT_DIR")
    if out_dir_env:
        cfg["out_dir"] = out_dir_env.strip()

    pipeline_cfg = dict(DEFAULT_PIPELINE_SWITCHES)
    pipeline_cfg.update(cfg.get("pipeline") or {})
    env_map = {
        "build_candidates": "AUTODOCX_PIPELINE_BUILD_CANDIDATES",
        "qc_candidates": "AUTODOCX_PIPELINE_QC_CANDIDATES",
        "enrich_index": "AUTODOCX_PIPELINE_ENRICH_INDEX",
        "push_qdrant": "AUTODOCX_PIPELINE_PUSH_QDRANT",
        "auto_anchor_fallback": "AUTODOCX_PIPELINE_AUTO_ANCHOR_FALLBACK",
        "guard_append_only": "AUTODOCX_PIPELINE_GUARD_APPEND_ONLY",
        "require_doc_hints": "AUTODOCX_PIPELINE_REQUIRE_DOC_HINTS",
        "include_archives": "AUTODOCX_PIPELINE_INCLUDE_ARCHIVES",
        "doc_plan_refresh": "AUTODOCX_PIPELINE_DOC_PLAN_REFRESH",
        "doc_plan_fulfill": "AUTODOCX_PIPELINE_DOC_PLAN_FULFILL",
        "rag_docs": "AUTODOCX_PIPELINE_RAG_DOCS",
        "llm_rollup": "AUTODOCX_PIPELINE_LLM_ROLLUP",
    }
    for key, env_name in env_map.items():
        env_val = _env_bool(env_name)
        if env_val is not None:
            pipeline_cfg[key] = env_val
    cfg["pipeline"] = pipeline_cfg


def get_llm_settings() -> Dict[str, Any]:
    """
    Returns all LLM settings; no defaults here.
    """
    cfg = load_config()
    settings = dict(cfg["llm"])
    model_override = os.getenv("AUTODOCX_LLM_MODEL")
    if model_override:
        settings["model"] = model_override.strip()
    return settings


def get_all_settings() -> Dict[str, Any]:
    """
    Returns full config; use this for non-LLM settings (e.g., rollup, out_dir).
    """
    return load_config()
