variable "subscription_prefix" {
  description = "Prefix that identifies the subscription."
  type        = string
}

variable "environment_id" {
  description = "Short environment identifier."
  type        = string
}

variable "environment_tag" {
  description = "Environment tag value."
  type        = string
}

variable "region_short" {
  description = "Short Azure region name."
  type        = string
}

variable "location" {
  description = "Azure location. Present to support shared environment tfvars."
  type        = string
}

variable "descriptor" {
  description = "Environment descriptor. Present to support shared environment tfvars."
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
  description = "Additional tags to apply to Notification Service resources."
  type        = map(string)
  default     = {}
}

variable "notification_service_image_name" {
  description = "Repository name of the Notification Service image in ACR."
  type        = string
  default     = "notification-service"
}

variable "notification_service_image_tag" {
  description = "Immutable image tag to deploy."
  type        = string

  validation {
    condition     = can(regex("^[0-9a-f]{40}$", var.notification_service_image_tag))
    error_message = "notification_service_image_tag must be a 40-character lowercase commit SHA."
  }
}

variable "notification_service_schedule_cron_expression" {
  description = "Five-field UTC cron expression for the scheduled job."
  type        = string
  default     = "0 6 * * *"
}

variable "notification_service_cpu" {
  description = "vCPU allocated to each Notification Service replica."
  type        = number
  default     = 0.5
}

variable "notification_service_memory" {
  description = "Memory allocated to each Notification Service replica."
  type        = string
  default     = "1Gi"
}

variable "notification_service_replica_timeout_in_seconds" {
  description = "Maximum execution duration for a Notification Service replica."
  type        = number
  default     = 1800
}

variable "notification_service_replica_retry_limit" {
  description = "Number of retries after a failed Notification Service replica."
  type        = number
  default     = 0
}

variable "notification_service_dotnet_environment" {
  description = "DOTNET_ENVIRONMENT value supplied to the container."
  type        = string
  default     = "Development"
}

# The variables below are declared because all service roots consume the shared
# environment tfvars files. They are not used by the Notification Service root.
variable "app_service_plan_sku" {
  type    = string
  default = null
}
variable "app_service_plan_os_type" {
  type    = string
  default = null
}
variable "app_service_plan_worker_count" {
  type    = number
  default = null
}
variable "function_dotnet_version" {
  type    = string
  default = null
}
variable "webapp_dotnet_version" {
  type    = string
  default = null
}
variable "use_auth_emulator" {
  type    = bool
  default = null
}
variable "use_ui_test_harness" {
  type    = bool
  default = null
}
variable "use_auxiliary_asp" {
  type    = bool
  default = null
}
variable "auxiliary_app_service_plan_sku" {
  type    = string
  default = null
}
variable "auxiliary_app_service_plan_os_type" {
  type    = string
  default = null
}
variable "auxiliary_app_service_plan_worker_count" {
  type    = number
  default = null
}
variable "key_vault_use_rbac" {
  type    = bool
  default = null
}
variable "getanidentifier_app_settings" {
  type    = map(string)
  default = {}
}
variable "ui_harness_app_settings" {
  type    = map(any)
  default = {}
}
variable "container_registry_sku" {
  type    = string
  default = null
}
