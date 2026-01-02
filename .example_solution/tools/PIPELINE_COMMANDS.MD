# PIPELINE_COMMANDS.MD

---

1) RUN FULL PIPELINE
python .\.roo\tools\bw\bw_orchestrate.py run --root .\bw-samples-master --include-archives

---

REORG DOCS
python .\.roo\tools\bw\docs_reorganize.py

SET MKDOCS HOMEPAGE
python .\.roo\tools\bw\set_homepage.py .\out\docs\repo-overview.md

MKDOCS CONFIG SETUP & NAVIGATION SIDEBAR
python .\.roo\tools\bw\mkdocs_bootstrap.py --force-config --force-home

DELETE .\OUT\DOCS
python .\.roo\tools\bw\wipeout.py

---
