terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.0"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  resource_provider_registrations = "none"
  storage_use_azuread             = true

  features {
    enhanced_validation {
      locations          = true
      resource_providers = true
      preflight_enabled  = false
    }
  }
}

provider "azapi" {}
