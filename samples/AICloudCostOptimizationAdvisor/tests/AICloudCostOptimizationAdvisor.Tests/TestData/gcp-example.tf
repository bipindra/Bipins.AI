# GCP Example Terraform Script
terraform {
  required_version = ">= 1.0"
  
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = "my-gcp-project"
  region  = "us-central1"
}

# Compute Instance
resource "google_compute_instance" "web_server" {
  name         = "vm-web-server"
  machine_type = "e2-medium"
  zone         = "us-central1-a"
  
  boot_disk {
    initialize_params {
      image = "ubuntu-os-cloud/ubuntu-2204-lts"
      size  = 100
      type  = "pd-standard"
    }
  }
  
  network_interface {
    network = "default"
    
    access_config {
      // Ephemeral public IP
    }
  }
  
  metadata = {
    ssh-keys = "admin:ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAAB..."
  }
  
  labels = {
    environment = "production"
    app         = "web"
  }
}

# Cloud Storage Bucket
resource "google_storage_bucket" "data_storage" {
  name          = "data-storage-bucket-2024"
  location      = "US"
  force_destroy = false
  
  versioning {
    enabled = true
  }
  
  labels = {
    environment = "production"
  }
}

# Cloud SQL Database
resource "google_sql_database_instance" "main_db" {
  name             = "main-database-instance"
  database_version = "MYSQL_8_0"
  region           = "us-central1"
  
  settings {
    tier = "db-f1-micro"
    
    backup_configuration {
      enabled = true
    }
    
    ip_configuration {
      ipv4_enabled = false
      private_network = "projects/my-gcp-project/global/networks/default"
    }
  }
  
  deletion_protection = false
}

resource "google_sql_database" "main_db" {
  name     = "main-database"
  instance = google_sql_database_instance.main_db.name
}

resource "google_sql_user" "main_db_user" {
  name     = "dbadmin"
  instance = google_sql_database_instance.main_db.name
  password = "P@ssw0rd123!"
}

# Cloud Function
resource "google_cloudfunctions_function" "api_handler" {
  name        = "api-handler"
  description = "API Handler Function"
  runtime     = "python311"
  
  available_memory_mb   = 256
  source_archive_bucket = google_storage_bucket.function_code.name
  source_archive_object  = google_storage_bucket_object.function_code.name
  trigger {
    http_trigger {}
  }
  
  entry_point = "handler"
  
  labels = {
    environment = "production"
  }
}

resource "google_storage_bucket" "function_code" {
  name     = "function-code-bucket"
  location = "US"
}

resource "google_storage_bucket_object" "function_code" {
  name   = "function.zip"
  bucket = google_storage_bucket.function_code.name
  source = "function.zip"
}

# Cloud Storage for backups
resource "google_storage_bucket" "backup_storage" {
  name          = "backup-storage-2024"
  location      = "US"
  storage_class = "STANDARD"
  
  lifecycle_rule {
    condition {
      age = 90
    }
    action {
      type = "Delete"
    }
  }
  
  labels = {
    purpose = "backup"
  }
}
