build-AnalyzeCostsFunction:
	@echo "Building AnalyzeCosts Lambda function..."
	cd "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\AnalyzeCosts" && dotnet build -c Release
	@echo "Copying files to artifacts directory..."
	@xcopy /E /I /Y "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\AnalyzeCosts\bin\Release\net8.0\*.*" "$(ARTIFACTS_DIR)\"
