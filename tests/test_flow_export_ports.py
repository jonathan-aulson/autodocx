from autodocx.types import Signal
from autodocx.visuals.flow_export import _build_workflow_graph


def test_flow_export_includes_port_metadata() -> None:
    props = {
        "name": "PortFlow",
        "component_or_service": "billing",
        "triggers": [{"name": "Manual", "type": "manual"}],
        "steps": [
            {"name": "BranchScope", "control_type": "if"},
            {"name": "SuccessAction", "run_after": ["BranchScope"]},
        ],
        "control_edges": [
            {"parent": "BranchScope", "branch": "Parse JSON - Success", "children": ["SuccessAction"]},
        ],
    }
    signal = Signal(kind="workflow", props=props, evidence=[], subscores={})
    graph = _build_workflow_graph(signal, props, "billing")

    branch_node = next(node for node in graph["nodes"] if node["name"] == "BranchScope")
    assert any(port["name"] == "branch_parse_json_success" for port in branch_node["ports"])

    branch_edge = next(edge for edge in graph["edges"] if edge["kind"] == "branch")
    assert branch_edge["source_port"] == "branch_parse_json_success"
    assert branch_edge["target_port"] == "in_main"

    seq_edge = next(edge for edge in graph["edges"] if edge["kind"] == "sequence")
    assert seq_edge["source_port"] == "out_default"
    assert seq_edge["target_port"] == "in_main"


def test_flow_export_reuses_triggers_for_relationship_edges() -> None:
    props = {
        "name": "TriggerFlow",
        "component_or_service": "billing",
        "triggers": [{"name": "manual", "type": "manual"}],
        "steps": [{"name": "StepA", "run_after": []}],
        "relationships": [
            {
                "source": {"type": "trigger", "name": "manual"},
                "target": {"kind": "http", "display": "https://example", "ref": "https://example"},
                "operation": {"type": "calls", "detail": "Calls endpoint"},
            }
        ],
    }
    signal = Signal(kind="workflow", props=props, evidence=[], subscores={})
    graph = _build_workflow_graph(signal, props, "billing")

    manual_nodes = [n for n in graph["nodes"] if n["name"] == "manual"]
    assert len(manual_nodes) == 1, "trigger node should not be duplicated as a step"
    assert any(edge for edge in graph["edges"] if edge["source"] == manual_nodes[0]["id"])


def test_flow_export_uses_transitions_when_run_after_missing() -> None:
    props = {
        "name": "TransitionFlow",
        "component_or_service": "bw",
        "triggers": [],
        "steps": [
            {"name": "A"},
            {"name": "B"},
        ],
        "transitions": [{"from": "A", "to": "B"}],
    }
    signal = Signal(kind="workflow", props=props, evidence=[], subscores={})
    graph = _build_workflow_graph(signal, props, "bw")
    assert any(edge for edge in graph["edges"] if edge["source"].endswith("a") and edge["target"].endswith("b"))
