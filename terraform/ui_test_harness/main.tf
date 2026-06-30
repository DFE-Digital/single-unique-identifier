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
  FindApiGatewayBaseUrl         = var.FindApiGatewayBaseUrl
  FindApiGatewayAuthScope       = var.FindApiGatewayAuthScope
}

# Moving top-level resources down into the conditional 'app' sub-module to prevent destroy/recreate during refactoring.

moved {
  from = module.key_vault
  to   = module.app[0].module.key_vault
}

moved {
  from = module.rbac_assignments_terraform_operator
  to   = module.app[0].module.rbac_assignments_terraform_operator
}

moved {
  from = azurerm_key_vault_access_policy.terraform_operator
  to   = module.app[0].azurerm_key_vault_access_policy.terraform_operator
}

moved {
  from = azurerm_key_vault_secret.ui_harness_password
  to   = module.app[0].azurerm_key_vault_secret.ui_harness_password
}

moved {
  from = module.web_app
  to   = module.app[0].module.web_app
}

moved {
  from = module.rbac_assignments_ui_harness_app
  to   = module.app[0].module.rbac_assignments_ui_harness_app
}

moved {
  from = azurerm_key_vault_access_policy.ui_harness_app
  to   = module.app[0].azurerm_key_vault_access_policy.ui_harness_app
}

moved {
  from = module.rbac_assignments_find_kv
  to   = module.app[0].module.rbac_assignments_find_kv
}

moved {
  from = azurerm_key_vault_access_policy.find_kv
  to   = module.app[0].azurerm_key_vault_access_policy.find_kv
}