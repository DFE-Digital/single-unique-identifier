module "resource_group" {
  source = "../modules/resource_group"

  subscription_prefix = var.subscription_prefix
  environment_id      = var.environment_id
  environment_tag     = var.environment_tag
  region_short        = var.region_short
  descriptor          = var.descriptor
  location            = var.location
  product             = var.product
  service_offering    = var.service_offering
  tags                = var.tags
}

resource "azurerm_service_plan" "shared" {
  name                = format("%s%sasp-%s-services01", var.subscription_prefix, var.environment_id, var.region_short)
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  os_type             = var.app_service_plan_os_type
  sku_name            = var.app_service_plan_sku
  worker_count        = var.app_service_plan_worker_count

  tags = merge(
    {
      Environment      = var.environment_tag
      Product          = var.product
      Service_Offering = var.service_offering
      ManagedBy        = "terraform"
    },
    var.tags,
  )
}
