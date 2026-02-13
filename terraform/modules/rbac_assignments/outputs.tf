output "role_assignment_ids" {
  description = "Map of role assignment IDs keyed by assignment name."
  value       = { for key, assignment in azurerm_role_assignment.this : key => assignment.id }
}
