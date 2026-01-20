variable "storage_account_name" {
  description = "Name of the storage account (must be globally unique, 3-24 lowercase letters/numbers)."
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group to deploy the storage account into."
  type        = string
}

variable "location" {
  description = "Azure location for the storage account."
  type        = string
}

variable "account_tier" {
  description = "Defines the Tier to use for this storage account (Standard or Premium)."
  type        = string
  default     = "Standard"
}

variable "account_replication_type" {
  description = "Defines the type of replication to use for this storage account (LRS, GRS, RAGRS, ZRS, GZRS or RAGZRS)."
  type        = string
  default     = "LRS"
}

variable "allow_nested_items_to_be_public" {
  description = "Allow or disallow nested items within this Account to opt into being public."
  type        = bool
  default     = false
}

variable "environment_tag" {
  description = "Environment tag value (for example Dev or Test)."
  type        = string
  default     = null
}

variable "product" {
  description = "Product tag value."
  type        = string
  default     = null
}

variable "service_offering" {
  description = "Service_Offering tag value."
  type        = string
  default     = null
}

variable "tags" {
  description = "Additional tags to apply to the storage account."
  type        = map(string)
  default     = {}
}
