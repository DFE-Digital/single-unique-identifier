locals {
  base_tags = merge(
    {
      ManagedBy = "terraform"
    },
    var.environment_tag == null ? {} : { Environment = var.environment_tag },
    var.product == null ? {} : { Product = var.product },
    var.service_offering == null ? {} : { Service_Offering = var.service_offering },
  )
}

resource "azurerm_storage_account" "this" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.account_tier
  account_replication_type = var.account_replication_type

  allow_nested_items_to_be_public = var.allow_nested_items_to_be_public

  tags = merge(
    local.base_tags,
    var.tags,
  )
}
