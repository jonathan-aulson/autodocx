from __future__ import annotations
from dataclasses import dataclass
from typing import Dict, Any, List

@dataclass
class Signal:
    """
    Fundamental record emitted by extractors.
    `props` may include narrative context such as user_story, inputs_example, outputs_example,
    latency_hints, route_hierarchy, ui_snapshot/screenshots, data_samples, foreign_keys,
    journey_touchpoints, and experience pack hints. Downstream consumers should tolerate
    new keys without breaking; only rely on presence when documented in `developer_onboarding_context.md`.
    """
    kind: str                    # 'api', 'op', 'workflow', 'event', 'db', 'infra', 'job', 'doc', ...
    props: Dict[str, Any]        # minimal facts from a single file (no cross-file inference)
    evidence: List[str]          # file:line anchors or short markers
    subscores: Dict[str, float]  # facets like parsed, schema_evidence, etc.

@dataclass
class Node:
    id: str
    type: str                    # API, Operation, Workflow, MessageTopic, Datastore, InfraResource, Doc, Job
    name: str
    props: Dict[str, Any]
    evidence: List[str]
    subscores: Dict[str, float]

@dataclass
class Edge:
    source: str
    target: str
    type: str                    # calls, publishes, reads, writes, deploys_to, depends_on, exposes_port
    props: Dict[str, Any]
    evidence: List[str]
    subscores: Dict[str, float]
