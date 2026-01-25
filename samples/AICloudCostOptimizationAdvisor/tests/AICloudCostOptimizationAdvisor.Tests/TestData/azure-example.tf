# Azure Example Terraform Script
terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-example"
  location = "East US"
}

# Virtual Machine
resource "azurerm_virtual_machine" "web_vm" {
  name                  = "vm-web-server"
  location              = azurerm_resource_group.main.location
  resource_group_name   = azurerm_resource_group.main.name
  network_interface_ids = [azurerm_network_interface.web_nic.id]
  vm_size              = "Standard_D2s_v3"
  
  storage_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts"
    version   = "latest"
  }
  
  storage_os_disk {
    name              = "osdisk-web"
    caching           = "ReadWrite"
    create_option     = "FromImage"
    managed_disk_type = "Premium_LRS"
    disk_size_gb      = 128
  }
  
  os_profile {
    computer_name  = "webserver"
    admin_username = "adminuser"
    admin_password = "P@ssw0rd123!"
  }
  
  os_profile_linux_config {
    disable_password_authentication = false
  }
  
  tags = {
    Environment = "Production"
  }
}

resource "azurerm_network_interface" "web_nic" {
  name                = "nic-web"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.main.id
    private_ip_address_allocation = "Dynamic"
  }
}

resource "azurerm_virtual_network" "main" {
  name                = "vnet-main"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
}

resource "azurerm_subnet" "main" {
  name                 = "subnet-main"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Storage Account
resource "azurerm_storage_account" "data_storage" {
  name                     = "storagedata2024"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type  = "LRS"
  account_kind             = "StorageV2"
  
  tags = {
    Environment = "Production"
  }
}

resource "azurerm_storage_container" "data_container" {
  name                  = "data"
  storage_account_name  = azurerm_storage_account.data_storage.name
  container_access_type = "private"
}

# SQL Database
resource "azurerm_sql_server" "main_db" {
  name                         = "sql-server-main"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password  = "P@ssw0rd123!"
}

resource "azurerm_sql_database" "main_db" {
  name                = "main-database"
  resource_group_name = azurerm_resource_group.main.name
  server_name         = azurerm_sql_server.main_db.name
  location            = azurerm_resource_group.main.location
  edition             = "Basic"
  requested_service_objective_name = "Basic"
  
  tags = {
    Environment = "Production"
  }
}

# Function App
resource "azurerm_function_app" "api_function" {
  name                       = "func-api-2024"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  app_service_plan_id        = azurerm_app_service_plan.function_plan.id
  storage_account_name       = azurerm_storage_account.function_storage.name
  storage_account_access_key = azurerm_storage_account.function_storage.primary_access_key
  version                    = "~4"
  
  app_settings = {
    FUNCTIONS_WORKER_RUNTIME = "python"
  }
}

resource "azurerm_app_service_plan" "function_plan" {
  name                = "asp-function-plan"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  kind                = "FunctionApp"
  reserved            = true
  
  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_storage_account" "function_storage" {
  name                     = "funcstorage2024"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type  = "LRS"
}
