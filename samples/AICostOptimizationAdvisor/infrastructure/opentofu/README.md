# OpenTofu Infrastructure

This directory contains OpenTofu configuration for deploying the AI Cost Optimization Advisor infrastructure to AWS.

OpenTofu is an open-source fork of Terraform that is fully compatible with Terraform configurations and providers.

## Prerequisites

- OpenTofu >= 1.6 (or Terraform >= 1.0)
- AWS CLI configured with appropriate credentials
- .NET 8 SDK (for building Lambda functions)
- Node.js and npm (for building frontend)

## Quick Start

1. Copy the example variables file:
```bash
cp terraform.tfvars.example terraform.tfvars
```

2. Edit `terraform.tfvars` with your desired values:
```hcl
aws_region      = "us-east-1"
environment     = "prod"
bedrock_model_id = "anthropic.claude-3-sonnet-20240229-v1:0"
```

3. Initialize OpenTofu:
```bash
tofu init
```

4. Review the deployment plan:
```bash
tofu plan
```

5. Deploy:
```bash
tofu apply
```

Or use the deployment script:
```bash
./deploy.sh
```

## Resources Created

- **Lambda Functions**: 3 functions (GetCostData, AnalyzeCosts, GetAnalysisHistory)
- **API Gateway**: HTTP API with CORS enabled
- **DynamoDB Tables**: 2 tables (CostDataCache, CostAnalyses)
- **IAM Roles & Policies**: Appropriate permissions for each Lambda function

## Outputs

After deployment, OpenTofu will output:
- `api_url`: API Gateway endpoint URL
- `get_cost_data_function_arn`: GetCostData Lambda ARN
- `analyze_costs_function_arn`: AnalyzeCosts Lambda ARN
- `get_analysis_history_function_arn`: GetAnalysisHistory Lambda ARN
- `cost_data_cache_table_name`: CostDataCache table name
- `cost_analyses_table_name`: CostAnalyses table name

## Destroying Resources

To remove all resources:
```bash
tofu destroy
```

## OpenTofu vs Terraform

This configuration is compatible with both OpenTofu and Terraform. The syntax is identical, and you can use either tool:

- **OpenTofu**: Use `tofu` command (e.g., `tofu init`, `tofu plan`, `tofu apply`)
- **Terraform**: Use `terraform` command (e.g., `terraform init`, `terraform plan`, `terraform apply`)

The deployment script uses OpenTofu by default, but you can modify it to use Terraform if preferred.

## Notes

- The OpenTofu configuration automatically builds the Lambda functions before packaging
- Lambda function code is packaged from the Release build output
- Make sure to update the frontend `.env` file with the API URL after deployment
