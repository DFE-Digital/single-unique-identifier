locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  service_state_key = format("%s/custodians.tfstate", var.environment_id)

  web_app_descriptor = "custodians01"
  web_app_name       = format("%s%sapp-%s-%s", var.subscription_prefix, var.environment_id, var.region_short, local.web_app_descriptor)

  otel_resource_attributes = join(",", [
    "deployment.environment=${var.environment_tag}",
    "service.namespace=${var.product}",
    "cloud.region=${var.location}",
  ])
}

data "terraform_remote_state" "core" {
  backend = "azurerm"

  config = {
    resource_group_name  = local.state_rg
    storage_account_name = local.state_storage
    container_name       = local.state_container
    key                  = local.core_state_key
  }
}

module "web_app" {
  source = "../modules/linux_web_app"

  name                = local.web_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  service_plan_id     = data.terraform_remote_state.core.outputs.app_service_plan_id

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.webapp_dotnet_version
  app_settings = merge(
    {
      APPLICATIONINSIGHTS_CONNECTION_STRING = data.terraform_remote_state.core.outputs.app_insights_connection_string
      OTEL_RESOURCE_ATTRIBUTES = local.otel_resource_attributes
      
      FindApi__BaseUrl = "https://s270d01func-ukw-find01.azurewebsites.net"
    },
    var.app_settings,
  )
  tags           = var.tags
}
