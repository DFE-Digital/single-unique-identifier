locals {
  state_rg       = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage  = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  service_state_key = format("%s/service-example.tfstate", var.environment_id)
}

data "terraform_remote_state" "core" {
  backend = "azurerm"

  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.core_state_key
  }
}

# Example service module call would go here, consuming outputs from core.
# module "example_service" {
#   source              = "../modules/example_service"
#   resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
#   location            = data.terraform_remote_state.core.outputs.resource_group_location
#   tags                = var.tags
#   feature_enabled     = var.example_feature_enabled
# }

output "core_resource_group_name" {
  value       = data.terraform_remote_state.core.outputs.resource_group_name
  description = "Resource group name from the core state."
}

output "core_resource_group_location" {
  value       = data.terraform_remote_state.core.outputs.resource_group_location
  description = "Resource group location from the core state."
}

output "service_state_key" {
  value       = local.service_state_key
  description = "Example state key for this service root."
}
