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

resource "azurerm_key_vault" "this" {
  name                        = var.name
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = var.tenant_id
    object_id = var.object_id

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

  tags = merge(
    local.base_tags,
    var.tags,
  )
}
