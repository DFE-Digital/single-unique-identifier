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
  purge_protection_enabled    = true
  rbac_authorization_enabled  = var.rbac_authorization_enabled

  sku_name = "standard"

  tags = merge(
    local.base_tags,
    var.tags,
  )
}
