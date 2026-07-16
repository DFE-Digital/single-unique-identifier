subscription_prefix           = "s270"
environment_id                = "d03"
environment_tag               = "Dev"
region_short                  = "ukw"
descriptor                    = "dev"
location                      = "ukwest"
product                       = "MAIS Pilot 3"
service_offering              = "MAIS Pilot 3"
app_service_plan_sku          = "B1"
app_service_plan_os_type      = "Linux"
app_service_plan_worker_count = 1
function_dotnet_version       = "10.0"
webapp_dotnet_version         = "10.0"
use_auth_emulator             = false
use_ui_test_harness           = true
use_auxiliary_asp                       = true
auxiliary_app_service_plan_sku          = "B1"
auxiliary_app_service_plan_os_type      = "Linux"
auxiliary_app_service_plan_worker_count = 1
key_vault_use_rbac            = false

tags = {
  # Additional tags can be added here
  # Environment, Product, and Service Offering will be added by default
}
