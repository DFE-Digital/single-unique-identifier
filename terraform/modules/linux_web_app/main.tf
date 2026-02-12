locals {
  base_app_settings = merge(
    {
      WEBSITE_RUN_FROM_PACKAGE = "1"
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

resource "azurerm_linux_web_app" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.service_plan_id
  https_only          = var.https_only

  dynamic "identity" {
    for_each = var.enable_system_assigned_identity ? [1] : []
    content {
      type = "SystemAssigned"
    }
  }

  site_config {
    ftps_state = var.ftps_state

    dynamic "application_stack" {
      for_each = var.dotnet_version == null ? [] : [1]
      content {
        dotnet_version = var.dotnet_version
      }
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

  lifecycle {
    ignore_changes = [
      tags["hidden-link: /app-insights-resource-id"] # This hidden link is managed by Azure, and so is safe to ignore in the Terraform
    ]
  }
}
