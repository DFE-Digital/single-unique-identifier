output "resource_group_name" {
  value       = module.resource_group.name
  description = "Name of the resource group created for this environment."
}

output "resource_group_location" {
  value       = module.resource_group.location
  description = "Location of the resource group created for this environment."
}

output "app_service_plan_id" {
  value       = azurerm_service_plan.shared.id
  description = "ID of the shared App Service plan."
}

output "app_service_plan_name" {
  value       = azurerm_service_plan.shared.name
  description = "Name of the shared App Service plan."
}

output "app_service_plan_os_type" {
  value       = azurerm_service_plan.shared.os_type
  description = "OS type of the shared App Service plan."
}

output "app_service_plan_sku" {
  value       = azurerm_service_plan.shared.sku_name
  description = "SKU of the shared App Service plan."
}

output "log_analytics_workspace_id" {
  value       = azurerm_log_analytics_workspace.shared.id
  description = "ID of the shared Log Analytics workspace."
}

output "app_insights_connection_string" {
  value       = azurerm_application_insights.shared.connection_string
  description = "Connection string for the shared Application Insights instance."
  sensitive   = true
}
