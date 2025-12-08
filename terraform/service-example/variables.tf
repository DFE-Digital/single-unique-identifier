variable "subscription_prefix" {
  description = "Prefix that identifies the subscription (for example s270)."
  type        = string
}

variable "environment_id" {
  description = "Short identifier for the environment (for example d01)."
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
  description = "Azure location for the resource group (for example uksouth)."
  type        = string
}

variable "tags" {
  description = "Tags to propagate to service resources."
  type        = map(string)
}

variable "example_feature_enabled" {
  description = "Example service-specific flag."
  type        = bool
  default     = false
}
