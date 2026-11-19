# ACR Private Endpoint
resource "azurerm_private_endpoint" "acr" {
  name                = format("%s%spe-%s-acr01", var.subscription_prefix, var.environment_id, var.region_short)
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = azapi_resource.private_endpoints_subnet.id

  private_service_connection {
    name                           = "acr"
    private_connection_resource_id = azurerm_container_registry.shared.id
    subresource_names              = ["registry"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "acr"
    private_dns_zone_ids = [azurerm_private_dns_zone.acr.id]
  }

  tags = local.base_tags
}

# Azure Monitor Private Link Scope
resource "azurerm_monitor_private_link_scope" "shared" {
  name                = format("%s%sampls-%s-services01", var.subscription_prefix, var.environment_id, var.region_short)
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_monitor_private_link_scoped_service" "log_analytics" {
  name                = "log-analytics"
  resource_group_name = module.resource_group.name
  scope_name          = azurerm_monitor_private_link_scope.shared.name
  linked_resource_id  = azurerm_log_analytics_workspace.shared.id
}

resource "azurerm_monitor_private_link_scoped_service" "app_insights" {
  name                = "app-insights"
  resource_group_name = module.resource_group.name
  scope_name          = azurerm_monitor_private_link_scope.shared.name
  linked_resource_id  = azurerm_application_insights.shared.id
}

resource "azurerm_private_endpoint" "ampls" {
  name                = format("%s%spe-%s-ampls01", var.subscription_prefix, var.environment_id, var.region_short)
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id           = azapi_resource.private_endpoints_subnet.id

  private_service_connection {
    name                           = "ampls"
    private_connection_resource_id = azurerm_monitor_private_link_scope.shared.id
    subresource_names              = ["azuremonitor"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name = "ampls"
    private_dns_zone_ids = [
      azurerm_private_dns_zone.monitor.id,
      azurerm_private_dns_zone.oms.id,
      azurerm_private_dns_zone.ods.id,
      azurerm_private_dns_zone.agentsvc.id,
      azurerm_private_dns_zone.blob.id,
    ]
  }

  tags = local.base_tags
}
