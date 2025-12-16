output "name" {
  value       = azurerm_linux_web_app.this.name
  description = "Name of the Linux web app."
}

output "id" {
  value       = azurerm_linux_web_app.this.id
  description = "ID of the Linux web app."
}

output "default_hostname" {
  value       = azurerm_linux_web_app.this.default_hostname
  description = "Default hostname of the Linux web app."
}

output "principal_id" {
  value       = try(azurerm_linux_web_app.this.identity[0].principal_id, null)
  description = "Principal ID of the system-assigned managed identity, if enabled."
}

