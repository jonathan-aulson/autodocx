from __future__ import annotations
from dataclasses import dataclass
from typing import Dict, Any, List

@dataclass
class Signal:
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
