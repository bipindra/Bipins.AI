build-GetAnalysisHistoryFunction:
	@echo "Building GetAnalysisHistory Lambda function..."
	cd "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\GetAnalysisHistory" && dotnet build -c Release
	@echo "Copying files to artifacts directory..."
	@xcopy /E /I /Y "C:\src\AI\Bipins.AI\samples\AICostOptimizationAdvisor\src\Lambda\GetAnalysisHistory\bin\Release\net8.0\*.*" "$(ARTIFACTS_DIR)\"
