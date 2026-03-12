# Structurizr Models

The models and diagrams contained within this directory can be visualised in an interactive UI using Structurizr.

## Recommended setup:
1. Install Rancher Desktop, using the Docker Engine option during install:
    * Download from https://rancherdesktop.io/ (or https://github.com/rancher-sandbox/rancher-desktop/releases)
    * Rancher Desktop is a free, open-source application for running Docker
    images and does not require a license to use for commercial purposes.
2. Run Structurizr using Docker and mount the Structurizr directory, from this **repo's root**:
    ```
    docker run -d -p 2323:8080 -v ./Docs/Structurizr/:/usr/local/structurizr --name sui-structurizr structurizr/lite
    ```
3. Go to http://localhost:2323/workspace/diagrams#SUI
