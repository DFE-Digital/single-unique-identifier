locals {
  base_tags = merge(
    {
      ManagedBy = "terraform"
    },
    var.environment_tag == null ? {} : { Environment = var.environment_tag },
    var.product == null ? {} : { Product = var.product },
    var.service_offering == null ? {} : { "Service Offering" = var.service_offering },
  )
}

# Accepted for now while the project is in early development and does not handle real data. This ignore must be removed and the key vault should be included in the vnet prior to handling real data.
# trivy:ignore:AZU-0013
resource "azurerm_key_vault" "this" {
  name                        = var.name
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled      = true
  rbac_authorization_enabled    = var.rbac_authorization_enabled
  public_network_access_enabled = var.public_network_access_enabled

  sku_name = "standard"

  tags = merge(
    local.base_tags,
    var.tags,
  )
}

resource "azurerm_private_endpoint" "this" {
  count = var.private_endpoints_subnet_id != null ? 1 : 0

  name                = "${var.name}-pe"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id

  private_service_connection {
    name                           = "key-vault"
    private_connection_resource_id = azurerm_key_vault.this.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }

  dynamic "private_dns_zone_group" {
    for_each = length(var.private_dns_zone_ids) > 0 ? [1] : []
    content {
      name                 = "key-vault"
      private_dns_zone_ids = var.private_dns_zone_ids
    }
  }

  tags = merge(
    local.base_tags,
    var.tags,
  )
}

