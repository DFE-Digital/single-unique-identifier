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
  description = "Whether the Key Vault is accessible from public networks."
  type        = bool
  default     = false
}

variable "private_endpoints_subnet_id" {
  description = "ID of the subnet where the private endpoint should be created."
  type        = string
  default     = null
}

variable "private_dns_zone_ids" {
  description = "IDs of the private DNS zones to link the private endpoint."
  type        = list(string)
  default     = []
}

variable "network_acls_ip_rules" {
  description = "One or more IPv4 addresses or CIDR blocks allowed through the Key Vault firewall when public network access is temporarily enabled."
  type        = list(string)
  default     = []
}
