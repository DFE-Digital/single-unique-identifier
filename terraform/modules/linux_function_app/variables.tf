variable "name" {
  description = "Name of the Linux function app."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name to deploy the function app into."
  type        = string
}

variable "location" {
  description = "Azure location for the function app."
  type        = string
}

variable "service_plan_id" {
  description = "ID of the App Service plan for the function app."
  type        = string
}

variable "storage_account_name" {
  description = "Globally unique name of the storage account used by the function app."
  type        = string
}

variable "https_only" {
  description = "Whether the function app enforces HTTPS-only."
  type        = bool
  default     = true
}

variable "enable_system_assigned_identity" {
  description = "Whether to enable a system-assigned managed identity."
  type        = bool
  default     = true
}

variable "ftps_state" {
  description = "FTPS state for the function app."
  type        = string
  default     = "Disabled"
}

variable "dotnet_version" {
  description = "Dotnet version for the function app stack."
  type        = string
  default     = "10.0"
}

variable "environment_tag" {
  description = "Environment tag value (e.g. Dev/Test/Prod). Used for tags and default app settings when set."
  type        = string
  default     = null
}

variable "product" {
  description = "Product tag value."
  type        = string
  default     = null
}

variable "service_offering" {
  description = "Service Offering tag value."
  type        = string
  default     = null
}

variable "tags" {
  description = "Additional tags to apply to the function app."
  type        = map(string)
  default     = {}
}

variable "app_settings" {
  description = "Additional app settings to apply to the function app."
  type        = map(string)
  default     = {}
}

variable "application_insights_connection_string" {
  description = "Optional. Specifies the connection string that the function app should use to connect to Application Insights for telemetry and logging."
  type        = string
  default     = null
}
