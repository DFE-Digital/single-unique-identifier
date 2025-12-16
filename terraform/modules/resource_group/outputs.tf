output "name" {
  value       = azurerm_resource_group.this.name
  description = "Resource group name."
}

output "location" {
  value       = azurerm_resource_group.this.location
  description = "Resource group location."
}
