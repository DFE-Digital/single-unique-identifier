locals {
  state_rg        = format("%s%srg-%s-tfstate", var.subscription_prefix, var.environment_id, var.region_short)
  state_storage   = format("%s%ssttfstate01", var.subscription_prefix, var.environment_id)
  state_container = "tfstate"

  core_state_key    = format("%s/terraform.tfstate", var.environment_id)
  service_state_key = format("%s/authemulator.tfstate", var.environment_id)

  web_app_descriptor = "authemulator01"
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
  count   = var.use_auth_emulator ? 1 : 0
  source = "../modules/linux_web_app"

  name                = local.web_app_name
  resource_group_name = data.terraform_remote_state.core.outputs.resource_group_name
  location            = data.terraform_remote_state.core.outputs.resource_group_location
  
  # UPDATED: Use auxiliary plan if it exists, otherwise fall back to the shared plan
  service_plan_id     = data.terraform_remote_state.core.outputs.auxiliary_app_service_plan_id != null ? data.terraform_remote_state.core.outputs.auxiliary_app_service_plan_id : data.terraform_remote_state.core.outputs.app_service_plan_id

  environment_tag  = var.environment_tag
  product          = var.product
  service_offering = var.service_offering

  dotnet_version = var.webapp_dotnet_version
  app_settings = merge(
    {
      APPLICATIONINSIGHTS_CONNECTION_STRING = data.terraform_remote_state.core.outputs.app_insights_connection_string
      OTEL_RESOURCE_ATTRIBUTES = local.otel_resource_attributes

      AuthSettings__Issuer   = var.AuthSettings_Issuer
      AuthSettings__Audience = var.AuthSettings_Audience

      # Map the environment's dynamic base URL down to the Auth Emulator config
      AuthSettings__BaseUrl = format("https://%s%sapp-%s-authemulator01.azurewebsites.net/", var.subscription_prefix, var.environment_id, var.region_short)
    },
    merge(
      #{
      #  # Provided for debugging and ease of updating, because this value can be retrieved from the app settings in the Azure Portal:
      #  AuthClientCredentialsMapRaw = sensitive(var.AuthClientCredentialsMap),
      #},
      [
        for client in var.AuthClientCredentialsMap : {
          "AuthClientCredentials__${client.clientId}__NewClientId"     = client.newClientId
          "AuthClientCredentials__${client.clientId}__NewClientSecret" = sensitive(client.newClientSecret)
        }
      ]...),
    var.authemulator_app_settings,
  )
  tags           = var.tags
}

moved {
  from = module.web_app
  to   = module.web_app[0]
}
