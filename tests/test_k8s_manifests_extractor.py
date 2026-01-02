from pathlib import Path

from autodocx.extractors.k8s_manifests import K8sManifestsExtractor

K8S_YAML = """
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
spec:
  template:
    spec:
      containers:
        - name: api
          image: myregistry.local/api:1.0
          env:
            - name: SECRET_TOKEN
              valueFrom:
                secretKeyRef:
                  name: api-secret
      volumes:
        - name: config
          configMap:
            name: api-config
"""


def test_k8s_extractor_surface_hints(tmp_path: Path) -> None:
    manifest = tmp_path / "deploy.yml"
    manifest.write_text(K8S_YAML, encoding="utf-8")

    extractor = K8sManifestsExtractor()
    signals = list(extractor.extract(manifest))
    assert signals
    props = signals[0].props
    assert "api-config" in (props.get("datasource_tables") or [])
    assert "myregistry.local/api:1.0" in (props.get("service_dependencies") or [])
