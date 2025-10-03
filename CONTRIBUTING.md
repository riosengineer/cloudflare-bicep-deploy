# Contributing to CloudFlare Bicep Extension

We welcome contributions to the CloudFlare Bicep Extension! Please read our contributing guidelines below.

## Contribution Workflow

Code contributions follow a GitHub-centered workflow. To participate in the development of the CloudFlare Bicep extension, you require a GitHub account first.

Then, you can follow the steps below:

1. **Fork this repo** by going to the project repo page and use the `Fork` button.

2. **Clone down the repo** to your local system:

   ```bash
   git clone https://github.com/<username>/cloudflare-bicep-deploy.git
   ```

3. **Create a new branch** to hold your code changes you want to make:

   ```bash
   git checkout -b branch-name
   ```

4. **Work on your code** and test it if applicable.

When you are done with your work, make sure you commit the changes to your branch. Then, you can open a pull request on this repository.

## Adding a New Resource Type

To add a new CloudFlare resource type:

1. **Create a model class** in the `src/Models/` directory following the pattern of existing models
2. **Implement a handler** in the `src/Handlers/` directory extending `TypedResourceHandler`
3. **Register the handler** in `src/Program.cs` using `.WithResourceHandler<YourHandler>()`
4. **Test your changes** locally by running `bicep local-deploy` in the Sample directory
5. **Open a PR** for review

> **Note**: The structure follows CloudFlare's REST API patterns. Refer to the [CloudFlare API documentation](https://developers.cloudflare.com/api/) for resource specifications.

## Code Conventions

- Follow standard C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public methods and classes
- Ensure all new code includes appropriate error handling
- Test your changes with both local development and ACR deployment scenarios

## Testing Your Changes

1. **Local Testing**: Use the local binary path in `bicepconfig.json`:

   ```json
   {
     "extensions": {
       "CloudFlare": "../bin/cloudflare"
     }
   }
   ```

2. **ACR Testing**: Publish to a test ACR and reference it:

   ```json
   {
     "extensions": {
       "CloudFlare": "br:youracr.azurecr.io/cloudflare:0.1.0" // example
     }
   }
   ```

3. **Environment Setup**: Ensure you have `CLOUDFLARE_API_TOKEN` configured for testing

## Updating Documentation

The `src/docs/` folder contains auto-generated documentation for the Bicep extension resources. When you add new resource types or modify existing ones, you should regenerate the documentation.

### Installing the Documentation Generator

First, install the `bicep-local-docgen` tool globally:

```bash
dotnet tool install bicep-local-docgen -g
```

### Generating Documentation

To update the documentation after making changes to resource models:

1. **Navigate to the src directory**:

   ```bash
   cd src
   ```

2. **Run the documentation generator**:

   ```bash
   bicep-local-docgen generate --force
   ```

3. **Review the generated files** in the `src/docs/` directory to ensure they reflect your changes

4. **Commit the updated documentation** along with your code changes

> **Note**: The documentation generator reads the `BicepDocHeading`, `BicepDocExample`, and `TypeProperty` attributes from your model classes to create comprehensive documentation for each resource type.

## Additional Links

- [Issues](https://github.com/riosengineer/cloudflare-bicep-deploy/issues)
- [Pull Requests](https://github.com/riosengineer/cloudflare-bicep-deploy/pulls)
- [Actions](https://github.com/riosengineer/cloudflare-bicep-deploy/actions)
- [CloudFlare API Documentation](https://developers.cloudflare.com/api/)
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
