locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  service_state_key = format("%s/notification-service.tfstate", var.environment_id)

  name_prefix = format("%s%s", var.subscription_prefix, var.environment_id)
  storage_account_name = lower(format(
    "%sstnotifications01",
    local.name_prefix,
  ))
  identity_name = format(
    "%sid-%s-notification01",
    local.name_prefix,
    var.region_short,
  )
  job_name = format(
    "%scaj-%s-notification01",
    local.name_prefix,
    var.region_short,
  )
  private_endpoint_name = format(
    "%spe-%s-notificationtable01",
    local.name_prefix,
    var.region_short,
  )
  private_dns_link_name = format(
    "%spdnl-%s-notificationtable01",
    local.name_prefix,
    var.region_short,
  )
  image = format(
    "%s/%s:%s",
    data.terraform_remote_state.core.outputs.container_registry_login_server,
    var.notification_service_image_name,
    var.notification_service_image_tag,
  )
  tags = merge(
    {
      Environment        = var.environment_tag
      Product            = var.product
      "Service Offering" = var.service_offering
      ManagedBy          = "terraform"
    },
    var.tags,
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

resource "azurerm_user_assigned_identity" "notification_service" {
  name                = local.identity_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location

  tags = local.tags
}

# The register contains non-production test configuration and has no agreed
# disaster-recovery requirement. Geo-replication would add ongoing cost.
#trivy:ignore:AZU-0058
resource "azurerm_storage_account" "notification_service" {
  name                              = local.storage_account_name
  resource_group_name               = data.terraform_remote_state.core.outputs.resource_group_name
  location                          = data.terraform_remote_state.core.outputs.resource_group_location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  infrastructure_encryption_enabled = true
  allow_nested_items_to_be_public   = false
  shared_access_key_enabled         = false
  default_to_oauth_authentication   = true
  public_network_access_enabled     = true
  min_tls_version                   = "TLS1_2"

  queue_properties {
    logging {
      delete                = true
      read                  = true
      write                 = true
      version               = "1.0"
      retention_policy_days = 30
    }
  }

  tags = local.tags
}

# The table is created through ARM, rather than the Storage data plane, so the
# deployment remains possible after public network access is denied.
resource "azapi_resource" "supplier_webhooks" {
  type      = "Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01"
  name      = "SupplierWebhooks"
  parent_id = "${azurerm_storage_account.notification_service.id}/tableServices/default"
  body      = {}
}

resource "azurerm_private_dns_zone" "storage_table" {
  name                = "privatelink.table.core.windows.net"
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  tags                = local.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "storage_table" {
  name                  = local.private_dns_link_name
  resource_group_name   = data.terraform_remote_state.core.outputs.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.storage_table.name
  virtual_network_id    = data.terraform_remote_state.core.outputs.function_app_integration_vnet_id
  registration_enabled  = false

  tags = local.tags
}

resource "azurerm_private_endpoint" "storage_table" {
  name                = local.private_endpoint_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  subnet_id           = data.terraform_remote_state.core.outputs.private_endpoints_subnet_id

  private_service_connection {
    name                           = "storage-table"
    private_connection_resource_id = azurerm_storage_account.notification_service.id
    subresource_names              = ["table"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "storage-table"
    private_dns_zone_ids = [azurerm_private_dns_zone.storage_table.id]
  }

  tags = local.tags
}

resource "azurerm_storage_account_network_rules" "notification_service" {
  storage_account_id = azurerm_storage_account.notification_service.id
  default_action     = "Deny"
  bypass             = ["AzureServices"]

  # ARM table provisioning and the private endpoint must be ready before the
  # public firewall is closed.
  depends_on = [
    azapi_resource.supplier_webhooks,
    azurerm_private_endpoint.storage_table,
  ]
}

resource "azurerm_role_assignment" "notification_service_acr_pull" {
  scope                = data.terraform_remote_state.core.outputs.container_registry_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.notification_service.principal_id
}

resource "azurerm_role_assignment" "notification_service_table_data_contributor" {
  scope                = azurerm_storage_account.notification_service.id
  role_definition_name = "Storage Table Data Contributor"
  principal_id         = azurerm_user_assigned_identity.notification_service.principal_id
}

resource "azurerm_container_app_job" "notification_service" {
  name                         = local.job_name
  resource_group_name          = data.terraform_remote_state.core.outputs.resource_group_name
  location                     = data.terraform_remote_state.core.outputs.resource_group_location
  container_app_environment_id = data.terraform_remote_state.core.outputs.container_app_environment_id
  workload_profile_name        = "Consumption"
  replica_timeout_in_seconds   = var.notification_service_replica_timeout_in_seconds
  replica_retry_limit          = var.notification_service_replica_retry_limit

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.notification_service.id]
  }

  registry {
    server   = data.terraform_remote_state.core.outputs.container_registry_login_server
    identity = azurerm_user_assigned_identity.notification_service.id
  }

  schedule_trigger_config {
    cron_expression          = var.notification_service_schedule_cron_expression
    parallelism              = 1
    replica_completion_count = 1
  }

  template {
    container {
      name   = "notification-service"
      image  = local.image
      cpu    = var.notification_service_cpu
      memory = var.notification_service_memory

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = var.notification_service_dotnet_environment
      }

      env {
        name  = "TableStorage__ServiceUri"
        value = azurerm_storage_account.notification_service.primary_table_endpoint
      }
    }
  }

  tags = local.tags

  depends_on = [
    azurerm_role_assignment.notification_service_acr_pull,
    azurerm_role_assignment.notification_service_table_data_contributor,
    azurerm_storage_account_network_rules.notification_service,
  ]
}
