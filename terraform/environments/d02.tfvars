subscription_prefix           = "s270"
environment_id                = "d02"
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
use_stub_custodians           = true
key_vault_use_rbac            = false

find_app_settings = {
  NhsAuthConfig__NHS_DIGITAL_TOKEN_URL                       = "https://int.api.service.nhs.uk/oauth2/token"
  NhsAuthConfig__NHS_DIGITAL_FHIR_ENDPOINT                   = "https://int.api.service.nhs.uk/personal-demographics/FHIR/R4/"
  NhsAuthConfig__NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES = 5
  IdEncryption__EnablePersonIdEncryption                     = false
}

tags = {
  # Additional tags can be added here
  # Environment, Product, and Service Offering will be added by default
}
