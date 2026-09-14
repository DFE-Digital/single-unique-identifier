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

resource "azurerm_storage_account" "notification_service" {
  name                            = local.storage_account_name
  resource_group_name             = data.terraform_remote_state.core.outputs.resource_group_name
  location                        = data.terraform_remote_state.core.outputs.resource_group_location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  allow_nested_items_to_be_public = false
  # azurerm_storage_table requires Shared Key while the application itself uses managed identity.
  shared_access_key_enabled       = true
  default_to_oauth_authentication = true
  public_network_access_enabled   = true
  min_tls_version                 = "TLS1_2"

  tags = local.tags
}

resource "azurerm_storage_table" "supplier_webhooks" {
  name                 = "SupplierWebhooks"
  storage_account_name = azurerm_storage_account.notification_service.name
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
  ]
}
