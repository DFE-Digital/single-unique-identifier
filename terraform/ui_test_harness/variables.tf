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
  description = "Free text descriptor for the resource group (for example dev)."
  type        = string
}

variable "location" {
  description = "Azure location for the resource group."
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
  description = "Tags to propagate to service resources."
  type        = map(string)
  default     = {}
}

variable "key_vault_use_rbac" {
  description = "Determines whether to use Azure RBAC for Key Vault access."
  type        = bool
  default     = true
}

variable "ui_harness_app_settings" {
  description = "Additional app settings to apply to the UI Test Harness web app."
  type        = map(string)
  default     = {}
}

variable "webapp_dotnet_version" {
  description = "Dotnet version for the web app stack."
  type        = string
  default     = "10.0"
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

variable "ui_test_harness_password" {
  description = "Password for the UI Test Harness injected from GitHub Secrets."
  type        = string
  sensitive   = true
}