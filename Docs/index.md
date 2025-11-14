# What is 'single unique identifier'?

'Single unique identifier' is a proposed set of systems and standards to 
faciliate information sharing between child social care (CSC) systems.

The goal is to demonstrate the viability of using the NHS number as the SUI
and demonstrate how data could be transferred between data owners
nationally, to present this data as a single view of a child for the
improved safeguarding of children.

## Contents

### Structurizr

This documentation, and the models and diagrams contained therein, can be
visualised in an interactive UI using Structurizr.

#### Recommended setup:
1. Install Rancher Desktop, using the Docker Engine option during install:
    * Download from https://rancherdesktop.io/ (or https://github.com/rancher-sandbox/rancher-desktop/releases)
    * Rancher Desktop is a free, open-source application for running Docker
    images and does not require a license to use for commercial purposes.
2. Run Structurizr using Docker, from this repo's root:
    ```
    docker run -it --rm -p 8080:8080 -v ./Docs/:/usr/local/structurizr structurizr/lite
    ```

### Architecture

* [Architecture models & diagrams](./Architecture%20models/c4models.md)
* [Architecture decisions](./Architecture%20decisions/overview.md)

### Development

* [SUI.Matching.API - Getting started](./Developers/SUI.Matching.API/gettingstarted.md)
