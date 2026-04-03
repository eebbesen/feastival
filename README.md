## feastival

An API for United States food holidays.

[See it in action!](https://feastival.azurewebsites.net/api/year)

![tests](https://github.com/eebbesen/feastival/actions/workflows/test.yml/badge.svg)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=eebbesen_feastival&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=eebbesen_feastival)

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

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/today provides all holidays for today

### range

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/range?startDate=MM-dd&endDate=MM-dd provides all holidays between the start and end dates

No date portions and no year allowed, just `MM-dd`

### about

https://AZURE_DOMAIN_PREFIX.azurewebsites.net/api/about provides the version and commit sha

## run

```bash
$ cd Function
$ func start
```

http://localhost:7071/api/year

## test

```bash
$ cd FeastivalTest
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
    -reports:"./FeastivalTest/TestResults/{guid}/coverage.opencover.xml" \
    -targetdir:"FeastivalTest/coveragereport" \
    -reporttypes:Html
```

### sonar

The sonar GitHub action will fail unless SONAR_TOKEN is set as a secret

## GitHub Actions workflows

This repository uses three GitHub Actions workflows:
* `test.yml` - Runs tests and generates coverage reports
* `sonar.yml` - Performs SonarQube code quality analysis
* `deploy.yml` - Builds, tests, and deploys to Azure Functions

### Rerunning failed workflows

If a GitHub Action workflow fails, there are several ways to rerun it:

#### Option 1: Rerun via GitHub UI

1. Go to the [Actions tab](https://github.com/eebbesen/feastival/actions) in the repository
2. Click on the failed workflow run
3. Click the "Re-run jobs" dropdown button in the top-right corner
4. Choose:
   - "Re-run all jobs" to rerun all jobs in the workflow
   - "Re-run failed jobs" to rerun only the jobs that failed

#### Option 2: Manually trigger workflows (workflow_dispatch)

All workflows in this repository are configured with `workflow_dispatch`, allowing manual triggering:

1. Go to the [Actions tab](https://github.com/eebbesen/feastival/actions) in the repository
2. Click on the workflow you want to run (test, sonar, or deploy)
3. Click the "Run workflow" button
4. Select the branch and click "Run workflow"

#### Option 3: Using GitHub CLI

If you have the [GitHub CLI](https://cli.github.com/) installed:

```bash
# Rerun a failed workflow
gh run rerun <run-id>

# Rerun only failed jobs
gh run rerun <run-id> --failed

# List recent workflow runs to find the run-id
gh run list

# Manually trigger a workflow
gh workflow run test.yml
gh workflow run sonar.yml
gh workflow run deploy.yml
```

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


## sources
* https://foodiegiggles.com/food-holidays-calendar
* https://nationaldayfood.com/all-national-food-days/


## days that shift every year
* National Fruitcake Toss Day (first Saturday in January)


* National Hot Dog Day (third Wednesday of July)
* National Ice Cream Day (third Sunday of July)
* National French Fry Day (second Friday in July)
* World Kebab Day (second Friday in July)
* National Refreshment Day (fourth Thursday in July)
* National Chili Dog Day (last Thursday in July)
* National Taco Day (first Tuesday in October)

