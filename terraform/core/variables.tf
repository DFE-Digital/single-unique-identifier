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

variable "location" {
  description = "Azure location for the resource group (for example uksouth)."
  type        = string
}

variable "region_short" {
  description = "Short name for the Azure region (for example uks for UK South)."
  type        = string
}

variable "descriptor" {
  description = "Default component descriptor used for resource naming when a more specific descriptor variable is not provided (for example dev). Component instance suffix (e.g. 01) is optional."
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
  description = "Additional tags to apply to the resource group."
  type        = map(string)
  default     = {}
}

variable "app_service_plan_sku" {
  description = "SKU name for the shared App Service plan (e.g. B1, P1v2, P1v3)."
  type        = string
}

variable "app_service_plan_os_type" {
  description = "OS type for the App Service plan."
  type        = string
  default     = "Linux"
}

variable "app_service_plan_worker_count" {
  description = "Number of workers for the App Service plan."
  type        = number
  default     = 1
}

variable "log_analytics_sku" {
  description = "SKU for the Log Analytics workspace."
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_in_days" {
  description = "Retention period for Log Analytics workspace data."
  type        = number
  default     = 30
}

variable "function_app_integration_vnet_address_space" {
  description = "Address space for the dedicated VNet used by App Service regional VNet integration."
  type        = list(string)
  default     = ["10.250.0.0/24"]
}

variable "function_app_integration_subnet_address_prefixes" {
  description = "Address prefixes for the delegated subnet used by App Service regional VNet integration."
  type        = list(string)
  default     = ["10.250.0.0/26"]
}
