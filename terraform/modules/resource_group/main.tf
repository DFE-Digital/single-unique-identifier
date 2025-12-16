locals {
  name = format(
    "%s%srg-%s-%s",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
    var.descriptor,
  )
}

resource "azurerm_resource_group" "this" {
  name     = local.name
  location = var.location

  tags = merge(
    {
      Environment      = var.environment_tag
      ManagedBy        = "terraform"
      Product          = var.product
      Service_Offering = var.service_offering
    },
    var.tags,
  )
}
