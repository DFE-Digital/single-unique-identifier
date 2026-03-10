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

variable "app_service_integration_subnet_id" {
  description = "Optional subnet ID used for regional VNet integration and storage account network rules."
  type        = string
  default     = null
}

variable "log_analytics_workspace_id" {
  description = "Optional Log Analytics workspace ID used for storage account diagnostics."
  type        = string
  default     = null
}

variable "storage_account_replication_type" {
  description = "Replication type for the storage account used by the function app."
  type        = string
  default     = "LRS"

  validation {
    condition = contains(["LRS", "GRS", "RAGRS", "GZRS", "RAGZRS"], var.storage_account_replication_type)
    error_message = "storage_account_replication_type must be one of LRS, GRS, RAGRS, GZRS, or RAGZRS."
  }
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

variable "health_check_path" {
  description = "The URI path to the endpoint to use for health checks. Should return 200 OK if the API is up and healthy."
  type        = string
  default     = "/api/health"
}
