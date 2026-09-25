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

output "function_app_integration_subnet_id" {
  value       = format("%s/subnets/%s", azurerm_virtual_network.function_app_integration.id, local.function_app_integration_subnet_name)
  description = "ID of the delegated subnet used for Function App regional VNet integration."
}

output "log_analytics_workspace_id" {
  value       = azurerm_log_analytics_workspace.shared.id
  description = "ID of the shared Log Analytics workspace."
}

output "log_analytics_workspace_name" {
  value       = azurerm_log_analytics_workspace.shared.name
  description = "Name of the shared Log Analytics workspace."
}

output "container_registry_id" {
  value       = azurerm_container_registry.shared.id
  description = "ID of the shared Azure Container Registry."
}

output "container_registry_name" {
  value       = azurerm_container_registry.shared.name
  description = "Name of the shared Azure Container Registry."
}

output "container_registry_login_server" {
  value       = azurerm_container_registry.shared.login_server
  description = "Login server of the shared Azure Container Registry."
}

output "container_app_environment_id" {
  value       = azurerm_container_app_environment.shared.id
  description = "ID of the shared Azure Container Apps environment."
}

output "private_endpoints_subnet_id" {
  value       = azapi_resource.private_endpoints_subnet.id
  description = "ID of the subnet reserved for private endpoints."
}

output "function_app_integration_vnet_id" {
  value       = azurerm_virtual_network.function_app_integration.id
  description = "ID of the VNet shared by Function App integration, Container Apps and private endpoints."
}

output "app_insights_connection_string" {
  value       = azurerm_application_insights.shared.connection_string
  description = "Connection string for the shared Application Insights instance."
  sensitive   = true
}

output "auxiliary_app_service_plan_id" {
  value       = one(azurerm_service_plan.auxiliary[*].id)
  description = "ID of the auxiliary App Service plan, if created."
}
