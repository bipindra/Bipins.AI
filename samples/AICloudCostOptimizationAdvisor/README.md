# AI Cloud Cost Optimization Advisor

A web application that analyzes Terraform infrastructure scripts and provides AI-powered cost optimization suggestions for AWS, Azure, and GCP using OpenAI via Bipins.AI.

## Features

- **Multi-Cloud Support**: Analyze Terraform scripts for AWS, Azure, and GCP resources
- **Cost Calculation**: Automatic cost estimation based on resource types and configurations
- **AI-Powered Optimizations**: Get intelligent optimization suggestions using OpenAI
- **Multiple Input Methods**: Upload files, paste code, or provide URLs
- **Visual Cost Breakdown**: Interactive charts showing costs by service and region
- **Resource Analysis**: Detailed breakdown of all resources with estimated costs

## Architecture

The application consists of:

- **ASP.NET MVC Backend**: RESTful API and server-side rendering
- **jQuery Frontend**: Interactive UI with Bootstrap and Chart.js
- **Bipins.AI Integration**: Uses `IChatModel` from Bipins.AI with OpenAI provider
- **Terraform Parser**: Regex-based HCL parser for extracting cloud resources
- **Cost Calculator**: Pricing estimation for AWS, Azure, and GCP resources

## Prerequisites

- .NET 8 SDK
- OpenAI API key
- (Optional) Terraform CLI for advanced parsing

## Getting Started

### 1. Configure OpenAI API Key

Set your OpenAI API key in one of the following ways:

**Option A: Environment Variable (Recommended)**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

**Option B: appsettings.json**
```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here"
  }
}
```

**Option C: User Secrets (Development)**
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

### 2. Build and Run

```bash
cd samples/AICloudCostOptimizationAdvisor/src/AICloudCostOptimizationAdvisor.Web
dotnet restore
dotnet build
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

### 3. Use the Application

1. Navigate to the home page
2. Choose an input method:
   - **Upload File**: Select a `.tf` file
   - **Paste Text**: Paste Terraform code directly
   - **From URL**: Provide a URL to a Terraform file
3. Select cloud providers to analyze (AWS, Azure, GCP)
4. Optionally enable AI-powered optimization suggestions
5. Click "Analyze Costs"
6. View detailed cost breakdown and optimization suggestions

## Example Terraform Script

```hcl
# AWS Example
resource "aws_instance" "web" {
  ami           = "ami-0c55b159cbfafe1f0"
  instance_type = "t3.large"
  
  tags = {
    Name = "WebServer"
  }
}

resource "aws_s3_bucket" "data" {
  bucket = "my-data-bucket"
}

# Azure Example
resource "azurerm_virtual_machine" "vm" {
  name                  = "vm-example"
  location              = "East US"
  resource_group_name   = "rg-example"
  vm_size              = "Standard_D2s_v3"
}

# GCP Example
resource "google_compute_instance" "vm" {
  name         = "vm-example"
  machine_type = "e2-medium"
  zone         = "us-central1-a"
}
```

## Project Structure

```
samples/AICloudCostOptimizationAdvisor/
├── src/
│   ├── AICloudCostOptimizationAdvisor.Web/     # ASP.NET MVC Web Application
│   │   ├── Controllers/                        # MVC Controllers
│   │   ├── Views/                              # Razor Views
│   │   ├── wwwroot/                           # Static files (JS, CSS)
│   │   ├── Program.cs                         # Application entry point
│   │   └── appsettings.json                   # Configuration
│   └── Shared/                                 # Shared library
│       ├── Models/                            # Data models
│       └── Services/                          # Business logic services
└── README.md
```

## Services

### TerraformParserService
- Parses Terraform HCL files using regex patterns
- Extracts resources by cloud provider (AWS, Azure, GCP)
- Validates Terraform syntax
- Supports file upload, text input, and URL fetching

### CloudCostCalculatorService
- Calculates estimated costs for AWS, Azure, and GCP resources
- Uses pricing data for common resource types
- Aggregates costs by service and region
- Provides monthly and annual cost estimates

### AICostAnalysisService
- Uses `IChatModel` from Bipins.AI with OpenAI provider
- Generates optimization suggestions based on infrastructure
- Provides actionable recommendations with estimated savings
- Categorizes suggestions by priority and type

## Configuration

### OpenAI Settings

```json
{
  "OpenAI": {
    "ApiKey": "required",
    "BaseUrl": "https://api.openai.com/v1",
    "DefaultChatModelId": "gpt-4o-mini",
    "DefaultEmbeddingModelId": "text-embedding-ada-002",
    "TimeoutSeconds": 60,
    "MaxRetries": 3
  }
}
```

## Cost Estimation

The application provides cost estimates based on:

- **AWS**: EC2, S3, RDS, Lambda, EBS pricing
- **Azure**: Virtual Machines, Storage Accounts, SQL Database, Functions
- **GCP**: Compute Engine, Cloud Storage, Cloud SQL, Cloud Functions

**Note**: These are estimates based on standard pricing. Actual costs may vary based on:
- Usage patterns
- Reserved instances and discounts
- Regional pricing differences
- Data transfer costs

## Limitations

- Terraform parsing uses regex patterns (not a full HCL parser)
- Cost estimates are approximations
- Does not account for reserved instances or discounts
- Limited to common resource types
- Analysis results are cached in memory (not persisted)

## Future Enhancements

- Full HCL parser integration
- Database persistence for analysis history
- Integration with cloud provider pricing APIs
- Support for Terraform modules
- Export analysis reports (PDF, CSV)
- Cost comparison across multiple Terraform scripts

## Troubleshooting

### OpenAI API Key Error
```
InvalidOperationException: OpenAI API key is required
```
**Solution**: Set `OPENAI_API_KEY` environment variable or configure in `appsettings.json`

### No Resources Found
```
BadRequest: No cloud resources found in Terraform script
```
**Solution**: Ensure your Terraform script contains valid resource blocks for AWS, Azure, or GCP

### Analysis Timeout
If analysis takes too long, increase the timeout:
```json
{
  "OpenAI": {
    "TimeoutSeconds": 120
  }
}
```

## License

This sample application is part of the Bipins.AI project and follows the same license.

## Contributing

Contributions are welcome! Please ensure your code follows the existing patterns and includes appropriate tests.
