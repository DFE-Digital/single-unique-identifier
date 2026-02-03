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

output "audit_processor_function_app_name" {
  value       = module.audit_processor_function_app.name
  description = "Name of the AuditProcessor function app."
}

output "audit_processor_function_app_id" {
  value       = module.audit_processor_function_app.id
  description = "ID of the AuditProcessor function app."
}

output "key_vault_name" {
  value       = module.key_vault.id
  description = "ID of the Key Vault."
}

output "api_key_secret_name" {
  value       = azurerm_key_vault_secret.find_match_api_key.name
  description = "Name of the API key secret in Key Vault."
}
