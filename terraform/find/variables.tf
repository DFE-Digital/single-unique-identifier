variable "subscription_prefix" {
  description = "Prefix that identifies the subscription (for example s270)."
  type        = string
}

variable "environment_id" {
  description = "Short identifier for the environment (for example d01)."
  type        = string
}

variable "environment_tag" {
  description = "Environment tag value (for example Dev or Test)."
  type        = string
}

variable "region_short" {
  description = "Short name for the Azure region (for example uks for UK South)."
  type        = string
}

variable "descriptor" {
  description = "Default component descriptor used for resource naming when a more specific descriptor variable is not provided."
  type        = string
}

variable "location" {
  description = "Azure location for the function app (for example uksouth)."
  type        = string
}

variable "product" {
  description = "Product tag value."
  type        = string
}

variable "service_offering" {
  description = "Service Offering tag value."
  type        = string
}

variable "tags" {
  description = "Additional tags to apply to the function app."
  type        = map(string)
  default     = {}
}

variable "find_app_settings" {
  description = "Additional app settings to apply to the Find function app."
  type        = map(string)
  default     = {}
}

variable "audit_app_settings" {
  description = "Additional app settings to apply to the Audit function app."
  type        = map(string)
  default     = {}
}

variable "function_dotnet_version" {
  description = "Dotnet version for the function app stack."
  type        = string
  default     = "9.0"
}

variable "app_service_plan_sku" {
  description = "SKU name for the shared App Service plan (present to support shared tfvars files)."
  type        = string
  default     = null
}

variable "app_service_plan_os_type" {
  description = "OS type for the App Service plan (present to support shared tfvars files)."
  type        = string
  default     = null
}

variable "app_service_plan_worker_count" {
  description = "Number of workers for the App Service plan (present to support shared tfvars files)."
  type        = number
  default     = null
}

variable "use_stub_custodians" {
  description = "Set to true to dynamically add the corresponding `StubCustodiansBaseUrl` configuration to the Find Function App (requires the StubCustodians to be deployed for the corresponding environment)."
  type        = bool
  default     = false
}
