locals {
  base_tags = merge(
    {
      ManagedBy = "terraform"
    },
    var.environment_tag == null ? {} : { Environment = var.environment_tag },
    var.product == null ? {} : { Product = var.product },
    var.service_offering == null ? {} : { "Service Offering" = var.service_offering },
  )

  all_tags = merge(
    local.base_tags,
    var.tags,
  )

  private_dns_zone_id = format(
    "%s/providers/Microsoft.Network/privateDnsZones/privatelink.vaultcore.azure.net",
    regex("^/subscriptions/[^/]+/resourceGroups/[^/]+", azurerm_key_vault.this.id),
  )
}

# Accepted for callers that have not yet enabled private endpoints (e.g. ui_test_harness).
# trivy:ignore:AZU-0013
resource "azurerm_key_vault" "this" {
  name                          = var.name
  location                      = var.location
  resource_group_name           = var.resource_group_name
  enabled_for_disk_encryption   = true
  tenant_id                     = var.tenant_id
  soft_delete_retention_days    = 7
  purge_protection_enabled      = true
  rbac_authorization_enabled    = var.rbac_authorization_enabled
  public_network_access_enabled = var.public_network_access_enabled

  sku_name = "standard"

  dynamic "network_acls" {
    for_each = var.private_endpoint_subnet_id != null ? [1] : []
    content {
      default_action = "Deny"
      bypass         = "AzureServices"
      ip_rules       = var.allowed_ip_rules
    }
  }

  tags = local.all_tags
}

resource "azurerm_private_endpoint" "this" {
  count = var.private_endpoint_subnet_id != null ? 1 : 0

  name                = "pe-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoint_subnet_id

  private_service_connection {
    name                           = "key-vault"
    private_connection_resource_id = azurerm_key_vault.this.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "key-vault"
    private_dns_zone_ids = [local.private_dns_zone_id]
  }

  tags = local.all_tags
}
