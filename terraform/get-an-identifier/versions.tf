terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.14.0"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  resource_provider_registrations = "none"

  features {
    enhanced_validation {
      locations          = true
      resource_providers = true
      preflight_enabled  = false
    }
  }
}
