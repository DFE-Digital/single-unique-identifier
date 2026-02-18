output "name" {
  value       = azurerm_linux_function_app.this.name
  description = "Name of the Linux function app."
}

output "id" {
  value       = azurerm_linux_function_app.this.id
  description = "ID of the Linux function app."
}

output "default_hostname" {
  value       = azurerm_linux_function_app.this.default_hostname
  description = "Default hostname of the Linux function app."
}

output "principal_id" {
  value       = try(azurerm_linux_function_app.this.identity[0].principal_id, null)
  description = "Principal ID of the system-assigned managed identity, if enabled."
}

output "tenant_id" {
  value       = try(azurerm_linux_function_app.this.identity[0].tenant_id, null)
  description = "Tenant ID of the system-assigned managed identity, if enabled."
}

output "storage_connection_string" {
  value       = azurerm_storage_account.this.primary_connection_string
  description = "Primary connection string of the storage account."
  sensitive   = true
}
