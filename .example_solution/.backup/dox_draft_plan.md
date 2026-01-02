---
docs:
- title: Custom Application Properties Reference
  filename: custom-app-props.md
  inputs:
  - com.tibco.bw.custom.app.props.CustomAppProps.md
  - Family_com.tibco.bw.custom.app.props.md
  status: done
  generated_at: '2025-09-12T20:02:56Z'
- title: Credit Application Service Guide
  filename: creditapp-service.md
  inputs:
  - creditapp.module.EquifaxScore.md
  - creditapp.module.ExperianScore.md
  - creditapp.module.MainProcess.md
  - Family_creditapp.module.md
  status: done
  generated_at: '2025-09-12T20:03:18Z'
- title: Credit Check Backend Service Guide
  filename: creditcheck-backend.md
  inputs:
  - creditcheckservice.LookupDatabase.md
  - creditcheckservice.Process.md
  - Family_creditcheckservice.md
  status: done
  generated_at: '2025-09-12T20:03:30Z'
- title: Experian Demo Service Guide
  filename: experian-service.md
  inputs:
  - experianservice.module.Process.md
  - Family_experianservice.module.md
  status: done
  generated_at: '2025-09-12T20:03:47Z'
- title: Logging Service Guide
  filename: logging-service.md
  inputs:
  - loggingservice.LogProcess.md
  - Family_loggingservice.md
  status: done
  generated_at: '2025-09-12T20:04:07Z'
- title: Movie Catalog Search Service Guide
  filename: moviecatalog-service.md
  inputs:
  - moviecatalogsearch.module.GetRatings.md
  - moviecatalogsearch.module.Process.md
  - moviecatalogsearch.module.SearchMovies.md
  - moviecatalogsearch.module.SortMovies.md
  - moviecatalogsearch.module.SortMovieSingle.md
  - moviecatalogsearch.module.SortSingleMovie.md
  - Family_moviecatalogsearch.module.md
  status: done
  generated_at: '2025-09-12T20:04:32Z'
- title: Movie Search Service Guide
  filename: moviesearch-service.md
  inputs:
  - moviesearch.module.Process.md
  - moviesearch.module.SearchOmdb.md
  - Family_moviesearch.module.md
  status: done
  generated_at: '2025-09-12T20:04:50Z'
- title: Custom JDBC Driver Extension
  filename: custom-jdbc-driver.md
  inputs:
  - TIBCO BW Custom DataSource Factory.md
  - Family_custom.jdbc.driver.md
  status: done
  generated_at: '2025-09-12T20:05:03Z'
- title: Execution Event Subscriber
  filename: execution-event-subscriber.md
  inputs:
  - tibco.bw.sample.application.execution.event.subscriber.md
  - Family_tibco.bw.sample.application.execution.event.md
  status: done
  generated_at: '2025-09-12T20:05:14Z'
- title: Repository Overview
  filename: repo-overview.md
  inputs:
  - REPO_OVERVIEW.md
  status: done
  generated_at: '2025-09-12T20:05:43Z'
meta:
  remaining_execs: 0
---

# Documentation Objectives

This documentation plan organizes the corpus into a set of cohesive deliverables. Each deliverable groups related process documentation and family-level summaries into a single guide.  

**Objectives:**
- Provide clear, modular documentation for each functional service (CreditApp, CreditCheck, Experian, Logging, Movie services).
- Consolidate technical process details with family-level overviews to give both granular and domain perspectives.
- Ensure that cross-cutting concerns (custom JDBC, execution event subscriber, custom app properties) are documented separately for reuse.
- Maintain a repository-level overview for orientation and navigation.

This structure allows developers, architects, and operators to quickly locate the relevant service guide, understand exposed endpoints, and trace interdependencies across modules. Each guide will include business context, technical appendix, and interdependency maps to support both design-time and run-time needs.