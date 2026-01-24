build-GetCostDataFunction:
	@echo "Building GetCostData Lambda function..."
	cd "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\GetCostData" && dotnet build -c Release
	@echo "Copying files to artifacts directory..."
	@xcopy /E /I /Y "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\GetCostData\bin\Release\net8.0\*.*" "$(ARTIFACTS_DIR)\"
