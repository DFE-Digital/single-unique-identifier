locals {
  secret_tags = merge(
    {
      Environment        = var.environment_tag
      Product            = var.product
      "Service Offering" = var.service_offering
      ManagedBy          = "terraform"
    },
    var.tags,
  )
  
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

  # Key Vault name
  key_vault_descriptor = "notifkv01"
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

resource "azurerm_key_vault_secret" "nhs_mesh_config_shared_key" {
  name         = "nhs-digital-private-key"
  value        = var.nhs_mesh_config_shared_key
  key_vault_id = module.key_vault.id
  content_type = "text/plain"
  tags         = local.secret_tags
  depends_on = [
    module.rbac_assignments_terraform_operator,
    azurerm_key_vault_access_policy.terraform_operator
  ]
}

resource "azurerm_key_vault_secret" "nhs_mesh_config_mailbox_id" {
  name         = "nhs-digital-kid"
  value        = var.nhs_mesh_config_mailbox_id
  key_vault_id = module.key_vault.id
  content_type = "text/plain"
  tags         = local.secret_tags
  depends_on = [
    module.rbac_assignments_terraform_operator,
    azurerm_key_vault_access_policy.terraform_operator
  ]
}

resource "azurerm_key_vault_secret" "nhs_mesh_config_mailbox_password" {
  name         = "nhs-digital-client-id"
  value        = var.nhs_mesh_config_mailbox_password
  key_vault_id = module.key_vault.id
  content_type = "text/plain"
  tags         = local.secret_tags
  depends_on = [
    module.rbac_assignments_terraform_operator,
    azurerm_key_vault_access_policy.terraform_operator
  ]
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
  public_network_access             = "Enabled"
  min_tls_version                   = "TLS1_2"

  tags = local.tags
}

resource "azurerm_storage_account_queue_properties" "notification_service" {
  storage_account_id = azurerm_storage_account.notification_service.id

  logging {
    delete                = true
    read                  = true
    write                 = true
    version               = "1.0"
    retention_policy_days = 30
  }

  # Keep Queue data-plane operations behind the configured default-deny
  # storage firewall.
  depends_on = [azurerm_storage_account_network_rules.notification_service]
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
  name                 = local.private_dns_link_name
  private_dns_zone_id  = azurerm_private_dns_zone.storage_table.id
  virtual_network_id   = data.terraform_remote_state.core.outputs.function_app_integration_vnet_id
  registration_enabled = false

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
  # Keep core infrastructure deployable while role-assignment delegation is pending.
  count = var.notification_service_acr_pull_enabled ? 1 : 0

  scope                = data.terraform_remote_state.core.outputs.container_registry_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.notification_service.principal_id
}

resource "azurerm_role_assignment" "notification_service_table_data_contributor" {
  # Keep core infrastructure deployable while role-assignment delegation is pending.
  count = var.notification_service_acr_pull_enabled ? 1 : 0

  scope                = azurerm_storage_account.notification_service.id
  role_definition_name = "Storage Table Data Contributor"
  principal_id         = azurerm_user_assigned_identity.notification_service.principal_id
}

resource "azurerm_container_app_job" "notification_service" {
  # Keep core infrastructure deployable while role-assignment delegation is pending.
  count = var.notification_service_job_enabled ? 1 : 0

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

  secret {
    name  = "nhs-mesh-shared-key"
    value = var.nhs_mesh_shared_key
  }

  secret {
    name  = "nhs-mesh-mailbox-id"
    value = var.nhs_mesh_mailbox_id
  }

  secret {
    name  = "nhs-mesh-mailbox-password"
    value = var.nhs_mesh_mailbox_password
  }

  secret {
    name  = "nhs-mesh-root-ca"
    value = var.nhs_mesh_root_ca
  }

  secret {
    name  = "nhs-mesh-intermediate-ca"
    value = var.nhs_mesh_intermediate_ca
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

      env {
        name  = "NhsMeshConfig__MailboxBaseUrl"
        value = var.nhs_mesh_mailbox_base_url
      }

      env {
        name        = "NhsMeshConfig__SharedKey"
        secret_name = "nhs-mesh-shared-key"
      }

      env {
        name        = "NhsMeshConfig__MailboxId"
        secret_name = "nhs-mesh-mailbox-id"
      }

      env {
        name        = "NhsMeshConfig__MailboxPassword"
        secret_name = "nhs-mesh-mailbox-password"
      }

      env {
        name        = "NhsMeshConfig__MailboxRootCA"
        secret_name = "nhs-mesh-root-ca"
      }

      env {
        name        = "NhsMeshConfig__MailboxIntermediateCA"
        secret_name = "nhs-mesh-intermediate-ca"
      }

      # appsettings.Development.json enables this for the local sandbox; it must never be
      # true against a deployed MESH endpoint (the app rejects it for non-loopback URLs).
      env {
        name  = "NhsMeshConfig__AcceptLocalDevCert"
        value = "false"
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
