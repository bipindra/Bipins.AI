terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    archive = {
      source  = "hashicorp/archive"
      version = "~> 2.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# Data source for Lambda runtime
data "aws_region" "current" {}

# IAM Role for Lambda Functions
resource "aws_iam_role" "lambda_role" {
  name = "ai-cost-optimization-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

# IAM Policy for Lambda execution
resource "aws_iam_role_policy" "lambda_basic_execution" {
  name = "lambda-basic-execution"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

# IAM Policy for GetCostData Lambda
resource "aws_iam_role_policy" "get_cost_data_policy" {
  name = "get-cost-data-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ce:GetCostAndUsage",
          "ce:GetDimensionValues"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem"
        ]
        Resource = aws_dynamodb_table.cost_data_cache.arn
      }
    ]
  })
}

# IAM Policy for AnalyzeCosts Lambda
resource "aws_iam_role_policy" "analyze_costs_policy" {
  name = "analyze-costs-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "bedrock:InvokeModel"
        ]
        Resource = "arn:aws:bedrock:*::foundation-model/anthropic.claude-3-sonnet-20240229-v1:0"
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:PutItem"
        ]
        Resource = aws_dynamodb_table.cost_analyses.arn
      }
    ]
  })
}

# IAM Policy for GetAnalysisHistory Lambda
resource "aws_iam_role_policy" "get_analysis_history_policy" {
  name = "get-analysis-history-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:Scan",
          "dynamodb:Query"
        ]
        Resource = aws_dynamodb_table.cost_analyses.arn
      }
    ]
  })
}

# DynamoDB Tables
resource "aws_dynamodb_table" "cost_data_cache" {
  name         = "CostDataCache"
  billing_mode = "PAY_PER_REQUEST"

  hash_key  = "date"
  range_key = "service"

  attribute {
    name = "date"
    type = "S"
  }

  attribute {
    name = "service"
    type = "S"
  }

  ttl {
    attribute_name = "ttl"
    enabled        = true
  }

  tags = {
    Name        = "CostDataCache"
    Environment = var.environment
  }
}

resource "aws_dynamodb_table" "cost_analyses" {
  name         = "CostAnalyses"
  billing_mode = "PAY_PER_REQUEST"

  hash_key  = "analysisId"
  range_key = "timestamp"

  attribute {
    name = "analysisId"
    type = "S"
  }

  attribute {
    name = "timestamp"
    type = "N"
  }

  tags = {
    Name        = "CostAnalyses"
    Environment = var.environment
  }
}

# Build Lambda functions before packaging
resource "null_resource" "build_lambda_functions" {
  triggers = {
    always_run = timestamp()
  }

  provisioner "local-exec" {
    command = <<-EOT
      cd ${path.module}/../..
      dotnet build src/Lambda/GetCostData/GetCostData.csproj -c Release
      dotnet build src/Lambda/AnalyzeCosts/AnalyzeCosts.csproj -c Release
      dotnet build src/Lambda/GetAnalysisHistory/GetAnalysisHistory.csproj -c Release
    EOT
  }
}

# Archive Lambda function code
data "archive_file" "get_cost_data_zip" {
  type        = "zip"
  source_dir  = "${path.module}/../../src/Lambda/GetCostData/bin/Release/net8.0"
  output_path = "${path.module}/get-cost-data.zip"
  depends_on  = [null_resource.build_lambda_functions]
}

data "archive_file" "analyze_costs_zip" {
  type        = "zip"
  source_dir  = "${path.module}/../../src/Lambda/AnalyzeCosts/bin/Release/net8.0"
  output_path = "${path.module}/analyze-costs.zip"
  depends_on  = [null_resource.build_lambda_functions]
}

data "archive_file" "get_analysis_history_zip" {
  type        = "zip"
  source_dir  = "${path.module}/../../src/Lambda/GetAnalysisHistory/bin/Release/net8.0"
  output_path = "${path.module}/get-analysis-history.zip"
  depends_on  = [null_resource.build_lambda_functions]
}

# Lambda Functions
resource "aws_lambda_function" "get_cost_data" {
  filename         = data.archive_file.get_cost_data_zip.output_path
  function_name    = "GetCostData"
  role            = aws_iam_role.lambda_role.arn
  handler         = "GetCostData::GetCostData.Function::FunctionHandler"
  runtime         = "provided.al2"
  architectures   = ["x86_64"]
  timeout         = 300
  memory_size     = 512

  environment {
    variables = {
      COST_DATA_CACHE_TABLE = aws_dynamodb_table.cost_data_cache.name
      COST_ANALYSES_TABLE   = aws_dynamodb_table.cost_analyses.name
      BEDROCK_MODEL_ID      = var.bedrock_model_id
    }
  }

  source_code_hash = data.archive_file.get_cost_data_zip.output_base64sha256

  depends_on = [
    aws_iam_role_policy.lambda_basic_execution,
    aws_iam_role_policy.get_cost_data_policy
  ]
}

resource "aws_lambda_function" "analyze_costs" {
  filename         = data.archive_file.analyze_costs_zip.output_path
  function_name    = "AnalyzeCosts"
  role            = aws_iam_role.lambda_role.arn
  handler         = "AnalyzeCosts::AnalyzeCosts.Function::FunctionHandler"
  runtime         = "provided.al2"
  architectures   = ["x86_64"]
  timeout         = 300
  memory_size     = 512

  environment {
    variables = {
      COST_DATA_CACHE_TABLE = aws_dynamodb_table.cost_data_cache.name
      COST_ANALYSES_TABLE   = aws_dynamodb_table.cost_analyses.name
      BEDROCK_MODEL_ID      = var.bedrock_model_id
    }
  }

  source_code_hash = data.archive_file.analyze_costs_zip.output_base64sha256

  depends_on = [
    aws_iam_role_policy.lambda_basic_execution,
    aws_iam_role_policy.analyze_costs_policy
  ]
}

resource "aws_lambda_function" "get_analysis_history" {
  filename         = data.archive_file.get_analysis_history_zip.output_path
  function_name    = "GetAnalysisHistory"
  role            = aws_iam_role.lambda_role.arn
  handler         = "GetAnalysisHistory::GetAnalysisHistory.Function::FunctionHandler"
  runtime         = "provided.al2"
  architectures   = ["x86_64"]
  timeout         = 300
  memory_size     = 512

  environment {
    variables = {
      COST_DATA_CACHE_TABLE = aws_dynamodb_table.cost_data_cache.name
      COST_ANALYSES_TABLE   = aws_dynamodb_table.cost_analyses.name
      BEDROCK_MODEL_ID      = var.bedrock_model_id
    }
  }

  source_code_hash = data.archive_file.get_analysis_history_zip.output_base64sha256

  depends_on = [
    aws_iam_role_policy.lambda_basic_execution,
    aws_iam_role_policy.get_analysis_history_policy
  ]
}

# API Gateway
resource "aws_apigatewayv2_api" "api" {
  name          = "CostOptimizationApi"
  protocol_type = "HTTP"
  description   = "AI Cost Optimization Advisor API"

  cors_configuration {
    allow_origins = ["*"]
    allow_methods = ["GET", "POST", "OPTIONS"]
    allow_headers = ["Content-Type", "X-Amz-Date", "Authorization", "X-Api-Key"]
  }
}

resource "aws_apigatewayv2_stage" "api_stage" {
  api_id      = aws_apigatewayv2_api.api.id
  name        = "Prod"
  auto_deploy = true
}

# API Gateway Integrations
resource "aws_apigatewayv2_integration" "get_cost_data_integration" {
  api_id           = aws_apigatewayv2_api.api.id
  integration_type = "AWS_PROXY"

  integration_method = "POST"
  integration_uri    = aws_lambda_function.get_cost_data.invoke_arn
}

resource "aws_apigatewayv2_integration" "analyze_costs_integration" {
  api_id           = aws_apigatewayv2_api.api.id
  integration_type = "AWS_PROXY"

  integration_method = "POST"
  integration_uri    = aws_lambda_function.analyze_costs.invoke_arn
}

resource "aws_apigatewayv2_integration" "get_analysis_history_integration" {
  api_id           = aws_apigatewayv2_api.api.id
  integration_type = "AWS_PROXY"

  integration_method = "POST"
  integration_uri    = aws_lambda_function.get_analysis_history.invoke_arn
}

# API Gateway Routes
resource "aws_apigatewayv2_route" "get_costs_route" {
  api_id    = aws_apigatewayv2_api.api.id
  route_key = "GET /costs"
  target    = "integrations/${aws_apigatewayv2_integration.get_cost_data_integration.id}"
}

resource "aws_apigatewayv2_route" "analyze_costs_route" {
  api_id    = aws_apigatewayv2_api.api.id
  route_key = "POST /analyze"
  target    = "integrations/${aws_apigatewayv2_integration.analyze_costs_integration.id}"
}

resource "aws_apigatewayv2_route" "get_history_route" {
  api_id    = aws_apigatewayv2_api.api.id
  route_key = "GET /history"
  target    = "integrations/${aws_apigatewayv2_integration.get_analysis_history_integration.id}"
}

# Lambda Permissions for API Gateway
resource "aws_lambda_permission" "get_cost_data_permission" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.get_cost_data.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "analyze_costs_permission" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.analyze_costs.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "get_analysis_history_permission" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.get_analysis_history.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.api.execution_arn}/*/*"
}
