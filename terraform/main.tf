module "resource_group" {
  source = "./modules/resource_group"

  subscription_prefix = var.subscription_prefix
  environment_id      = var.environment_id
  region_short        = var.region_short
  descriptor          = var.descriptor
  location            = var.location
  tags                = var.tags
}

output "resource_group_name" {
  value       = module.resource_group.name
  description = "Name of the resource group created for this environment."
}
