# Azure DevOps Pipeline Setup Guide

This guide explains how to set up the Azure DevOps pipeline for building and publishing NuGet packages for Bipins.AI.

## Prerequisites

- Azure DevOps organization and project
- Repository connected to Azure DevOps
- Permissions to create pipelines and variable groups

## Pipeline Setup

### 1. Create Variable Group

1. Go to **Pipelines** → **Library** in Azure DevOps
2. Click **+ Variable group**
3. Name it `NuGetPublish`
4. Add the following variables:

   | Variable Name | Value | Type | Description |
   |--------------|-------|------|-------------|
   | `NuGetFeedUrl` | `https://api.nuget.org/v3/index.json` | Variable | NuGet feed URL (or your custom feed URL) |
   | `NuGetApiKey` | `your-api-key-here` | **Secret** | API key for NuGet feed authentication |
   | `PublishToNuGet` | `false` | Variable | Set to `true` to enable publishing (default: `false`) |

5. **Important**: Mark `NuGetApiKey` as a **Secret** by checking the lock icon
6. Click **Save**

### 2. Configure Variable Group Permissions

1. In the variable group, click **Security**
2. Grant **Read** permission to your build service account:
   - `[Project Name] Build Service`
   - Or your specific build service account

### 3. Create Pipeline

1. Go to **Pipelines** → **Pipelines**
2. Click **New pipeline**
3. Select your repository source (Azure Repos, GitHub, etc.)
4. Choose **Existing Azure Pipelines YAML file**
5. Select the branch and path: `azure-pipelines.yml`
6. Click **Continue** and **Run**

## Variable Group Examples

### For NuGet.org

```
NuGetFeedUrl: https://api.nuget.org/v3/index.json
NuGetApiKey: [Your NuGet.org API key from https://www.nuget.org/account/apikeys]
PublishToNuGet: true
```

### For Azure Artifacts

```
NuGetFeedUrl: https://pkgs.dev.azure.com/[Organization]/[Project]/_packaging/[FeedName]/nuget/v3/index.json
NuGetApiKey: [Personal Access Token with Packaging (read & write) scope]
PublishToNuGet: true
```

### For Private NuGet Server

```
NuGetFeedUrl: https://your-nuget-server.com/v3/index.json
NuGetApiKey: [Your API key]
PublishToNuGet: true
```

## Pipeline Behavior

### Build Stage

- Always runs on push to `master`, `main`, `develop`, or `release/*` branches
- Builds the solution in Release configuration
- Creates NuGet package using Cake build system
- Publishes packages as build artifacts

### Publish Stage

- Only runs if:
  - Build stage succeeded
  - `PublishToNuGet` variable is set to `true`
  - `NuGetFeedUrl` and `NuGetApiKey` are provided
- Downloads NuGet packages from artifacts
- Publishes to the configured NuGet feed

## Enabling Publishing

To enable publishing:

1. Go to **Pipelines** → **Library**
2. Open the `NuGetPublish` variable group
3. Edit `PublishToNuGet` and set it to `true`
4. Save the variable group
5. Run the pipeline again

## Disabling Publishing

To disable publishing (build only):

1. Go to **Pipelines** → **Library**
2. Open the `NuGetPublish` variable group
3. Edit `PublishToNuGet` and set it to `false`
4. Save the variable group

## Troubleshooting

### Pipeline fails with "Variable group not found"

- Ensure the variable group is named exactly `NuGetPublish`
- Check that the build service account has permission to read the variable group

### Publishing fails with authentication error

- Verify `NuGetApiKey` is correct and marked as a secret
- For Azure Artifacts, ensure the Personal Access Token has `Packaging (read & write)` scope
- For NuGet.org, verify the API key is valid and not expired

### Publishing is skipped

- Check that `PublishToNuGet` is set to `true`
- Verify `NuGetFeedUrl` and `NuGetApiKey` are not empty
- Check the pipeline logs for condition evaluation

### Package not found error

- Ensure the build stage completed successfully
- Verify packages were created in the `artifacts` directory
- Check that the Cake Pack task ran successfully

## Security Best Practices

1. **Never commit API keys** to the repository
2. **Always use variable groups** for sensitive information
3. **Mark secrets as secret** in variable groups (lock icon)
4. **Limit variable group access** to only necessary accounts
5. **Rotate API keys** regularly
6. **Use separate variable groups** for different environments (dev, staging, prod)

## Advanced Configuration

### Multiple Feeds

To publish to multiple feeds, create separate variable groups:
- `NuGetPublish-Prod` (for production)
- `NuGetPublish-Dev` (for development)

Then modify the pipeline to reference the appropriate variable group based on branch or manual selection.

### Conditional Publishing

You can modify the publish condition to only publish on specific branches:

```yaml
condition: and(succeeded(), eq(variables['PublishToNuGet'], 'true'), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
```

This will only publish when:
- Build succeeded
- `PublishToNuGet` is `true`
- Branch is `master`

## Support

For issues or questions, please refer to:
- [Azure DevOps Pipelines Documentation](https://docs.microsoft.com/en-us/azure/devops/pipelines/)
- [NuGet Package Publishing Guide](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
