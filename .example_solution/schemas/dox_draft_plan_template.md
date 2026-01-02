---
plan_version: 1
docs:
  - title: "Custom Application Properties Documentation"
    filename: "custom-app-props.md"
    inputs:
      - "com.tibco.bw.custom.app.props.CustomAppProps.md"
      - "Family_com.tibco.bw.custom.app.props.md"

  - title: "Credit Application Service Documentation"
    filename: "creditapp-service.md"
    inputs:
      - "creditapp.module.EquifaxScore.md"
      - "creditapp.module.ExperianScore.md"
      - "creditapp.module.MainProcess.md"
      - "Family_creditapp.module.md"

  - title: "Credit Check Backend Service Documentation"
    filename: "creditcheck-backend.md"
    inputs:
      - "creditcheckservice.LookupDatabase.md"
      - "creditcheckservice.Process.md"
      - "Family_creditcheckservice.md"

  - title: "Experian Service Documentation"
    filename: "experian-service.md"
    inputs:
      - "experianservice.module.Process.md"
      - "Family_experianservice.module.md"

  - title: "Logging Service Documentation"
    filename: "logging-service.md"
    inputs:
      - "loggingservice.LogProcess.md"
      - "Family_loggingservice.md"

  - title: "Movie Catalog Search Service Documentation"
    filename: "moviecatalogsearch-service.md"
    inputs:
      - "moviecatalogsearch.module.GetRatings.md"
      - "moviecatalogsearch.module.Process.md"
      - "moviecatalogsearch.module.SearchMovies.md"
      - "moviecatalogsearch.module.SortMovies.md"
      - "moviecatalogsearch.module.SortMovieSingle.md"
      - "moviecatalogsearch.module.SortSingleMovie.md"
      - "Family_moviecatalogsearch.module.md"

  - title: "Movie Search Service Documentation"
    filename: "moviesearch-service.md"
    inputs:
      - "moviesearch.module.Process.md"
      - "moviesearch.module.SearchOmdb.md"
      - "Family_moviesearch.module.md"

  - title: "Execution Event Subscriber Documentation"
    filename: "execution-event-subscriber.md"
    inputs:
      - "tibco.bw.sample.application.execution.event.subscriber.md"
      - "Family_tibco.bw.sample.application.execution.event.md"

  - title: "Unclassified / Utilities Documentation"
    filename: "unclassified-utilities.md"
    inputs:
      - "TIBCO BW Custom DataSource Factory.md"
      - "Family_unclassified.md"
meta:
  remaining_execs: 9
---

# Documentation Objectives

This documentation plan organizes the available process-level and family-level markdown files into coherent deliverables. Each deliverable groups related processes and their synthesized family views into a single document.

## Goals
- Consolidation: Merge auto-generated process documentation with family-level summaries for easier navigation.
- Traceability: Ensure that each service (credit app, credit check, movie search, logging, etc.) has a dedicated document that captures both technical and business perspectives.
- Clarity: Provide a structured YAML front matter that can be used by static site generators or documentation pipelines to build a navigable knowledge base.
- Maintainability: Keep related processes together so that updates to one service can be reflected in a single deliverable.

## Next Steps
- Review each generated deliverable for accuracy of endpoints, flows, and interdependencies.
- Add business context and usage examples where possible.
- Validate that all exposed REST endpoints are correctly documented and mapped to their respective processes.
