data "azurerm_subscription" "current" {}

locals {
  role_names = toset(distinct([
    for assignment in var.assignments : assignment.role_name
  ]))
}

data "azurerm_role_definition" "roles" {
  for_each = local.role_names
  name     = each.value
  scope    = data.azurerm_subscription.current.id
}

resource "azurerm_role_assignment" "this" {
  for_each           = var.assignments
  scope              = var.scope
  role_definition_id = data.azurerm_role_definition.roles[each.value.role_name].id
  principal_id       = each.value.principal_id
}
