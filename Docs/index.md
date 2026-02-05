# What is 'single unique identifier'?

'Single unique identifier' is a proposed set of systems and standards to 
faciliate information sharing between child social care (CSC) systems.

This workspace/repository contains software that demonstrates the viability
of using the NHS number as the SUI and how data could be transferred between
data owners nationally, to present this data as a single view of a child for
the improved safeguarding and welfare of children.

## Contributing to the Docs

* Please make sure that the `./Architecture decisions` Markdown documents are navigable outside of Structurizer, by running the `./Architecture decisions/generate-overview-table.sh` bash script if any ADRs are changed/added.
* After changing/adding any decisions and documentation, please make sure that Structurizer runs and displays correctly, and check it has no errors in the UI after loading it.

## Structurizr

This documentation, and the models and diagrams contained therein, can be
visualised in an interactive UI using Structurizr.

### Recommended setup:
1. Install Rancher Desktop, using the Docker Engine option during install:
    * Download from https://rancherdesktop.io/ (or https://github.com/rancher-sandbox/rancher-desktop/releases)
    * Rancher Desktop is a free, open-source application for running Docker
    images and does not require a license to use for commercial purposes.
2. Run Structurizr using Docker, from this repo's root:
    ```
    docker run -it --rm -p 2323:8080 -v ./Docs/:/usr/local/structurizr structurizr/lite
    ```

## Contents

### Architecture

* [Architecture models & diagrams](./Architecture%20models/c4models.md)
* [Architecture decisions](./Architecture%20decisions/overview.md)

### Development

* [SUI.Matching.API - Getting started](./Developers/SUI.Matching.API/gettingstarted.md)
