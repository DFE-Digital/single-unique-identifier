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

# Accepted until Alpha while the web app auth and mTLS approach is still under review.
#trivy:ignore:AZU-0001 trivy:ignore:AZU-0003
resource "azurerm_linux_web_app" "this" {
  name                       = var.name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  service_plan_id            = var.service_plan_id
  https_only                 = var.https_only
  client_certificate_enabled = var.client_certificate_enabled
  client_certificate_mode    = var.client_certificate_mode

  dynamic "identity" {
    for_each = var.enable_system_assigned_identity ? [1] : []
    content {
      type = "SystemAssigned"
    }
  }

  site_config {
    ftps_state    = var.ftps_state
    http2_enabled = var.http2_enabled

    health_check_path                 = var.health_check_path
    health_check_eviction_time_in_min = var.health_check_path == null ? null : 5

    dynamic "application_stack" {
      for_each = var.dotnet_version == null ? [] : [1]
      content {
        dotnet_version = var.dotnet_version
      }
    }
  }

  dynamic "auth_settings_v2" {
    for_each = var.auth_settings_v2 == null ? [] : [var.auth_settings_v2]

    content {
      auth_enabled           = lookup(auth_settings_v2.value, "auth_enabled", true)
      runtime_version        = lookup(auth_settings_v2.value, "runtime_version", null)
      config_file_path       = lookup(auth_settings_v2.value, "config_file_path", null)
      require_authentication = lookup(auth_settings_v2.value, "require_authentication", null)
      unauthenticated_action = lookup(auth_settings_v2.value, "unauthenticated_action", null)
      default_provider       = lookup(auth_settings_v2.value, "default_provider", null)
      excluded_paths         = lookup(auth_settings_v2.value, "excluded_paths", null)
      require_https          = lookup(auth_settings_v2.value, "require_https", null)

      dynamic "active_directory_v2" {
        for_each = try(auth_settings_v2.value.active_directory_v2, null) == null ? [] : [auth_settings_v2.value.active_directory_v2]

        content {
          client_id                  = lookup(active_directory_v2.value, "client_id", null)
          tenant_auth_endpoint       = lookup(active_directory_v2.value, "tenant_auth_endpoint", null)
          client_secret_setting_name = lookup(active_directory_v2.value, "client_secret_setting_name", null)
          allowed_applications       = lookup(active_directory_v2.value, "allowed_applications", null)
          allowed_audiences          = lookup(active_directory_v2.value, "allowed_audiences", null)
        }
      }

      dynamic "login" {
        for_each = try(auth_settings_v2.value.login, null) == null ? [] : [auth_settings_v2.value.login]

        content {
          token_store_enabled = lookup(login.value, "token_store_enabled", null)
          logout_endpoint     = lookup(login.value, "logout_endpoint", null)
        }
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
