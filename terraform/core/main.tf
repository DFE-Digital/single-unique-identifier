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

output "resource_group_name" {
  value       = module.resource_group.name
  description = "Name of the resource group created for this environment."
}

output "resource_group_location" {
  value       = module.resource_group.location
  description = "Location of the resource group created for this environment."
}
