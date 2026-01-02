import pytest

from autodocx.visuals import flow_renderer


@pytest.mark.skipif(flow_renderer.Digraph is None, reason="graphviz python bindings not installed")
def test_flow_renderer_declares_ports_in_dot() -> None:
    data = {
        "nodes": [
            {
                "id": "step__branchscope",
                "name": "BranchScope",
                "kind": "control",
                "ports": [
                    {"name": "in_main", "direction": "in"},
                    {"name": "out_default", "direction": "out"},
                    {"name": "branch_success", "label": "Success", "direction": "out"},
                ],
            },
            {
                "id": "step__success",
                "name": "SuccessAction",
                "kind": "step",
                "ports": [
                    {"name": "in_main", "direction": "in"},
                    {"name": "out_default", "direction": "out"},
                ],
            },
        ],
        "edges": [
            {
                "source": "step__branchscope",
                "target": "step__success",
                "kind": "branch",
                "label": "Success",
                "source_port": "branch_success",
                "target_port": "in_main",
            }
        ],
    }
    dot = flow_renderer._build_graphviz_diagram(data)
    assert dot is not None
    source_text = dot.source
    assert 'port="branch_success"' in source_text
    assert "step__branchscope:branch_success" in source_text
