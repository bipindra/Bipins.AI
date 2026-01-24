# AI Cost Optimization Advisor - Architecture

## System Architecture

```mermaid
graph TB
    subgraph Frontend["Frontend Layer"]
        React[React UI<br/>Static Site<br/>Vite + TypeScript]
        Charts[Cost Charts<br/>Recharts]
        Analysis[Analysis Display<br/>Components]
    end
    
    subgraph API["API Gateway Layer"]
        APIGW[HTTP API Gateway<br/>CORS Enabled<br/>Prod Stage]
        Route1[GET /costs]
        Route2[POST /analyze]
        Route3[GET /history]
    end
    
    subgraph Lambda["Lambda Functions Layer"]
        GetCost[GetCostData<br/>Runtime: .NET 8<br/>Timeout: 300s]
        Analyze[AnalyzeCosts<br/>Runtime: .NET 8<br/>Timeout: 300s]
        History[GetAnalysisHistory<br/>Runtime: .NET 8<br/>Timeout: 300s]
    end
    
    subgraph Storage["Data Storage Layer"]
        CacheTable[(CostDataCache<br/>DynamoDB<br/>TTL: 24h)]
        AnalysisTable[(CostAnalyses<br/>DynamoDB<br/>On-Demand)]
    end
    
    subgraph AWS["AWS Services"]
        CostExplorer[AWS Cost Explorer<br/>GetCostAndUsage API]
        Bedrock[AWS Bedrock<br/>Claude 3 Sonnet]
    end
    
    subgraph IAM["Security & Permissions"]
        LambdaRole[Lambda Execution Role]
        CostPolicy[Cost Explorer<br/>Read Permissions]
        BedrockPolicy[Bedrock<br/>Invoke Permissions]
        DynamoPolicy[DynamoDB<br/>Read/Write Permissions]
    end
    
    React -->|HTTPS REST| APIGW
    Charts --> React
    Analysis --> React
    
    APIGW --> Route1
    APIGW --> Route2
    APIGW --> Route3
    
    Route1 --> GetCost
    Route2 --> Analyze
    Route3 --> History
    
    GetCost -->|Query Cost Data| CostExplorer
    GetCost -->|Cache Results| CacheTable
    GetCost -->|Check Cache| CacheTable
    
    Analyze -->|Read Cost Data| CacheTable
    Analyze -->|AI Analysis| Bedrock
    Analyze -->|Store Results| AnalysisTable
    
    History -->|Query History| AnalysisTable
    
    LambdaRole --> GetCost
    LambdaRole --> Analyze
    LambdaRole --> History
    
    CostPolicy --> GetCost
    BedrockPolicy --> Analyze
    DynamoPolicy --> GetCost
    DynamoPolicy --> Analyze
    DynamoPolicy --> History
    
    style React fill:#61dafb,stroke:#333,stroke-width:2px
    style APIGW fill:#ff9900,stroke:#333,stroke-width:2px
    style GetCost fill:#fa7d00,stroke:#333,stroke-width:2px
    style Analyze fill:#fa7d00,stroke:#333,stroke-width:2px
    style History fill:#fa7d00,stroke:#333,stroke-width:2px
    style CacheTable fill:#4053d6,stroke:#333,stroke-width:2px,color:#fff
    style AnalysisTable fill:#4053d6,stroke:#333,stroke-width:2px,color:#fff
    style CostExplorer fill:#232f3e,stroke:#333,stroke-width:2px,color:#fff
    style Bedrock fill:#ff9900,stroke:#333,stroke-width:2px
```

## Data Flow

### 1. Cost Data Retrieval Flow

```mermaid
sequenceDiagram
    participant User
    participant React
    participant APIGW
    participant GetCostLambda
    participant CostExplorer
    participant DynamoDB
    
    User->>React: Select Date Range
    React->>APIGW: GET /costs?startDate=X&endDate=Y
    APIGW->>GetCostLambda: Invoke Function
    
    GetCostLambda->>DynamoDB: Check Cache
    alt Cache Hit
        DynamoDB-->>GetCostLambda: Return Cached Data
    else Cache Miss
        GetCostLambda->>CostExplorer: GetCostAndUsage API
        CostExplorer-->>GetCostLambda: Cost Data
        GetCostLambda->>DynamoDB: Store in Cache (TTL: 24h)
    end
    
    GetCostLambda-->>APIGW: Cost Data Response
    APIGW-->>React: JSON Response
    React->>React: Display Charts
    React-->>User: Visualized Cost Data
```

### 2. Cost Analysis Flow

```mermaid
sequenceDiagram
    participant User
    participant React
    participant APIGW
    participant AnalyzeLambda
    participant Bedrock
    participant DynamoDB
    
    User->>React: Click "Analyze Costs"
    React->>APIGW: POST /analyze { costData }
    APIGW->>AnalyzeLambda: Invoke Function
    
    AnalyzeLambda->>AnalyzeLambda: Prepare Analysis Prompt
    AnalyzeLambda->>Bedrock: InvokeModel (Claude)
    Bedrock-->>AnalyzeLambda: AI Analysis JSON
    
    AnalyzeLambda->>AnalyzeLambda: Parse Analysis
    AnalyzeLambda->>DynamoDB: Store Analysis
    
    AnalyzeLambda-->>APIGW: Analysis Response
    APIGW-->>React: Analysis JSON
    React->>React: Display Results
    React-->>User: Cost Drivers, Anomalies, Suggestions
```

### 3. Analysis History Flow

```mermaid
sequenceDiagram
    participant User
    participant React
    participant APIGW
    participant HistoryLambda
    participant DynamoDB
    
    User->>React: Click "Load History"
    React->>APIGW: GET /history?limit=10
    APIGW->>HistoryLambda: Invoke Function
    
    HistoryLambda->>DynamoDB: Scan CostAnalyses Table
    DynamoDB-->>HistoryLambda: Analysis Records
    
    HistoryLambda->>HistoryLambda: Sort by Date (Newest First)
    HistoryLambda-->>APIGW: Analysis List
    APIGW-->>React: JSON Response
    React->>React: Display History
    React-->>User: Past Analyses List
```

## Component Details

### Frontend Components

```mermaid
graph LR
    App[App.tsx<br/>Main Component]
    CostChart[CostChart.tsx<br/>Recharts Visualization]
    AnalysisResults[AnalysisResults.tsx<br/>Display Analysis]
    Suggestions[OptimizationSuggestions.tsx<br/>Show Recommendations]
    APIService[api.ts<br/>Axios Client]
    
    App --> CostChart
    App --> AnalysisResults
    App --> Suggestions
    App --> APIService
    
    APIService -->|HTTP Calls| APIGW[API Gateway]
    
    style App fill:#61dafb
    style CostChart fill:#ffc107
    style AnalysisResults fill:#28a745
    style Suggestions fill:#dc3545
    style APIService fill:#17a2b8
```

### Lambda Function Architecture

```mermaid
graph TB
    subgraph GetCostData["GetCostData Lambda"]
        GC1[Parse Request<br/>Date Range]
        GC2[Check DynamoDB Cache]
        GC3[Query Cost Explorer]
        GC4[Transform Response]
        GC5[Cache Results]
    end
    
    subgraph AnalyzeCosts["AnalyzeCosts Lambda"]
        AC1[Parse Cost Data]
        AC2[Build Bedrock Prompt]
        AC3[Invoke Bedrock]
        AC4[Parse AI Response]
        AC5[Store in DynamoDB]
    end
    
    subgraph GetHistory["GetAnalysisHistory Lambda"]
        GH1[Query DynamoDB]
        GH2[Sort by Date]
        GH3[Format Response]
    end
    
    GC1 --> GC2
    GC2 -->|Cache Miss| GC3
    GC3 --> GC4
    GC4 --> GC5
    
    AC1 --> AC2
    AC2 --> AC3
    AC3 --> AC4
    AC4 --> AC5
    
    GH1 --> GH2
    GH2 --> GH3
```

## Infrastructure Components

### DynamoDB Schema

```mermaid
erDiagram
    CostDataCache ||--o{ CostEntry : contains
    CostAnalyses ||--o{ AnalysisEntry : contains
    
    CostDataCache {
        string date PK
        string service SK
        string costData
        number ttl
    }
    
    CostAnalyses {
        string analysisId PK
        number timestamp SK
        string costData
        string dateRange
        string createdAt
    }
```

## Deployment Architecture

```mermaid
graph TB
    subgraph Dev["Development"]
        Local[Local Development<br/>npm run dev]
    end
    
    subgraph Build["Build Process"]
        DotNetBuild[.NET Build<br/>Release Mode]
        FrontendBuild[Frontend Build<br/>Vite Production]
    end
    
    subgraph Deploy["Deployment Options"]
        SAM[AWS SAM<br/>CloudFormation]
        Terraform[Terraform]
        OpenTofu[OpenTofu]
    end
    
    subgraph AWS["AWS Cloud"]
        LambdaFuncs[Lambda Functions]
        APIGateway[API Gateway]
        DynamoDB[DynamoDB Tables]
    end
    
    Local --> DotNetBuild
    DotNetBuild --> FrontendBuild
    FrontendBuild --> SAM
    FrontendBuild --> Terraform
    FrontendBuild --> OpenTofu
    
    SAM --> AWS
    Terraform --> AWS
    OpenTofu --> AWS
    
    AWS --> LambdaFuncs
    AWS --> APIGateway
    AWS --> DynamoDB
    
    style Local fill:#61dafb
    style DotNetBuild fill:#512bd4
    style FrontendBuild fill:#646cff
    style SAM fill:#ff9900
    style Terraform fill:#7b42bc
    style OpenTofu fill:#ff6b35
    style AWS fill:#232f3e,color:#fff
```

## Security Architecture

```mermaid
graph TB
    subgraph Security["Security Layers"]
        CORS[CORS Policy<br/>API Gateway]
        IAMRole[IAM Execution Role]
        IAMPolicies[IAM Policies]
        VPC[VPC Isolation<br/>Optional]
    end
    
    subgraph Permissions["Permission Matrix"]
        GetCostPerms[GetCostData<br/>- Cost Explorer Read<br/>- DynamoDB Read/Write]
        AnalyzePerms[AnalyzeCosts<br/>- Bedrock Invoke<br/>- DynamoDB Write]
        HistoryPerms[GetAnalysisHistory<br/>- DynamoDB Read]
    end
    
    CORS --> APIGW[API Gateway]
    IAMRole --> LambdaFuncs[Lambda Functions]
    IAMPolicies --> IAMRole
    
    IAMRole --> GetCostPerms
    IAMRole --> AnalyzePerms
    IAMRole --> HistoryPerms
    
    style CORS fill:#28a745
    style IAMRole fill:#ffc107
    style IAMPolicies fill:#dc3545
    style GetCostPerms fill:#17a2b8
    style AnalyzePerms fill:#17a2b8
    style HistoryPerms fill:#17a2b8
```

## Cost Flow

```mermaid
graph LR
    Start[User Request] --> GetCost[GetCostData]
    GetCost -->|Cache Hit| Return1[Return Cached]
    GetCost -->|Cache Miss| CostAPI[Cost Explorer API]
    CostAPI --> Cache[Store in Cache]
    Cache --> Return1
    
    Return1 --> Analyze[AnalyzeCosts]
    Analyze --> BedrockAPI[Bedrock API]
    BedrockAPI --> Analysis[Store Analysis]
    Analysis --> Return2[Return Analysis]
    
    Start --> History[GetAnalysisHistory]
    History --> Query[Query DynamoDB]
    Query --> Return3[Return History]
    
    style Start fill:#61dafb
    style GetCost fill:#fa7d00
    style Analyze fill:#fa7d00
    style History fill:#fa7d00
    style CostAPI fill:#232f3e,color:#fff
    style BedrockAPI fill:#ff9900
    style Cache fill:#4053d6,color:#fff
    style Analysis fill:#4053d6,color:#fff
```
