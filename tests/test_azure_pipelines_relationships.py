from __future__ import annotations

from pathlib import Path

from autodocx.extractors.azure_pipelines import AzurePipelinesExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold


PIPELINE_YAML = """
stages:
- stage: Build
  jobs:
    - job: BuildJob
      steps:
        - task: PublishBuildArtifacts@1
          inputs:
            ArtifactName: drop
- stage: Deploy
  dependsOn: Build
  jobs:
    - deployment: DeployJob
      environment: prod
      strategy:
        runOnce:
          deploy:
            steps:
              - task: PowerPlatformImportSolution@2
                inputs:
                  PowerPlatformSPN: STAGE
"""


def test_azure_pipelines_extractor_relationships(tmp_path: Path) -> None:
    pipeline_path = tmp_path / "azure-pipelines.yml"
    pipeline_path.write_text(PIPELINE_YAML, encoding="utf-8")

    extractor = AzurePipelinesExtractor()
    signals = list(extractor.extract(pipeline_path))
    assert signals, "Expected pipeline signal"
    relationships = signals[0].props.get("relationships") or []
    ops = {rel["operation"]["type"] for rel in relationships}
    targets = {rel["target"]["kind"] for rel in relationships}
    assert "depends_on" in ops, "Stage dependencies should be captured"
    assert "deploys_to" in ops and "environment" in targets, "Environment relationship expected"
    assert "artifact" in targets, "Artifact publishing relationship expected"
    steps = signals[0].props.get("steps") or []
    assert steps, "pipeline steps metadata should be captured"
    datastores = signals[0].props.get("datasource_tables") or []
    assert "drop" in datastores
    service_deps = signals[0].props.get("service_dependencies") or []
    assert "prod" in service_deps
    scaffold = build_scaffold(signals[0])
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]
