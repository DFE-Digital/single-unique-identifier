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
