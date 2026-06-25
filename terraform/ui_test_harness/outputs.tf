output "service_state_key" {
  value       = one(module.app[*].service_state_key)
  description = "State key for the UI Test Harness Service"
}

output "web_app_name" {
  value       = one(module.app[*].web_app_name)
  description = "Name of the UI Test Harness web app."
}

output "web_app_id" {
  value       = one(module.app[*].web_app_id)
  description = "ID of the UI Test Harness web app."
}

output "web_app_default_hostname" {
  value       = one(module.app[*].web_app_default_hostname)
  description = "Default hostname of the UI Test Harness web app."
}