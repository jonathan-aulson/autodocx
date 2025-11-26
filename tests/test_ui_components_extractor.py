from __future__ import annotations

from pathlib import Path

from autodocx.extractors.ui_components import UIComponentsExtractor


def test_react_component_detection(tmp_path: Path) -> None:
    source = tmp_path / "Dashboard.tsx"
    source.write_text(
        """
import React from "react";
function Dashboard() {
  return <div>Hi</div>;
}
""",
        encoding="utf-8",
    )
    extractor = UIComponentsExtractor()
    signals = list(extractor.extract(source))
    assert signals
    assert signals[0].props["name"] == "Dashboard"


def test_razor_route_detection(tmp_path: Path) -> None:
    source = tmp_path / "Home.cshtml"
    source.write_text(
        '@page "/home"\n<h1>Home</h1>',
        encoding="utf-8",
    )
    extractor = UIComponentsExtractor()
    signals = list(extractor.extract(source))
    assert signals[0].props["routes"] == ["/home"]


def test_angular_component_detection(tmp_path: Path) -> None:
    source = tmp_path / "dashboard.component.ts"
    source.write_text(
        """
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {}
""",
        encoding="utf-8",
    )
    extractor = UIComponentsExtractor()
    signals = list(extractor.extract(source))
    assert signals
    assert signals[0].props["selector"] == "app-dashboard"


def test_angular_template_html_detection(tmp_path: Path) -> None:
    source = tmp_path / "reports.component.html"
    source.write_text('<a routerLink="/reports/detail">Detail</a>', encoding="utf-8")
    extractor = UIComponentsExtractor()
    signals = list(extractor.extract(source))
    assert signals
    assert signals[0].props["routes"] == ["/reports/detail"]
