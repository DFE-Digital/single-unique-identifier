resource "azurerm_private_dns_zone" "acr" {
  name                = "privatelink.azurecr.io"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "acr" {
  name                 = "acr"
  private_dns_zone_id  = azurerm_private_dns_zone.acr.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "key_vault" {
  name                 = "key-vault"
  private_dns_zone_id  = azurerm_private_dns_zone.key_vault.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                 = "blob"
  private_dns_zone_id  = azurerm_private_dns_zone.blob.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "file" {
  name                = "privatelink.file.core.windows.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "file" {
  name                 = "file"
  private_dns_zone_id  = azurerm_private_dns_zone.file.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "queue" {
  name                = "privatelink.queue.core.windows.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "queue" {
  name                 = "queue"
  private_dns_zone_id  = azurerm_private_dns_zone.queue.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "table" {
  name                = "privatelink.table.core.windows.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "table" {
  name                 = "table"
  private_dns_zone_id  = azurerm_private_dns_zone.table.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

# Azure Monitor Private Link Scope (AMPLS) DNS Zones
resource "azurerm_private_dns_zone" "monitor" {
  name                = "privatelink.monitor.azure.com"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "monitor" {
  name                 = "monitor"
  private_dns_zone_id  = azurerm_private_dns_zone.monitor.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "oms" {
  name                = "privatelink.oms.opinsights.azure.com"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "oms" {
  name                 = "oms"
  private_dns_zone_id  = azurerm_private_dns_zone.oms.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "ods" {
  name                = "privatelink.ods.opinsights.azure.com"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "ods" {
  name                 = "ods"
  private_dns_zone_id  = azurerm_private_dns_zone.ods.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}

resource "azurerm_private_dns_zone" "agentsvc" {
  name                = "privatelink.agentsvc.azure-automation.net"
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "agentsvc" {
  name                 = "agentsvc"
  private_dns_zone_id  = azurerm_private_dns_zone.agentsvc.id
  virtual_network_id   = azurerm_virtual_network.function_app_integration.id
  registration_enabled = false

  tags = local.base_tags
}
