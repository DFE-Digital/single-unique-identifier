locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  find_state_key    = format("%s/find.tfstate", var.environment_id)
  service_state_key = format("%s/ui_test_harness.tfstate", var.environment_id)

  web_app_descriptor   = "uiharness01"
  web_app_name = format("%s%sapp-%s-%s", var.subscription_prefix, var.environment_id, var.region_short, local.web_app_descriptor)
  
  key_vault_descriptor = "uiharnesskv01" // gitleaks:allow
  key_vault_name       = format("%s%skv-%s-%s", var.subscription_prefix, var.environment_id, var.region_short, local.key_vault_descriptor)

  otel_resource_attributes = join(",", [
    "deployment.environment=${var.environment_tag}",
    "service.namespace=${var.product}",
    "cloud.region=${var.location}",
  ])
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

data "terraform_remote_state" "find" {
  backend = "azurerm"
  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.find_state_key
  }
}

# 1. UI Harness Key Vault
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
  tags             = var.tags
}

# 1a. Give Terraform Operator access to create the secret
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

resource "azurerm_key_vault_access_policy" "terraform_operator" {
  count = var.key_vault_use_rbac ? 0 : 1

  key_vault_id = module.key_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = ["Get", "Set", "Delete", "List"]
}

# 2. UI Harness Password Secret
resource "azurerm_key_vault_secret" "ui_harness_password" {
  name         = "UI-TEST-HARNESS-PASSWORD"
  value        = var.ui_test_harness_password
  key_vault_id = module.key_vault.id
  content_type = "text/plain"

  depends_on = [
    module.rbac_assignments_terraform_operator,
    azurerm_key_vault_access_policy.terraform_operator
  ]
}

# 3. Look up the Find Secret using the ID from the Find remote state
data "azurerm_key_vault_secret" "find_match_api_key" {
  name         = "find-match-api-key"
  key_vault_id = data.terraform_remote_state.find.outputs.key_vault_id
}

# 4. Web App
module "web_app" {
  source = "../modules/linux_web_app"

  name                = local.web_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  service_plan_id     = data.terraform_remote_state.core.outputs.app_service_plan_id

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.webapp_dotnet_version

  app_settings = merge(
    {
      APPLICATIONINSIGHTS_CONNECTION_STRING = data.terraform_remote_state.core.outputs.app_insights_connection_string
      OTEL_RESOURCE_ATTRIBUTES              = local.otel_resource_attributes

      # Key Vault References mapped to App Settings
      UI_TEST_HARNESS_PASSWORD = "@Microsoft.KeyVault(SecretUri=${module.key_vault.vault_uri}secrets/${azurerm_key_vault_secret.ui_harness_password.name}/)"
      MATCH_API_KEY            = "@Microsoft.KeyVault(SecretUri=${data.azurerm_key_vault_secret.find_match_api_key.versionless_id})"
    },
    var.ui_harness_app_settings,
  )
  tags = var.tags
}

# 5. Grant Web App access to its own Key Vault
module "rbac_assignments_ui_harness_app" {
  source = "../modules/rbac_assignments"
  scope  = module.key_vault.id

  assignments = var.key_vault_use_rbac ? {
    function_app_secrets = {
      principal_id = module.web_app.principal_id
      role_name    = "Key Vault Secrets User"
    }
  } : {}
}

resource "azurerm_key_vault_access_policy" "ui_harness_app" {
  count = var.key_vault_use_rbac ? 0 : 1

  key_vault_id = module.key_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.web_app.principal_id

  secret_permissions = ["Get", "List"]
}

# 6. Grant Web App access to the Find Key Vault
module "rbac_assignments_find_kv" {
  source = "../modules/rbac_assignments"
  scope  = data.terraform_remote_state.find.outputs.key_vault_id

  assignments = var.key_vault_use_rbac ? {
    function_app_secrets = {
      principal_id = module.web_app.principal_id
      role_name    = "Key Vault Secrets User"
    }
  } : {}
}

resource "azurerm_key_vault_access_policy" "find_kv" {
  count = var.key_vault_use_rbac ? 0 : 1

  key_vault_id = data.terraform_remote_state.find.outputs.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.web_app.principal_id

  secret_permissions = ["Get"]
}