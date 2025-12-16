output "core_resource_group_name" {
  value       = data.terraform_remote_state.core.outputs.resource_group_name
  description = "Resource group name from the core state."
}

output "core_resource_group_location" {
  value       = data.terraform_remote_state.core.outputs.resource_group_location
  description = "Resource group location from the core state."
}

output "web_app_name" {
  value       = module.web_app.name
  description = "Name of the example service web app."
}

output "web_app_id" {
  value       = module.web_app.id
  description = "ID of the example service web app."
}

output "web_app_default_hostname" {
  value       = module.web_app.default_hostname
  description = "Default hostname of the example service web app."
}

output "service_state_key" {
  value       = local.service_state_key
  description = "Example state key for this service root."
}
