locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key      = format("%s/terraform.tfstate", var.environment_id)
  service_state_key   = format("%s/find.tfstate", var.environment_id)

  custodians_service_state_key = format("%s/custodians.tfstate", var.environment_id)

  function_descriptor = "find01"
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

  # AuditProcessor configuration
  audit_processor_descriptor = "findaudit01"
  audit_processor_function_app_name = format(
    "%s%sfunc-%s-%s",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
    local.audit_processor_descriptor
  )

  # Storage account name for AuditProcessor (used for both function internals and audit data)
  audit_processor_storage_account_name = lower(
    format("%s%sst%s", var.subscription_prefix, var.environment_id, local.audit_processor_descriptor)
  )
}

data "terraform_remote_state" "core" {
  backend = "azurerm"

  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.core_state_key
  }
}

data "terraform_remote_state" "stub_custodians" {
  count   = var.use_stub_custodians ? 1 : 0
  backend = "azurerm"

  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.custodians_service_state_key
  }
}

module "function_app" {
  source = "../modules/linux_function_app"

  name                = local.function_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  service_plan_id     = data.terraform_remote_state.core.outputs.app_service_plan_id

  storage_account_name = local.function_storage_account_name

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.function_dotnet_version

  app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME       = "dotnet-isolated"
      FUNCTIONS_EXTENSION_VERSION    = "~4"
      WEBSITE_RUN_FROM_PACKAGE       = "1"
      APPLICATIONINSIGHTS_CONNECTION_STRING = data.terraform_remote_state.core.outputs.app_insights_connection_string
      AuditProcessorConnectionString = module.audit_processor_function_app.storage_connection_string
      StubCustodiansBaseUrl          = try(
        "https://${data.terraform_remote_state.stub_custodians[0].outputs.web_app_default_hostname}",
        null
      )
    },
    var.app_settings
  )

  tags = var.tags
}

module "audit_processor_function_app" {
  source = "../modules/linux_function_app"

  name                = local.audit_processor_function_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  service_plan_id     = data.terraform_remote_state.core.outputs.app_service_plan_id

  storage_account_name = local.audit_processor_storage_account_name

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.function_dotnet_version

  app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME    = "dotnet-isolated"
      FUNCTIONS_EXTENSION_VERSION = "~4"
      WEBSITE_RUN_FROM_PACKAGE    = "1"
      APPLICATIONINSIGHTS_CONNECTION_STRING = data.terraform_remote_state.core.outputs.app_insights_connection_string
    },
    var.app_settings
  )

  tags = var.tags
}
