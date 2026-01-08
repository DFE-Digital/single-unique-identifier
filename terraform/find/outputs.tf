output "service_state_key" {
  value       = local.service_state_key
  description = "State key for the Find service."
}

output "function_app_name" {
  value       = module.function_app.name
  description = "Name of the Find function app."
}

output "function_app_id" {
  value       = module.function_app.id
  description = "ID of the Find function app."
}

output "function_app_default_hostname" {
  value       = module.function_app.default_hostname
  description = "Default hostname of the Find function app."
}
