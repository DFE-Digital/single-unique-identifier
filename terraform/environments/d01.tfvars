subscription_prefix           = "s270"
environment_id                = "d01"
environment_tag               = "Dev"
region_short                  = "ukw"
descriptor                    = "dev"
location                      = "ukwest"
product                       = "MAIS Pilot 3"
service_offering              = "MAIS Pilot 3"
app_service_plan_sku          = "B1"
app_service_plan_os_type      = "Linux"
app_service_plan_worker_count = 1
function_dotnet_version       = "9.0"
webapp_dotnet_version         = "9.0"
use_stub_custodians           = true

tags = {
  # Additional tags can be added here
  # Environment, Product, and Service Offering will be added by default
}
