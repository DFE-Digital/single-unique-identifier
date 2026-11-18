terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.81.0"
    }
  }

  backend "azurerm" {}
}

provider "azurerm" {
  features {}
}
