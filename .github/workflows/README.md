# CI Workflows
This project contains two GitHub Action workflows that are performed following various triggers. For more information on Github Actions see [here](https://docs.github.com/en/actions).

## openactive-test-suite.yml


This workflow defines the following jobs: 
- **test-server**
- **test-fake-database**
- **core**
- **framework**
- **deploy-reference-implementation**
- **publish-server**
- **publish-fake-database**

### test-server job
This job builds and runs the `OpenActive.Server.NET.Tests` project i.e. it runs unit tests against `OpenActive.Server.NET`.

### test-fake-database job
This job builds and runs the `Fakes/OpenActive.FakeDatabase.NET.Tests` project i.e. it runs unit tests against `Fakes/OpenActive.FakeDatabase.NET`.

### core job
This job uses Test Suite to run a matrix of tests against the `OpenActive.Server.NET` project.

### framework job
This job is very similar to the above but builds `OpenActive.Server.NET` using .NET Framework and runs Test Suite against it.


### deploy-reference-implementation job
This job deploys the `master` branch of Reference Implementation Booking Server ([see here](../../Examples/BookingSystem.AspNetCore/README.md)) and Identity Server Reference Implementation ([see here](../../Examples/BookingSystem.AspNetCore.IdentityServer/README.md)) to two Azure webapps


### publish-server job
This job publishes the OpenActive.Server.NET project to Nuget. It then creates a release in the GitHub repository.


### publish-fake-database job
Very similar to the above `publish-server` job, this job publishes the Fake Database project within OpenActive.Server.NET to Nuget.

## create-dependencies-pr.yaml

This workflow creates a pull request to OpenActive.Server.NET's OpenActive (OA) dependencies if there has been a change in one of those projects. This therefore keeps Server.NET up-to-date with the rest of the OA ecosystem.

This workflow can be triggered in two ways:

- **workflow_dispatch**: This allows you to manually trigger the workflow through the GitHub Actions UI.

- **repository_dispatch**: This allows the workflow to be triggered when a custom repository dispatch event is sent. It listens for dispatch events from two OA repos: `OpenActive.NET` and `OpenActive.DatasetSite.NET`. For more information on repository dispatch events [see here](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows#repository_dispatch)  

This workflow defines one job: **generate**.

### generate job
The main steps of this job are as follows:
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
