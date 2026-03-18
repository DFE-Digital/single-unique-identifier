locals {
  base_app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME    = "dotnet-isolated"
      FUNCTIONS_EXTENSION_VERSION = "~4"
      WEBSITE_RUN_FROM_PACKAGE    = "1"

      APPLICATIONINSIGHTS_CONNECTION_STRING = var.application_insights_connection_string
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

# Accepted until Alpha while the DR posture for Function App storage is still under review.
#trivy:ignore:AZU-0058
resource "azurerm_storage_account" "this" {
  name                               = var.storage_account_name
  resource_group_name                = var.resource_group_name
  location                           = var.location
  account_tier                       = "Standard"
  account_replication_type           = var.storage_account_replication_type
  infrastructure_encryption_enabled = true

  allow_nested_items_to_be_public = false

  network_rules {
    default_action             = "Deny"
    bypass                     = ["AzureServices"]
    virtual_network_subnet_ids = [var.app_service_integration_subnet_id]
  }

  queue_properties {
    logging {
      delete                = true
      read                  = true
      write                 = true
      version               = "1.0"
      retention_policy_days = 30
    }
  }

  tags = merge(
    local.base_tags,
    var.tags,
  )
}

resource "azurerm_monitor_diagnostic_setting" "storage_service" {
  for_each = var.log_analytics_workspace_id == null ? {} : {
    blob  = "${azurerm_storage_account.this.id}/blobServices/default"
    file  = "${azurerm_storage_account.this.id}/fileServices/default"
    queue = "${azurerm_storage_account.this.id}/queueServices/default"
    table = "${azurerm_storage_account.this.id}/tableServices/default"
  }

  name                       = "${var.storage_account_name}-${each.key}-diag"
  target_resource_id         = each.value
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "StorageRead"
  }

  enabled_log {
    category = "StorageWrite"
  }

  enabled_log {
    category = "StorageDelete"
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

resource "azurerm_linux_function_app" "this" {
  name                      = var.name
  resource_group_name       = var.resource_group_name
  location                  = var.location
  service_plan_id           = var.service_plan_id
  virtual_network_subnet_id = var.app_service_integration_subnet_id

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
    always_on              = true
    ftps_state             = var.ftps_state
    vnet_route_all_enabled = true

    health_check_path                 = var.health_check_path
    health_check_eviction_time_in_min = var.health_check_path == null ? null : 5

    application_stack {
      dotnet_version               = var.dotnet_version
      use_dotnet_isolated_runtime = true
    }

    application_insights_connection_string = var.application_insights_connection_string
  }

  app_settings = merge(
    local.base_app_settings,
    var.app_settings,
  )

  tags = merge(
    local.base_tags,
    var.tags,
  )

  lifecycle {
    ignore_changes = [
      tags["hidden-link: /app-insights-resource-id"] # This hidden link is managed by Azure, and so is safe to ignore in the Terraform
    ]
  }
}
