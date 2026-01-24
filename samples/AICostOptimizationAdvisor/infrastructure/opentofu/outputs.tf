output "api_url" {
  description = "API Gateway endpoint URL"
  value       = aws_apigatewayv2_api.api.api_endpoint
}

output "get_cost_data_function_arn" {
  description = "GetCostData Lambda Function ARN"
  value       = aws_lambda_function.get_cost_data.arn
}

output "analyze_costs_function_arn" {
  description = "AnalyzeCosts Lambda Function ARN"
  value       = aws_lambda_function.analyze_costs.arn
}

output "get_analysis_history_function_arn" {
  description = "GetAnalysisHistory Lambda Function ARN"
  value       = aws_lambda_function.get_analysis_history.arn
}

output "cost_data_cache_table_name" {
  description = "CostDataCache DynamoDB table name"
  value       = aws_dynamodb_table.cost_data_cache.name
}

output "cost_analyses_table_name" {
  description = "CostAnalyses DynamoDB table name"
  value       = aws_dynamodb_table.cost_analyses.name
}
