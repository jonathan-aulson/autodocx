# PIPELINE_COMMANDS.MD

---

RUN FULL PIPELINE
python .\.roo\tools\bw\bw_orchestrate.py run --root .\bw-samples-master --include-archives

DELETE .\OUT\DOCS
python .\.roo\tools\bw\wipeout.py

SET MKDOCS HOMEPAGE
python .\.roo\tools\bw\set_homepage.py .\out\docs\repo-overview.md

---
