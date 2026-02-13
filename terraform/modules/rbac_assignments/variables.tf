variable "scope" {
  description = "Scope to apply role assignments (typically the Key Vault resource ID)."
  type        = string
}

variable "assignments" {
  description = "Map of role assignments to create at the given scope."
  type = map(object({
    principal_id = string
    role_name    = string
  }))
  default = {}
}
