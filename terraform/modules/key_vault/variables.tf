variable "name" {
  description = "Name of the Key Vault"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "location" {
  description = "Azure location"
  type        = string
}

variable "tags" {
  description = "Additional tags to apply to the Key Vault."
  type        = map(string)
  default     = {}
}

variable "environment_tag" {
  description = "Environment tag value (e.g. Dev/Test/Prod)."
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

variable "tenant_id" {
  description = "Azure tenant ID for the Key Vault"
  type        = string
}

variable "rbac_authorization_enabled" {
  description = "Whether Azure Key Vault uses RBAC for data plane authorization."
  type        = bool
  default     = true
}

variable "network_acls_default_action" {
  type    = string
  default = "Deny"
}

variable "network_acls_bypass" {
  type    = string
  default = "AzureServices"
}

variable "network_acls_ip_rules" {
  type    = list(string)
  default = []
}

variable "network_acls_subnet_ids" {
  type    = list(string)
  default = []
}
