from pathlib import Path

from autodocx.extractors.bw_archives import BwArchiveExpansionExtractor
from autodocx.extractors.bw_diagram_bin import BwDiagramBinaryExtractor
from autodocx.extractors.bw_java_osgi import BwJavaOsgiComponentExtractor
from autodocx.extractors.bw_service_descriptor import BwServiceDescriptorExtractor
from autodocx.extractors.bw_tests import BwTestSuiteExtractor


def test_bw_archive_expansion_lists_entries(tmp_path: Path) -> None:
    archive = tmp_path / "Sample.zip"
    import zipfile

    with zipfile.ZipFile(archive, "w") as zf:
        zf.writestr("inner/process.bwp", "<process/>")
        zf.writestr("META-INF/MANIFEST.MF", "Bundle-SymbolicName: sample.bundle")

    ex = BwArchiveExpansionExtractor()
    sig = next(iter(ex.extract(archive)))
    assert sig.kind == "archive"
    entries = sig.props.get("entries") or []
    names = [e.get("name") for e in entries]
    assert "inner/process.bwp" in names
    assert "META-INF/MANIFEST.MF" in names


def test_bw_diagram_binary_emits_signal(tmp_path: Path) -> None:
    path = tmp_path / "Process.bwd"
    path.write_bytes(b"binary-diagram")
    ex = BwDiagramBinaryExtractor()
    sig = next(iter(ex.extract(path)))
    assert sig.kind == "diagram"
    assert sig.props["binary_diagram"] is True


def test_bw_java_osgi_manifest_fields(tmp_path: Path) -> None:
    mf = tmp_path / "MANIFEST.MF"
    mf.write_text(
        "Bundle-SymbolicName: com.sample.bundle\nBundle-Activator: com.sample.Activator\nBundle-ClassPath: lib/a.jar\n",
        encoding="utf-8",
    )
    ex = BwJavaOsgiComponentExtractor()
    sig = next(iter(ex.extract(mf)))
    props = sig.props
    assert props["bundle_symbolic_name"] == "com.sample.bundle"
    assert props["bundle_activator"] == "com.sample.Activator"
    assert "bundle_classpath" in props


def test_bw_service_descriptor_rest(tmp_path: Path) -> None:
    sd = tmp_path / "MyProcess-REST.json"
    sd.write_text(
        '{"interface":{"rest":{"path":"/score","method":"POST","inputs":["id"],"outputs":["score"]}}}',
        encoding="utf-8",
    )
    ex = BwServiceDescriptorExtractor()
    sig = next(iter(ex.extract(sd)))
    props = sig.props
    assert props["endpoint"] == "/score"
    assert props["method"] == "POST"
    assert props["inputs"] == ["id"]
    assert props["outputs"] == ["score"]


def test_bw_test_suite_extractor(tmp_path: Path) -> None:
    tf = tmp_path / "TEST-Credit.bwt"
    tf.write_text('{"inputs": {"id": 1}, "expected": {"score": 800}}', encoding="utf-8")
    ex = BwTestSuiteExtractor()
    sig = next(iter(ex.extract(tf)))
    props = sig.props
    assert props["inputs"]["id"] == 1
    assert props["expected"]["score"] == 800
