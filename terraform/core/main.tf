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
  container_registry_name = lower(format(
    "%s%sacr%sservices01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  ))
  container_app_environment_name = format(
    "%s%scae-%s-services01",
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
  aux_asp_name = format(
    "%s%sasp-%s-auxsvcs01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short
  )
  function_app_integration_vnet_name = format(
    "%s%svnet-%s-funcint01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
  function_app_integration_subnet_name = "snet-function-app-integration"
  function_app_integration_nsg_name = format(
    "%s%snsg-%s-funcint01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
  container_apps_infrastructure_subnet_name = "snet-container-apps-infrastructure"
  container_apps_infrastructure_nsg_name = format(
    "%s%snsg-%s-caeinfra01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
  private_endpoints_subnet_name = "snet-private-endpoints"
  private_endpoints_nsg_name = format(
    "%s%snsg-%s-privateep01",
    var.subscription_prefix,
    var.environment_id,
    var.region_short,
  )
}

data "azurerm_client_config" "current" {}

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

resource "azurerm_service_plan" "auxiliary" {
  count = var.use_auxiliary_asp ? 1 : 0

  name                = local.aux_asp_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  os_type             = var.auxiliary_app_service_plan_os_type
  sku_name            = var.auxiliary_app_service_plan_sku
  worker_count        = var.auxiliary_app_service_plan_worker_count

  tags = local.base_tags
}

resource "azurerm_virtual_network" "function_app_integration" {
  name                = local.function_app_integration_vnet_name
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  address_space       = var.function_app_integration_vnet_address_space

  subnet {
    name              = local.function_app_integration_subnet_name
    address_prefixes  = var.function_app_integration_subnet_address_prefixes
    security_group    = azurerm_network_security_group.function_app_integration.id
    service_endpoints = ["Microsoft.Storage"]

    delegation {
      name = "app-service-delegation"

      service_delegation {
        name    = "Microsoft.Web/serverFarms"
        actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      }
    }
  }

  tags = local.base_tags

  # The Function App integration subnet is created inline with the VNet. The
  # Container Apps and private endpoint subnets are managed as separate ARM
  # child resources so their NSGs are present at creation time. Do not let the
  # VNet resource attempt to reconcile or delete those child subnets.
  lifecycle {
    ignore_changes = [subnet]
  }
}

resource "azurerm_network_security_group" "function_app_integration" {
  name                = local.function_app_integration_nsg_name
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

# A workload profiles environment is required for the scheduled job to reach
# Table Storage through its private endpoint. This subnet is dedicated to the
# Container Apps control plane and must not host other resources.
resource "azurerm_network_security_group" "container_apps_infrastructure" {
  name                = local.container_apps_infrastructure_nsg_name
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azapi_resource" "container_apps_infrastructure_subnet" {
  type      = "Microsoft.Network/virtualNetworks/subnets@2022-07-01"
  name      = local.container_apps_infrastructure_subnet_name
  parent_id = azurerm_virtual_network.function_app_integration.id

  body = {
    properties = {
      addressPrefix = "10.250.0.64/27"
      delegations = [
        {
          name = "container-apps-environments-delegation"
          properties = {
            serviceName = "Microsoft.App/environments"
          }
        },
      ]
      networkSecurityGroup = {
        id = azurerm_network_security_group.container_apps_infrastructure.id
      }
    }
  }
}

# Private endpoints are kept in their own subnet so that service resources can
# remain inaccessible from public networks.
resource "azurerm_network_security_group" "private_endpoints" {
  name                = local.private_endpoints_nsg_name
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name

  tags = local.base_tags
}

resource "azapi_resource" "private_endpoints_subnet" {
  type      = "Microsoft.Network/virtualNetworks/subnets@2022-07-01"
  name      = local.private_endpoints_subnet_name
  parent_id = azurerm_virtual_network.function_app_integration.id

  body = {
    properties = {
      addressPrefix                  = "10.250.0.96/27"
      privateEndpointNetworkPolicies = "Disabled"
      networkSecurityGroup = {
        id = azurerm_network_security_group.private_endpoints.id
      }
    }
  }
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

resource "azurerm_container_registry" "shared" {
  name                          = local.container_registry_name
  resource_group_name           = module.resource_group.name
  location                      = module.resource_group.location
  sku                           = var.container_registry_sku
  admin_enabled                 = false
  anonymous_pull_enabled        = false
  public_network_access_enabled = true

  tags = local.base_tags
}

resource "azurerm_role_assignment" "terraform_operator_acr_push" {
  scope                = azurerm_container_registry.shared.id
  role_definition_name = "AcrPush"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_container_app_environment" "shared" {
  name                           = local.container_app_environment_name
  resource_group_name            = module.resource_group.name
  location                       = module.resource_group.location
  log_analytics_workspace_id     = azurerm_log_analytics_workspace.shared.id
  infrastructure_subnet_id       = azapi_resource.container_apps_infrastructure_subnet.id
  internal_load_balancer_enabled = true

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
    minimum_count         = 0
    maximum_count         = 3
  }

  tags = local.base_tags

  # Both subnets modify the same VNet. Completing them first prevents Azure
  # from rejecting the private endpoint subnet with AnotherOperationInProgress.
  depends_on = [azapi_resource.private_endpoints_subnet]
}
