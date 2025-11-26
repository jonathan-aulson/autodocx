from __future__ import annotations

from typing import Any, Dict, Optional

from autodocx.types import Signal


def build_experience_pack(signal: Signal) -> Optional[Dict[str, Any]]:
    """
    Derive a lightweight "experience pack" object from a single signal.
    In future iterations this can aggregate multiple related signals, but
    for now we expose a consistent structure so renderers/LLM prompts can rely on it.
    """
    props: Dict[str, Any] = signal.props if isinstance(signal.props, dict) else {}
    component = props.get("component_or_service") or props.get("service") or props.get("name")
    if not component:
        return None

    summary = props.get("user_story") or props.get("description") or ""
    screenshots = props.get("screenshots") or ([] if not props.get("ui_snapshot") else [props.get("ui_snapshot")])
    if not summary and not screenshots and not props.get("journey_touchpoints"):
        # Nothing narrative to expose yet; skip pack generation
        return None

    pack_id = f"{component}:{signal.kind}"
    return {
        "id": pack_id,
        "component": component,
        "kind": signal.kind,
        "summary": summary,
        "touchpoints": props.get("journey_touchpoints") or props.get("route_hierarchy") or [],
        "inputs_example": props.get("inputs_example") or {},
        "outputs_example": props.get("outputs_example") or {},
        "screenshots": screenshots,
    }
