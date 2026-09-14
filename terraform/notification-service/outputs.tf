output "service_state_key" {
  value       = local.service_state_key
  description = "State key for the Notification Service."
}

output "notification_service_job_name" {
  value       = azurerm_container_app_job.notification_service.name
  description = "Name of the Notification Service Container Apps job."
}

output "notification_service_job_id" {
  value       = azurerm_container_app_job.notification_service.id
  description = "ID of the Notification Service Container Apps job."
}

output "resource_group_name" {
  value       = data.terraform_remote_state.core.outputs.resource_group_name
  description = "Resource group containing the Notification Service job."
}

output "log_analytics_workspace_id" {
  value       = data.terraform_remote_state.core.outputs.log_analytics_workspace_id
  description = "ID of the shared Log Analytics workspace."
}

output "log_analytics_workspace_name" {
  value       = data.terraform_remote_state.core.outputs.log_analytics_workspace_name
  description = "Name of the shared Log Analytics workspace."
}
