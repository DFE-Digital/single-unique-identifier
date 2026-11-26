variable "scope" {
  description = "Scope to apply role assignments (typically the Key Vault resource ID)."
  type        = string
}

variable "assignments" {
  description = "Map of role assignments to create at the given scope."
  type = map(object({
    principal_id   = string
    role_name      = string
    principal_type = optional(string, "ServicePrincipal")
  }))
  default = {}

  validation {
    condition = alltrue([
      for assignment in values(var.assignments) :
      contains(["User", "Group", "ServicePrincipal"], assignment.principal_type)
    ])
    error_message = "Each assignment principal_type must be User, Group, or ServicePrincipal."
  }
}
