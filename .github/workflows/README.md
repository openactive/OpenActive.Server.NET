# CI Workflows
This project contains two GitHub Action workflows that are performed following various triggers. For more information on Github Actions see [here](https://docs.github.com/en/actions).

## openactive-test-suite.yml

This workflow is triggered when there is a push or a pull request to the master branch. This is indicated by:
```yml
on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]
```
This workflow defines the following jobs: 
- **test-server**
- **test-fake-database**
- **core**
- **framework**
- **deploy-reference-implementation**
- **publish-server**
- **publish-fake-database**

### test-server job

#### Setup
- **runs-on: ubuntu-latest**: This specifies that the job will run on an Ubuntu-based virtual machine.

#### Steps

- **Checkout**: This step checks out OpenActive.Server.NET.

- **Setup .NET Core 3.1.419**: This step uses the actions/setup-dotnet action to set up the .NET Core runtime environment with version 3.1.419.

- **Build OpenActive.Server.NET.Tests and dependencies**: This step runs the `dotnet build` command to build the "OpenActive.Server.NET.Tests" project and its dependencies. It uses the `--configuration Release` flag to build in release mode, which results in optimized code generation.

- **Run OpenActive.Server.NET.Tests**: This step runs the tests for the "OpenActive.Server.NET.Tests" project using the `dotnet test` command. It runs the unit tests and uses the `--configuration Release` flag for release mode, `--no-build` to skip rebuilding the project since it was built in the previous step, and `--verbosity normal` for the level of detail in the test output.

### test-fake-database job
This job is almost identical to the `test-server` job, but instead of building and running `OpenActive.Server.NET.Tests`, it builds and runs `Fakes/OpenActive.FakeDatabase.NET.Tests`.

### core job
#### Setup
- **runs-on: ubuntu-latest**: This specifies that the job will run on an Ubuntu-based virtual machine.
- **needs**: This section specifies that this job depends on the successful completion of two other jobs: "test-server" and "test-fake-database."
- **strategy**: This section defines a matrix strategy for running tests with different combinations of parameters, allowing for parallel test runs with various configurations. For more information about these parameters, see `.github/README.md` in Test Suite.

#### Steps
- **Checkout OpenActive.Server.NET**: This step checks out the `OpenActive.Server.NET` project's code repository and places it in a directory named "server."

- **Use matching coverage/ branch**: This step checks if the workflow is triggered by a branch starting with `coverage/`. If so, it sets an output variable named `mirror_ref` with the branch name, allowing later steps to use it.

- **Checkout OpenActive Test Suite**: This step checks out Test Suite and places it in a directory named "tests." The branch used for checkout is determined by the `mirror_ref` set in the previous step.

- **Setup .NET Core SDK 3.1.419**: This step sets up the .NET Core SDK version 3.1.419 in the job's environment.

- **Setup Node.js 14.x**: This step sets up Node.js version 14.x in the job's environment. Node.js is required for Test Suite.

- **Install OpenActive.Server.NET dependencies**: This step runs `dotnet restore` to install the dependencies for the "OpenActive.Server.NET" project. It is conditional and depends on the "profile" value not being 'no-auth' or 'single-seller.'

- **Build .NET Core Authentication Authority Reference Implementation**: This step builds `BookingSystem.AspNetCore.IdentityServer` if the `profile` is not `no-auth` or `single-seller` as it is not needed for these profiles.

- **Start .NET Core Authentication Authority Reference Implementation**: This step starts IdentityServer if the profiles are relevant (see above).

- **Build .NET Core Booking Server Reference Implementation**: This step builds Reference Implementation booking server.

- **Start .NET Core Booking Server Reference Implementation**: This step starts Reference Implementation booking server.

- **Install OpenActive Test Suite**: This step runs `npm install` to install the JavaScript dependencies for Test Suite, which is located in the "tests" directory.

- **Run OpenActive Integration Tests**: This step runs the OpenActive integration tests using `npm start` and specifies various environment variables based on the `mode` and `profile` matrix variables specified above.

- **Upload test output as artifact**: This step uploads the test output as an artifact, which can be used for reference or debugging later.

- **Deploy conformance certificate to Azure Blob Storage**: This step deploys a conformance certificate to Azure Blob Storage, but it is conditional and only runs when the GitHub branch is `master` and the `profile` is 'all-features' and the `mode` is 'controlled'. For more information on Coformance Certificates, see `packages/openactive-integration-tests/test/certification/README.md` in Test Suite.

### framework job
This job is very similar to the `core` job explained above. However because it runs on .NET Framework as opposed to .NET Core, there are some differences. Due to the similarity, only the differences are outlined below.

#### Setup
- **runs-on: windows-2019**: The Framework job runs on an Windows 2019 virtual machine, not an Ubuntu machine.
- **strategy**: The Framework job only runs the `no-auth` Test Suite profile.

#### Steps
- Building and deploying Framework is done using `msbuild` as opposed to `dotnet build`/`dotnet run`. It is started using IIS Express.

### deploy-reference-implementation job
This job uses many of the steps and setup used above. Because of this, only important or different parts have been highlighted.
This job deploys the `master` branch of Reference Implementation Booking Server and Identity Server Reference Implementation to two Azure webapps.
This can be seen in:
```yml
    # Deploy to Azure Web apps
      - name: 'Deploy Booking Server Reference Implementation using publish profile credentials'
        uses: azure/webapps-deploy@v2
        with: 
          app-name: openactive-reference-implementation
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE  }} # Define secret variable in repository settings as per action documentation
          package: './web-app-package/BookingSystem.AspNetCore'
      - name: 'Deploy Authentication Authority Reference Implementation using publish profile credentials'
        uses: azure/webapps-deploy@v2
        with: 
          app-name: openactive-reference-implementation-auth
          publish-profile: ${{ secrets.AZURE_WEBAPP_AUTH_PUBLISH_PROFILE  }} # Define secret variable in repository settings as per action documentation
          package: './web-app-package/BookingSystem.AspNetCore.IdentityServer'
```


### publish-server job
This job uses many of the steps and setup used above. Because of this, only important or different parts have been highlighted.

This job publishes the `master` branch of OpenActive.Server.NET to Nuget. It then creates a release in the GitHub repository.
```yml
      - name: Push to Nuget
        if: "! contains(toJSON(github.event.commits.*.message), '[no-release]')"
        run: dotnet nuget push "./OpenActive.Server.NET/**/*.nupkg" -k ${{secrets.NUGET_API_KEY}} --skip-duplicate -s https://api.nuget.org/v3/index.json
      - name: Create Release
        if: "! contains(toJSON(github.event.commits.*.message), '[no-release]')"
        id: create_release
        uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: v${{ steps.nbgv.outputs.SimpleVersion }}
          release_name: Release ${{ steps.nbgv.outputs.SimpleVersion }}
          body: |
            This release contains minor amendments based on updates to the [OpenActive Vocabulary](https://openactive.io/ns/) (codified by the [Data Models](https://github.com/openactive/data-models)), and the latest version of the [Dataset Site Template](https://github.com/openactive/dataset-site-template).
            
            Published to Nuget: [OpenActive.Server.NET](https://www.nuget.org/packages/OpenActive.Server.NET/${{ steps.nbgv.outputs.SimpleVersion }}) and [OpenActive.FakeDatabase.NET](https://www.nuget.org/packages/OpenActive.FakeDatabase.NET/${{ steps.nbgv.outputs.SimpleVersion }}).
          draft: false
          prerelease: false
```

### publish-fake-database job
This job uses many of the steps and setup used above. Because of this, only important or different parts have been highlighted.

Very similar to the above `publish-server` job, this job publishes the Fake Database project within OpenActive.Server.NET to Nuget.

## create-dependencies-pr.yaml

This workflow updates OpenActive.Server.NET's OA dependencies if there has been a change in one of the projects. This therefore keeps Server.NET up-to-date with the rest of the OA ecosystem.

This workflow can be triggered in two ways:

- **workflow_dispatch**: This allows you to manually trigger the workflow through the GitHub Actions UI.

- **repository_dispatch**: This allows the workflow to be triggered when a custom repository dispatch event is sent. It listens for dispatch events from two OA repos, specifically when they are updated: `OpenActive.NET-update` and `OpenActive.DatasetSite.NET-update`. 

This workflow defines one job: **generate**.

### generate job
#### Setup
- **runs-on: ubuntu-latest**: This specifies that the job will run on an Ubuntu-based virtual machine.

#### Steps
- **Checkout**: This step uses the actions/checkout action to clone the repository.

- **Setup .NET 6.0.x**: This step sets up the .NET SDK version 6.0.x in the job's environment.

- **Update OpenActive.NET to latest version in OpenActive.Server.NET**: This step updates the Server.NET solution with the latest OpenActive.NET library.

- **Update OpenActive.DatasetSite.NET to latest version in OpenActive.Server.NET**: Similar to the previous step, this updates Server.NET with the latest OpenActive.DatasetSite.NET.

- **Update OpenActive.NET to latest version in OpenActive.FakeDatabase.NET**: Similar to the previous step, this updates FakeDatabase.NET with the latest OpenActive.NET.

- **Create Pull Request**: This step uses the peter-evans/create-pull-request action to create a pull request. It performs several actions which are relatively self explanatory:

  - Sets up the access token needed for making changes to the repository.
  - Specifies the path to the repository containing the changes.
  - Defines a commit message, committer, author, branch name, and other PR-related details.
  - Provides a title and body for the pull request, explaining the purpose of the update.
  - Adds labels to the pull request (`automated pr`).
  - Marks the pull request as not a draft.
  
- **Check outputs** prints information about the created pull request, including the pull request number and URL, to the console.
