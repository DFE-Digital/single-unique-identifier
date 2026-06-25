output "service_state_key" {
  value       = format("%s/ui_test_harness.tfstate", var.environment_id)
  description = "State key for the UI Test Harness Service"
}

output "web_app_name" {
  value       = try(module.app[0].web_app_name, null)
  description = "Name of the UI Test Harness web app."
}

output "web_app_id" {
  value       = try(module.app[0].web_app_id, null)
  description = "ID of the UI Test Harness web app."
}

output "web_app_default_hostname" {
  value       = try(module.app[0].web_app_default_hostname, null)
  description = "Default hostname of the UI Test Harness web app."
}