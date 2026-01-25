# AWS Example Terraform Script
terraform {
  required_version = ">= 1.0"
  
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = "us-east-1"
}

# EC2 Instance
resource "aws_instance" "web_server" {
  ami           = "ami-0c55b159cbfafe1f0"
  instance_type = "t3.large"
  
  tags = {
    Name = "WebServer"
    Environment = "Production"
  }
}

# S3 Bucket
resource "aws_s3_bucket" "data_storage" {
  bucket = "my-data-bucket-2024"
  
  tags = {
    Name = "DataStorage"
  }
}

resource "aws_s3_bucket_versioning" "data_storage" {
  bucket = aws_s3_bucket.data_storage.id
  versioning_configuration {
    status = "Enabled"
  }
}

# RDS Database
resource "aws_db_instance" "main_db" {
  identifier     = "main-database"
  engine         = "mysql"
  engine_version = "8.0"
  instance_class = "db.t3.medium"
  allocated_storage = 100
  
  db_name  = "mydb"
  username = "admin"
  password = "changeme"
  
  tags = {
    Name = "MainDatabase"
  }
}

# Lambda Function
resource "aws_lambda_function" "api_handler" {
  filename      = "lambda.zip"
  function_name = "api-handler"
  role          = aws_iam_role.lambda_role.arn
  handler       = "index.handler"
  runtime       = "python3.11"
  
  tags = {
    Name = "APIHandler"
  }
}

resource "aws_iam_role" "lambda_role" {
  name = "lambda-execution-role"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })
}

# EBS Volume
resource "aws_ebs_volume" "data_volume" {
  availability_zone = "us-east-1a"
  size              = 500
  type              = "gp3"
  
  tags = {
    Name = "DataVolume"
  }
}
