module "app" {
  source = "./app"

  # The Toggle Switch
  count  = var.use_ui_test_harness ? 1 : 0

  # Passing all variables down
  subscription_prefix           = var.subscription_prefix
  environment_id                = var.environment_id
  environment_tag               = var.environment_tag
  region_short                  = var.region_short
  descriptor                    = var.descriptor
  location                      = var.location
  product                       = var.product
  service_offering              = var.service_offering
  tags                          = var.tags
  key_vault_use_rbac            = var.key_vault_use_rbac
  ui_harness_app_settings       = var.ui_harness_app_settings
  webapp_dotnet_version         = var.webapp_dotnet_version
  app_service_plan_sku          = var.app_service_plan_sku
  app_service_plan_os_type      = var.app_service_plan_os_type
  app_service_plan_worker_count = var.app_service_plan_worker_count
  ui_test_harness_password      = var.ui_test_harness_password
  AuthSettings_AccessTokenUrl   = var.AuthSettings_AccessTokenUrl
  AuthClientIdsJsonMap          = var.AuthClientIdsJsonMap
  AuthClientSecretsJsonMap      = var.AuthClientSecretsJsonMap
}