variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "bedrock_model_id" {
  description = "Bedrock model ID to use for cost analysis"
  type        = string
  default     = "anthropic.claude-3-sonnet-20240229-v1:0"
}
