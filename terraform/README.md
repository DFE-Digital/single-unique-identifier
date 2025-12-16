# Terraform

This folder holds the Terraform for the platform, including shared foundations (resource group) and per-service modules. Every deployable service should have its own Terraform root so it can be planned/applied in isolation.

## Structure

- `core/`: Root for shared/core infrastructure; `core/main.tf` composes modules for the environment (currently just the resource group) and exposes outputs for downstream modules.
- `singleview/`: Root for Single View, consuming outputs from `core` and deploying a web app onto the shared App Service plan.
- `service-example/`: Example service root showing how to consume outputs from `core` via remote state, deploy a web app using the reusable module, and set a per-service state key.
- `modules/resource_group`: Creates the environment resource group.
- `modules/linux_web_app`: Reusable Linux Web App module for services running on the shared App Service plan.
- `environments/*.tfvars`: Per-environment configuration; source of truth for naming and tags.

## Running via GitHub Actions (manual)

- `Terraform Backend Bootstrap`: Seeds the tfstate resource group/storage/container for the selected environment.
- `Terraform Core Infrastructure`: Runs init/validate/plan, and apply when `apply=true`, using the core root (`terraform/core`) with tfvars-driven naming and OIDC auth.
- `Terraform Plan and Apply`: Internal reusable workflow used by the core and service workflows.
- `Terraform Service Example`: Illustrative workflow for a service root; it is disabled via `if: ${{ false }}` to prevent accidental runs. Copy/enable per service with the correct root/path/state key.

### GitHub secrets expected

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
  (From the service principal configured for GitHub OIDC.)

## Adding another environment

1. Add `terraform/environments/<env>.tfvars` following the `d01` example.
2. Run the backend bootstrap workflow for that environment.
3. Run the Terraform Core workflow to plan/apply for that environment.

### State backend

The Azure backend must exist before `terraform init` runs. Use the manual workflow `.github/workflows/terraform-bootstrap.yml` to create the state resource group, storage account, and container using the values from the selected tfvars file (backend names are derived from those values).

## Adding a service module

Create `modules/<service>` with its resources and inputs. Then:

- Create a new root under `terraform/<service>/` that calls the relevant modules (for example `modules/linux_web_app`) and consumes outputs from `core`.
- Add any service-specific variables to the shared tfvars files so each environment’s settings stay in one place.
- Add a dedicated GitHub Actions workflow for the service root (similar to the core workflow) so plans/applies are isolated per service.

### Example service root

`terraform/service-example/` demonstrates:

- Deriving backend names from the same tfvars values (subscription/environment/region).
- Using `terraform_remote_state` to read `resource_group_name`/`resource_group_location` from the `core` state (key `<env>/terraform.tfstate`).
- An example per-service state key (`<env>/service-example.tfstate`).
- Calling the reusable Linux web app module (`modules/linux_web_app`) to deploy a web app onto the shared App Service plan.
- A disabled GitHub Actions workflow (`.github/workflows/terraform-service-example.yml`) that calls the reusable workflow; set `if: ${{ true }}` (or remove) when ready to use.
