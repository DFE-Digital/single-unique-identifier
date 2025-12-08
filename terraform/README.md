# Terraform

This folder holds the Terraform for the platform, including shared foundations (resource group) and per-service modules. Every deployable service should have its own Terraform root so it can be planned/applied in isolation.

## Structure

- `core/`: Root for shared/core infrastructure; `core/main.tf` composes modules for the environment (currently just the resource group) and exposes outputs for downstream modules.
- `service-<name>/`: Create a new root per service (for example `service-singleview/`, `service-transfer/`) that composes service-specific modules and consumes outputs from `core` (for example, resource group).
- `modules/resource_group`: Creates the environment resource group; future service modules can be added under `modules/<service>` and wired into the relevant service root.
- `environments/*.tfvars`: Per-environment configuration; source of truth for naming and tags.

## Running via GitHub Actions (manual)

- `Terraform Backend Bootstrap`: Seeds the tfstate resource group/storage/container for the selected environment.
- `Terraform Core Infrastructure`: Runs init/validate/plan, and apply when `apply=true`, using the core root (`terraform/core`) with tfvars-driven naming and OIDC auth.
- `Terraform Plan and Apply`: Internal reusable workflow used by the core and service workflows.

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

- Create a new root under `terraform/service-<name>/` that calls the service module and consumes outputs from `core` (for example, `module.resource_group.name`/`location`).
- Add any service-specific variables to the shared tfvars files so each environment’s settings stay in one place.
- Add a dedicated GitHub Actions workflow for the service root (similar to the core workflow) so plans/applies are isolated per service.
