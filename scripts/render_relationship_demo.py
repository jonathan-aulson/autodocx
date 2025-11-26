from pathlib import Path
from autodocx.render import business_renderer

out_dir = Path('analysis/demo_docs')
out_dir.mkdir(parents=True, exist_ok=True)
component = {
    "title": "Demo Workflow",
    "llm_subscore": 0.9,
    "component": {
        "name": "Demo Workflow",
        "what_it_does": [
            {"claim": "Calls external pricing API", "evidence_ids": ["e1"]},
            {"claim": "Writes totals to Dataverse", "evidence_ids": ["e2"]},
        ],
    },
}
relationships = [
    {
        "id": "call_api",
        "source": {"type": "action", "name": "CallAPI"},
        "target": {"kind": "http", "ref": "https://example.com/api", "display": "https://example.com/api"},
        "operation": {"type": "calls", "verb": "POST", "crud": "execute", "protocol": "https"},
        "connector": "http",
        "direction": "outbound",
        "context": {"url_or_resource": "https://example.com/api"},
        "roles": ["interface.calls"],
        "evidence": ["demo:CallAPI"],
        "confidence": 0.9,
    },
    {
        "id": "write_dv",
        "source": {"type": "action", "name": "UpdateDataverse"},
        "target": {"kind": "dataverse", "ref": "accounts", "display": "accounts"},
        "operation": {"type": "writes", "verb": "PATCH", "crud": "update", "protocol": "dataverse"},
        "connector": "shared_commondataservice",
        "direction": "outbound",
        "context": {"table": "accounts"},
        "roles": ["data.mutates"],
        "evidence": ["demo:UpdateDataverse"],
        "confidence": 0.9,
    },
]
props = {
    "name": "Demo Flow",
    "file": "demo.json",
    "wf_kind": "power_automate",
    "engine": "logicapps",
    "triggers": [{"name": "manual", "type": "Request"}],
    "steps": [
        {"name": "CallAPI", "connector": "http"},
        {"name": "UpdateDataverse", "connector": "shared_commondataservice"},
    ],
    "relationships": relationships,
}
sir = {
    "id": "workflow:demo",
    "name": "Demo Flow",
    "component_or_service": "Demo",
    "props": props,
    "relationships": relationships,
    "relationship_matrix": {"http": {"calls": 1}, "dataverse": {"writes": 1}},
}

business_renderer.render_business_component_page(
    out_docs_dir=out_dir,
    group_id='DemoGroup',
    component_key='DemoWorkflow',
    c_json=component,
    sirs=[sir],
    evidence_md_filename=None,
    facets={"score": 0.9},
    settings={},
)
print(out_dir / 'components' / 'DemoGroup' / 'DemoWorkflow.md')
