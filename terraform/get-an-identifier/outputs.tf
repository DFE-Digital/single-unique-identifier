output "service_state_key" {
  value       = local.service_state_key
  description = "State key for the GetAnIdentifier service."
}

output "function_app_name" {
  value       = module.function_app.name
  description = "Name of the GetAnIdentifier function app."
}

output "function_app_id" {
  value       = module.function_app.id
  description = "ID of the GetAnIdentifier function app."
}

output "function_app_default_hostname" {
  value       = module.function_app.default_hostname
  description = "Default hostname of the GetAnIdentifier function app."
}

output "key_vault_name" {
  value       = module.key_vault.name
  description = "Name of the Key Vault."
}

output "key_vault_id" {
  value       = module.key_vault.id
  description = "ID of the Key Vault."
}