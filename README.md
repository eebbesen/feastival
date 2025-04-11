## feastival

An API for United States food holidays.

[See it in action!](https://feastival.azurewebsites.net/api/year)

![tests](https://github.com/eebbesen/feastival/actions/workflows/test.yml/badge.svg)

### year

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/year provides all holidays for the year

### month-day

Given DATE_PORTION is any of `MM-DD`, `MM-D`, `MM`, `M`

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/month-day?filter=DATE_PORTION provides
* all holidays for a date when `MM-DD` is provieded (e.g., `05-21`)
* all holidays for a block of days in a month when `MM-D` is provided (e.g., `05-2`)
* all holidays for a month when `MM` is provided (e.g., `05`)
* all holidays for a block of months when `M` is provided (e.g., `0`)

### today

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/year provides all holidays for today

### about

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/about provides the version and commit sha

## run

```bash
$ func start
```

http://localhost:7071/api/year

## test

```bash
$ dotnet test --settings tests.runsettings --collect "XPlat Code Coverage"
```

### parse test coverage
To view tests in HTML, install `reportgenerator` (once) https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=linux

    $ dotnet tool install -g dotnet-reportgenerator-globaltool

and add it to your path

    $ export PATH="$PATH:/Users/username/.dotnet/tools"

then after each test

```bash
$ reportgenerator \
    -reports:"./FeastivalTest/TestResults/{guid}/coverage.cobertura.xml" \
    -targetdir:"FeastivalTest/coveragereport" \
    -reporttypes:Html
```

### sonar

The sonar GitHub action will fail unless SONAR_TOKEN is set as a secret

## setup notes

* followed https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal?pivots=programming-language-csharp
    * created things in Azure first
    * created app using Visual Studio Code
    * added Application Insights instrumentation
        * but needed to add additional packages from what documentation stated to make it work
            * `using Microsoft.Azure.Functions.Worker;`
            * `using Microsoft.Extensions.DependencyInjection;`
        * removed Resource restriction on logging from host.json
    * deployed using Visual Studio Code and the Deploy to Function App option
    * Log Stream took a bit to show up, but it eventually did
    * needed to use VS Code to generate token as part of URL for the URL to work with the default `AuthorizationLevel`

### Azure deployment via GitHub Actions
Create deployer via the Azure CLI

```bash
az ad sp create-for-rbac --name NAME \
  --role contributor \
  --scopes /subscriptions/SUBSCRIPTION/resourceGroups/RESOURCE_GROUP \
  --sdk-auth
```

For deployments to succeed the following Azure secrets need to be populated in GitHub:
* CLIENT_ID_SECRET
* SUBSCRIPTION_ID_SECRET
* TENANT_ID_SECRET

then in Azure Portal create application registration credential linked to your GitHub repository
