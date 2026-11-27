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

variable "public_network_access_enabled" {
  description = "Whether public network access is enabled for the Key Vault."
  type        = bool
  default     = true
}

variable "private_endpoint_subnet_id" {
  description = "Optional subnet ID in which to create a private endpoint for the Key Vault."
  type        = string
  default     = null
}

variable "allowed_ip_rules" {
  description = "Optional list of IPv4 addresses or CIDR ranges to allow in the Key Vault network ACLs (e.g. temporary CI runner IP)."
  type        = list(string)
  default     = []
}
