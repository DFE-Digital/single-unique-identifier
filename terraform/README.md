# Terraform

This folder holds the Terraform for the platform, including shared foundations (resource group) and per-service modules.

## Structure

- `main.tf`: Composes modules for the environment (currently just the resource group) and exposes outputs for downstream modules.
- `modules/resource_group`: Creates the environment resource group; future service modules can be added under `modules/<service>` and wired into `main.tf`.
- `environments/*.tfvars`: Per-environment configuration; source of truth for naming and tags.

## Running via GitHub Actions (manual)

- `Terraform Backend Bootstrap`: Seeds the tfstate resource group/storage/container for the selected environment.
- `Terraform`: Runs init/validate/plan, and apply when `apply=true`, using the same tfvars-driven naming and OIDC auth.

### GitHub secrets expected

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
  (From the service principal configured for GitHub OIDC.)

## Adding another environment

1. Add `terraform/environments/<env>.tfvars` following the `d01` example.
2. Run the [backend bootstrap workflow](#state-backend) for that environment.
3. Run the Terraform workflow to plan/apply for that environment.

### State backend

The Azure backend must exist before `terraform init` runs. Use the manual workflow `.github/workflows/terraform-bootstrap.yml` to create the state resource group, storage account, and container using the values from the selected tfvars file (backend names are derived from those values).

## Adding a service module

Create `modules/<service>` with its resources and inputs. Call it from `main.tf`, wiring dependencies to `module.resource_group` (name/location/tags). Keep service-specific variables in tfvars so each environment can override as needed.
