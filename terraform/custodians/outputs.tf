output "service_state_key" {
  value       = local.service_state_key
  description = "state key for the Custodians Service"
}
output "web_app_name" {
  value       = module.web_app.name
  description = "Name of the Custodians web app."
}

output "web_app_id" {
  value       = module.web_app.id
  description = "ID of the Custodians web app."
}

output "web_app_default_hostname" {
  value       = module.web_app.default_hostname
  description = "Default hostname of the Custodians web app."
}


