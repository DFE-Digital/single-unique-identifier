output "id" {
  description = "The ID of the Key Vault"
  value       = azurerm_key_vault.this.id
}

output "name" {
  description = "The Name of the Key Vault"
  value       = azurerm_key_vault.this.name
}

output "vault_uri" {
  description = "The URI of the Key Vault"
  value       = azurerm_key_vault.this.vault_uri
}

output "private_endpoint_id" {
  description = "The ID of the Key Vault Private Endpoint, if created."
  value       = one(azurerm_private_endpoint.this[*].id)
}
