# autodocx/llm/provider.py
from __future__ import annotations
import os
import json
from typing import Optional, Dict, Any

from autodocx.config_loader import get_llm_settings

# Try to load .env so OPENAI_API_KEY is available
try:
    from dotenv import load_dotenv, find_dotenv
    load_dotenv(find_dotenv(usecwd=True), override=False)
except Exception:
    pass



def _as_full_input(prompt: str, input_json: Optional[Dict[str, Any]]) -> str:
    if input_json is not None:
        return f"{prompt}\n\n{json.dumps(input_json, ensure_ascii=False)}"
    return prompt


def _extract_responses_text(resp: Any) -> str:
    """
    Extract plain text from a Responses API result across SDK variants.
    """
    # Preferred (SDK convenience on newer SDKs)
    text = getattr(resp, "output_text", None)
    if text:
        return text

    # Fallback: iterate the structured 'output'
    try:
        chunks = []
        for item in getattr(resp, "output", []) or []:
            for c in getattr(item, "content", []) or []:
                t = getattr(c, "text", None)
                if t:
                    chunks.append(t)
        if chunks:
            return "".join(chunks)
    except Exception:
        pass

    # Last resort: string-ify whole object
    return str(resp)


def call_openai_meta(
    prompt: str,
    input_json: Optional[Dict[str, Any]] = None,
    json_schema: Optional[Dict[str, Any]] = None,
) -> Dict[str, Any]:
    """
    Single-entry LLM call that returns text plus metadata.

    Returns:
      {
        "text": str,
        "usage": {"input_tokens": int|None, "output_tokens": int|None, "total_tokens": int|None} | {},
        "response_id": str | None,
        "latency_ms": int
      }
    All runtime settings (model, tokens, temperature, structured outputs) are read from autodocx.yaml via get_llm_settings().
    """
    llm = get_llm_settings()
    model = llm["model"]
    max_output_tokens = llm["max_output_tokens"]
    temperature = llm.get("temperature", None)  # include only if present in YAML
    so = llm.get("structured_outputs") or {}
    so_enabled = bool(so.get("enabled"))
    so_name = so.get("schema_name")
    so_strict = bool(so.get("strict"))

    print("[provider] Responses-API provider — model:", model)

    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        raise RuntimeError("OPENAI_API_KEY not set")

    full_input = _as_full_input(prompt, input_json)

    try:
        from openai import OpenAI  # >= 1.0 SDK
    except Exception as e:
        raise RuntimeError("openai>=1.0 is required. Install with: pip install --upgrade openai") from e

    client = OpenAI(api_key=api_key)

    # Build request payload
    payload: Dict[str, Any] = {
        "model": model,
        "input": full_input,
    }
    # Only include temperature if present and numeric (prevents 'temperature not supported' errors)
    if isinstance(temperature, (int, float)):
        payload["temperature"] = float(temperature)

    # Structured outputs: place schema under text.format.schema with name and strict
    if so_enabled and json_schema:
        payload["text"] = {
            "format": {
                "type": "json_schema",
                "name": so_name,       # required at text.format.name
                "schema": json_schema, # required at text.format.schema
                "strict": so_strict
            }
        }

    # Call Responses API and capture basic telemetry
    import time as _t
    t0 = _t.time()
    resp = client.responses.create(
        **payload,
        max_output_tokens=max_output_tokens,
    )
    dt_ms = int((_t.time() - t0) * 1000)

    text = _extract_responses_text(resp)
    usage = {}
    rid = None
    try:
        rid = getattr(resp, "id", None)
        u = getattr(resp, "usage", None) or {}
        # SDKs may expose usage as attributes or dict; normalize
        input_tokens = getattr(u, "input_tokens", None) if hasattr(u, "input_tokens") else u.get("input_tokens")
        output_tokens = getattr(u, "output_tokens", None) if hasattr(u, "output_tokens") else u.get("output_tokens")
        total_tokens = getattr(u, "total_tokens", None) if hasattr(u, "total_tokens") else u.get("total_tokens")
        usage = {
            "input_tokens": int(input_tokens) if input_tokens is not None else None,
            "output_tokens": int(output_tokens) if output_tokens is not None else None,
            "total_tokens": int(total_tokens) if total_tokens is not None else None,
        }
    except Exception:
        usage = {}

    return {"text": text, "usage": usage, "response_id": rid, "latency_ms": dt_ms}


def call_openai(
    prompt: str,
    input_json: Optional[Dict[str, Any]] = None,
    json_schema: Optional[Dict[str, Any]] = None,
) -> str:
    """
    Thin wrapper returning only the text output. Settings still come from YAML.
    """
    res = call_openai_meta(prompt=prompt, input_json=input_json, json_schema=json_schema)
    return res["text"]
