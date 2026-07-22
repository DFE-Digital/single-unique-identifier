locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  service_state_key = format("%s/getanidentifier.tfstate", var.environment_id)

  function_descriptor = "getanid01"
  function_app_name = format(
    "%s%sfunc-%s-%s",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
    local.function_descriptor
  )

  # Storage account name for Function App (must be globally unique, 3-24 lowercase letters/numbers)
  function_storage_account_name = lower(
    format("%s%sstfunc%s", var.subscription_prefix, var.environment_id, local.function_descriptor)
  )

  otel_resource_attributes = join(",", [
    "deployment.environment=${var.environment_tag}",
    "service.namespace=${var.product}",
    "cloud.region=${var.location}",
  ])

  # Key Vault name
  key_vault_descriptor = "getanidkv01"
  key_vault_name = format(
    "%s%skv-%s-%s",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
    local.key_vault_descriptor
  )
}

data "azurerm_client_config" "current" {}

data "terraform_remote_state" "core" {
  backend = "azurerm"

  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.core_state_key
  }
}

module "key_vault" {
  source = "../modules/key_vault"

  name                = local.key_vault_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location

  tenant_id                  = data.azurerm_client_config.current.tenant_id
  rbac_authorization_enabled = var.key_vault_use_rbac

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  tags = var.tags
}

module "rbac_assignments_terraform_operator" {
  source = "../modules/rbac_assignments"
  scope  = module.key_vault.id

  assignments = var.key_vault_use_rbac ? {
    terraform_operator_secrets = {
      principal_id = data.azurerm_client_config.current.object_id
      role_name    = "Key Vault Secrets Officer"
    }
  } : {}
}

# Access policy fallback for environments that cannot create RBAC role assignments.
resource "azurerm_key_vault_access_policy" "terraform_operator" {
  count = var.key_vault_use_rbac ? 0 : 1

  key_vault_id = module.key_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  key_permissions = [
    "Get",
  ]

  secret_permissions = [
    "Get",
    "Set",
    "Delete",
    "List",
  ]

  storage_permissions = [
    "Get",
  ]
}

module "function_app" {
  source = "../modules/linux_function_app"

  name                = local.function_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  service_plan_id     = data.terraform_remote_state.core.outputs.app_service_plan_id

  storage_account_name              = local.function_storage_account_name
  app_service_integration_subnet_id = data.terraform_remote_state.core.outputs.function_app_integration_subnet_id
  log_analytics_workspace_id        = data.terraform_remote_state.core.outputs.log_analytics_workspace_id

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.function_dotnet_version

  app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME = "dotnet-isolated"
      WEBSITE_RUN_FROM_PACKAGE = "1"
      OTEL_RESOURCE_ATTRIBUTES = local.otel_resource_attributes
    },
    var.getanidentifier_app_settings
  )

  application_insights_connection_string = data.terraform_remote_state.core.outputs.app_insights_connection_string

  tags = var.tags
}

module "rbac_assignments_function_app" {
  source = "../modules/rbac_assignments"
  scope  = module.key_vault.id

  assignments = var.key_vault_use_rbac ? {
    function_app_secrets = {
      principal_id = module.function_app.principal_id
      role_name    = "Key Vault Secrets User"
    }
  } : {}
}

resource "azurerm_key_vault_access_policy" "function_app" {
  count = var.key_vault_use_rbac ? 0 : 1

  key_vault_id = module.key_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.function_app.principal_id

  secret_permissions = [
    "Get",
    "List",
  ]
}