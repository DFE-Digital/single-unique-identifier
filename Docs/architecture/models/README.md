# Architecture models

This directory contains architecture models for the Single Unique Identifier (SUI) service. These models are the authoritative source for the target architecture.

Currently, the models cover 1 architecture layer:

- data architecture, showing data stores, processes, data flows, participating organisations, and the data transferred between them

Planned layers will cover:

- business architecture, showing core capabilities of the system
- application architecture, showing integrated systems and high-level logical components using C4 context (C1) and container (C2) diagrams
- cloud architecture, showing deployed cloud resources and infrastructure
- security architecture, showing trust boundaries between systems, authentication flows, and policy enforcement.

If you find any discrepancies, contact the technical architect, Josh Taylor at [joshua.taylor@education.gov.uk](mailto:joshua.taylor@education.gov.uk).

## Editing the diagrams

All diagrams are created in [draw.io](https://app.diagrams.net/).

The source diagram data is embedded directly in the image files. To edit a diagram, open the image file directly in draw.io.

For more information on embedded diagrams, see the draw.io guide on [saving diagram data in an image file](https://www.drawio.com/docs/manual/collaboration/diagram-data-image-formats/#save-diagram-data-in-an-image).

### Markdown summaries for LLMs

Each diagram has an accompanying Markdown file that describes its components and data flows in text. This ensures the architecture is accessible and easy for large language models (LLMs) to process.

You can use an LLM to generate these files. If you do, you must:

- check the content for accuracy before committing
- update the Markdown file whenever you change the diagram
