#!/bin/bash

set -e

echo "Building Lambda functions..."
cd "$(dirname "$0")/../.."

# Build all Lambda functions
dotnet build src/Lambda/GetCostData/GetCostData.csproj -c Release
dotnet build src/Lambda/AnalyzeCosts/AnalyzeCosts.csproj -c Release
dotnet build src/Lambda/GetAnalysisHistory/GetAnalysisHistory.csproj -c Release

echo "Building frontend..."
cd frontend
npm install
npm run build
cd ..

echo "Deploying with Terraform..."
cd infrastructure/terraform

# Initialize Terraform if needed
if [ ! -d ".terraform" ]; then
  terraform init
fi

# Plan and apply
terraform plan
read -p "Apply these changes? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
  terraform apply
fi

echo "Deployment complete!"
echo "API URL: $(terraform output -raw api_url)"
