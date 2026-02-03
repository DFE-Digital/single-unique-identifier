locals {
  base_app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME    = "dotnet-isolated"
      FUNCTIONS_EXTENSION_VERSION = "~4"
      WEBSITE_RUN_FROM_PACKAGE    = "1"
    },
      var.environment_tag == null ? {} : { ASPNETCORE_ENVIRONMENT = var.environment_tag },
  )

  base_tags = merge(
    {
      ManagedBy = "terraform"
    },
      var.environment_tag == null ? {} : { Environment = var.environment_tag },
      var.product == null ? {} : { Product = var.product },
      var.service_offering == null ? {} : { "Service Offering" = var.service_offering },
  )
}

resource "azurerm_storage_account" "this" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  allow_nested_items_to_be_public = false

  tags = merge(
    local.base_tags,
    var.tags,
  )
}

resource "azurerm_linux_function_app" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.service_plan_id

  storage_account_name       = azurerm_storage_account.this.name
  storage_account_access_key = azurerm_storage_account.this.primary_access_key

  https_only = var.https_only

  dynamic "identity" {
    for_each = var.enable_system_assigned_identity ? [1] : []
    content {
      type = "SystemAssigned"
    }
  }

  site_config {
    ftps_state = var.ftps_state

    application_stack {
      dotnet_version = var.dotnet_version
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge(
    local.base_app_settings,
    var.app_settings,
  )

  tags = merge(
    local.base_tags,
    var.tags,
  )
}
