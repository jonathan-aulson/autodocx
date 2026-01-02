from pathlib import Path

from autodocx.extractors.typescript_project import TypeScriptProjectExtractor


def test_typescript_extractor_reads_tsconfig(tmp_path: Path) -> None:
    tsconfig = tmp_path / "tsconfig.json"
    tsconfig.write_text(
        """
{
  "compilerOptions": {
    "rootDir": "src",
    "outDir": "dist",
    "module": "commonjs",
    "target": "ES2022",
    "paths": {
      "@app/*": ["src/app/*"]
    }
  },
  "include": ["src/**/*.ts"]
}
""",
        encoding="utf-8",
    )
    extractor = TypeScriptProjectExtractor()
    signals = list(extractor.extract(tsconfig))
    assert len(signals) == 1
    signal = signals[0]
    assert signal.props["root_dir"] == "src"
    assert signal.props["alias_count"] == 1


def test_typescript_extractor_reads_package_json(tmp_path: Path) -> None:
    package = tmp_path / "package.json"
    package.write_text(
        """
{
  "name": "demo-app",
  "scripts": {
    "start": "ts-node src/index.ts",
    "build": "tsc -p .",
    "lint": "eslint ."
  },
  "devDependencies": {
    "typescript": "^5.0.0",
    "@nestjs/core": "10.0.0",
    "eslint": "^9.0.0"
  }
}
""",
        encoding="utf-8",
    )
    extractor = TypeScriptProjectExtractor()
    signals = list(extractor.extract(package))
    assert len(signals) == 1
    signal = signals[0]
    assert "nest" in signal.props["frameworks"]
    assert any(script["name"] == "start" for script in signal.props["scripts"])
