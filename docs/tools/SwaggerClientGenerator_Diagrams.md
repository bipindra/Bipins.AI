# Swagger Client Generator - Architecture Diagrams

## System Architecture

```mermaid
graph TB
    subgraph "AI Agent"
        Agent[AI Agent]
        ToolRegistry[Tool Registry]
    end
    
    subgraph "Swagger Client Generator Tool"
        Tool[SwaggerClientGeneratorTool]
        Parser[IOpenApiParser]
        ModelGen[IModelGenerator]
        ClientGen[IClientGenerator]
        AuthGen[IAuthGenerator]
        Writer[IFileWriter]
    end
    
    subgraph "External Resources"
        Swagger[Swagger/OpenAPI Spec]
        FileSystem[File System]
    end
    
    subgraph "Generated Output"
        Models[Models/*.cs]
        Clients[Clients/*.cs]
        Auth[Auth/*.cs]
        DI[DI Setup]
    end
    
    Agent --> ToolRegistry
    ToolRegistry --> Tool
    Tool --> Parser
    Parser --> Swagger
    Tool --> ModelGen
    Tool --> ClientGen
    Tool --> AuthGen
    ModelGen --> Writer
    ClientGen --> Writer
    AuthGen --> Writer
    Writer --> FileSystem
    FileSystem --> Models
    FileSystem --> Clients
    FileSystem --> Auth
    FileSystem --> DI
```

## Component Interaction Flow

```mermaid
sequenceDiagram
    participant Agent
    participant Tool as SwaggerClientGeneratorTool
    participant Parser as OpenApiParser
    participant ModelGen as ModelGenerator
    participant ClientGen as ClientGenerator
    participant AuthGen as AuthGenerator
    participant Writer as FileWriter
    participant FS as File System
    
    Agent->>Tool: ExecuteAsync(toolCall)
    Tool->>Tool: Extract & Validate Parameters
    Tool->>Parser: ParseAsync(swaggerUrl)
    Parser->>Parser: Fetch & Parse OpenAPI
    Parser-->>Tool: OpenApiDocument
    
    Tool->>ModelGen: GenerateAsync(document)
    ModelGen->>ModelGen: Process Schemas
    ModelGen->>ModelGen: Apply Templates
    ModelGen-->>Tool: List<GeneratedFile>
    
    Tool->>ClientGen: GenerateAsync(document)
    ClientGen->>ClientGen: Process Operations
    ClientGen->>ClientGen: Apply Templates
    ClientGen-->>Tool: List<GeneratedFile>
    
    Tool->>AuthGen: GenerateAsync(document)
    AuthGen->>AuthGen: Process Security Schemes
    AuthGen->>AuthGen: Apply Templates
    AuthGen-->>Tool: List<GeneratedFile>
    
    Tool->>Writer: WriteAllAsync(files)
    Writer->>FS: Create Directories
    Writer->>FS: Write Files
    FS-->>Writer: File Paths
    Writer-->>Tool: Written Paths
    
    Tool-->>Agent: ToolExecutionResult
```

## Code Generation Pipeline

```mermaid
flowchart LR
    A[OpenAPI Spec] --> B{Parser}
    B --> C[OpenApiDocument]
    
    C --> D{Model Generator}
    C --> E{Client Generator}
    C --> F{Auth Generator}
    
    D --> G[Model Templates]
    G --> H[Models Code]
    
    E --> I[Client Templates]
    I --> J[Client Code]
    
    F --> K[Auth Templates]
    K --> L[Auth Code]
    
    H --> M[File Writer]
    J --> M
    L --> M
    
    M --> N[File System]
    N --> O[Complete Client Library]
```

## SOLID Principles Application

```mermaid
graph TD
    subgraph "Single Responsibility"
        Parser[OpenApiParser<br/>Parse OpenAPI specs]
        ModelGen[ModelGenerator<br/>Generate models]
        ClientGen[ClientGenerator<br/>Generate clients]
        AuthGen[AuthGenerator<br/>Generate auth]
        Writer[FileWriter<br/>Write files]
    end
    
    subgraph "Interface Segregation"
        IParser[IOpenApiParser]
        IModelGen[IModelGenerator]
        IClientGen[IClientGenerator]
        IAuthGen[IAuthGenerator]
        IWriter[IFileWriter]
    end
    
    subgraph "Dependency Inversion"
        Tool[SwaggerClientGeneratorTool<br/>Depends on Interfaces]
    end
    
    Parser -.implements.-> IParser
    ModelGen -.implements.-> IModelGen
    ClientGen -.implements.-> IClientGen
    AuthGen -.implements.-> IAuthGen
    Writer -.implements.-> IWriter
    
    Tool --> IParser
    Tool --> IModelGen
    Tool --> IClientGen
    Tool --> IAuthGen
    Tool --> IWriter
```

## Type Mapping Process

```mermaid
flowchart TB
    Start[OpenAPI Schema] --> Check{Schema Type}
    
    Check -->|string| String[C# string]
    Check -->|integer| Integer{Format?}
    Check -->|number| Number{Format?}
    Check -->|boolean| Bool[C# bool]
    Check -->|array| Array[List&lt;T&gt;]
    Check -->|object| Object{Has Properties?}
    
    Integer -->|int32| Int[int]
    Integer -->|int64| Long[long]
    Integer -->|none| DefaultInt[int]
    
    Number -->|float| Float[float]
    Number -->|double| Double[double]
    Number -->|none| DefaultDouble[double]
    
    Object -->|yes| CustomClass[Custom Class]
    Object -->|no| Dict[Dictionary&lt;string, object&gt;]
    
    String --> StringFormat{Format?}
    StringFormat -->|date-time| DateTime[DateTime]
    StringFormat -->|date| DateOnly[DateOnly]
    StringFormat -->|uuid| Guid[Guid]
    StringFormat -->|byte| ByteArray[byte[]]
    StringFormat -->|none| RegString[string]
    
    Int --> Nullable{Nullable?}
    Long --> Nullable
    Float --> Nullable
    Double --> Nullable
    Bool --> Nullable
    DateTime --> Nullable
    DateOnly --> Nullable
    Guid --> Nullable
    RegString --> Nullable
    
    Nullable -->|yes| NullableType[Type?]
    Nullable -->|no| RegularType[Type]
    
    Array --> ArrayNullable{Nullable?}
    ArrayNullable -->|yes| NullableList[List&lt;T&gt;?]
    ArrayNullable -->|no| RegularList[List&lt;T&gt;]
```

## File Organization Structure

```mermaid
graph TD
    Root[OutputPath/] --> Models[Models/]
    Root --> Clients[Clients/]
    Root --> Auth[Auth/]
    Root --> Exceptions[Exceptions/]
    Root --> Options[Options/]
    Root --> Extensions[ServiceCollectionExtensions.cs]
    
    Models --> Model1[User.cs]
    Models --> Model2[Product.cs]
    Models --> Model3[Order.cs]
    
    Clients --> Interface1[IUserClient.cs]
    Clients --> Client1[UserClient.cs]
    Clients --> Interface2[IProductClient.cs]
    Clients --> Client2[ProductClient.cs]
    
    Auth --> TokenProvider[ITokenProvider.cs]
    Auth --> Bearer[BearerAuthenticationHandler.cs]
    Auth --> ApiKey[ApiKeyAuthenticationHandler.cs]
    
    Exceptions --> ApiEx[ApiException.cs]
    
    Options --> ClientOpts[ClientOptions.cs]
```

## Implementation Phases

```mermaid
gantt
    title Swagger Client Generator Implementation Timeline
    dateFormat YYYY-MM-DD
    section Phase 1
    Core Infrastructure          :done, p1, 2024-01-01, 1d
    
    section Phase 2
    Add NuGet Packages          :p2, 2024-01-02, 1d
    Implement OpenApiParser     :p3, 2024-01-03, 2d
    Implement TypeMapper        :p4, 2024-01-05, 1d
    
    section Phase 3
    Model Templates             :p5, 2024-01-06, 1d
    ModelGenerator              :p6, 2024-01-07, 2d
    
    section Phase 4
    Client Templates            :p7, 2024-01-09, 2d
    ClientGenerator             :p8, 2024-01-11, 3d
    
    section Phase 5
    Auth Templates              :p9, 2024-01-14, 1d
    AuthGenerator               :p10, 2024-01-15, 2d
    
    section Phase 6
    FileWriter                  :p11, 2024-01-17, 2d
    
    section Phase 7
    Tool Integration            :p12, 2024-01-19, 2d
    
    section Phase 8
    Unit Tests                  :p13, 2024-01-21, 2d
    Integration Tests           :p14, 2024-01-23, 2d
    
    section Phase 9
    Sample Project              :p15, 2024-01-25, 1d
    
    section Phase 10
    Documentation               :p16, 2024-01-26, 2d
```

## Error Handling Flow

```mermaid
flowchart TD
    Start[Tool Execution] --> Validate{Validate<br/>Parameters}
    Validate -->|Invalid| Error1[Return Error Result]
    Validate -->|Valid| Parse{Parse<br/>OpenAPI}
    
    Parse -->|Error| Error2[Return Parse Error]
    Parse -->|Success| Generate{Generate<br/>Code}
    
    Generate -->|Models Error| Log1[Log Error]
    Generate -->|Clients Error| Log2[Log Error]
    Generate -->|Auth Error| Log3[Log Error]
    
    Log1 --> Partial[Continue with<br/>Partial Results]
    Log2 --> Partial
    Log3 --> Partial
    
    Generate -->|All Success| Write{Write<br/>Files}
    Partial --> Write
    
    Write -->|Error| Error3[Return Write Error]
    Write -->|Success| Success[Return Success Result]
    
    Error1 --> End[End]
    Error2 --> End
    Error3 --> End
    Success --> End
```

## Testing Strategy

```mermaid
graph TD
    subgraph "Unit Tests"
        UT1[OpenApiParser Tests]
        UT2[TypeMapper Tests]
        UT3[ModelGenerator Tests]
        UT4[ClientGenerator Tests]
        UT5[AuthGenerator Tests]
        UT6[FileWriter Tests]
        UT7[Tool Tests with Mocks]
    end
    
    subgraph "Integration Tests"
        IT1[Parse Real Swagger Specs]
        IT2[Generate Complete Libraries]
        IT3[Compile Generated Code]
        IT4[Test Generated Clients]
    end
    
    subgraph "End-to-End Tests"
        E2E1[Agent Uses Tool]
        E2E2[Generated Client Works]
        E2E3[Sample App Runs]
    end
    
    UT1 --> IT1
    UT2 --> IT1
    UT3 --> IT2
    UT4 --> IT2
    UT5 --> IT2
    UT6 --> IT2
    UT7 --> E2E1
    
    IT1 --> E2E1
    IT2 --> E2E2
    IT3 --> E2E2
    IT4 --> E2E3
```

---

**Note**: All diagrams are in Mermaid format and can be rendered in GitHub, VSCode, or any Markdown viewer with Mermaid support.
