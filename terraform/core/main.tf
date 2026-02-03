locals {
  base_tags = merge(
    {
      Environment        = var.environment_tag
      Product            = var.product
      "Service Offering" = var.service_offering
      ManagedBy          = "terraform"
    },
    var.tags,
  )

  log_analytics_workspace_name = format(
    "%s%slog-%s-services01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
  app_insights_name = format(
    "%s%sappi-%s-services01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
  asp_name = format(
    "%s%sasp-%s-services01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short
  )
}

module "resource_group" {
  source = "../modules/resource_group"

  subscription_prefix = var.subscription_prefix
  environment_id      = var.environment_id
  environment_tag     = var.environment_tag
  region_short        = var.region_short
  descriptor          = var.descriptor
  location            = var.location
  product             = var.product
  service_offering    = var.service_offering
  tags                = var.tags
}

resource "azurerm_service_plan" "shared" {
  name                = local.asp_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  os_type             = var.app_service_plan_os_type
  sku_name            = var.app_service_plan_sku
  worker_count        = var.app_service_plan_worker_count

  tags = local.base_tags
}

resource "azurerm_log_analytics_workspace" "shared" {
  name                = local.log_analytics_workspace_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  sku                 = var.log_analytics_sku
  retention_in_days   = var.log_analytics_retention_in_days

  tags = local.base_tags
}

resource "azurerm_application_insights" "shared" {
  name                = local.app_insights_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.shared.id

  tags = local.base_tags
}
