# Contributing

When contributing to this repository, please first discuss the change you wish to make via issue, Slack, or any other method with the owners of this repository before submitting a PR. 

Please note we have a [code of conduct](https://openactive.io/public-openactive-w3c/code-of-conduct/), please follow it in all your interactions with the project.

## Environment

Note that to set up a development environment for this project on a Mac, you will need:
- Visual Studio for Mac, selecting only .NET Core features on setup - [Download](https://visualstudio.microsoft.com/vs/mac/)
- .NET Core SDK 2.1.808 installer for Mac - [Download](https://dotnet.microsoft.com/download/dotnet-core/2.1)

## Test Suite

[**OpenActive Test Suite**](https://github.com/openactive/openactive-test-suite) is a suite of tests that can verify the conformance of an Open Booking API implementation, such as the **reference implementation** ([BookingSystem.AspNetCore](./Examples/BookingSystem.AspNetCore/)) contained within this project. OpenActive.Server.NET's CI checks that Test Suite passes for its reference implementation.

## Pull Request Process

Changes to [OpenActive.Server.Net](.) should be tested with the [Test Suite](#test-suite) before a Pull Request is submitted, to ensure that the reference implementation remains conformant. If changes are also required to Test Suite in order to properly test the new changes, then `coverage/*` branches should be used for both repositories, as documented in Test Suite's [Pull Request Process](https://github.com/openactive/openactive-test-suite/blob/master/CONTRIBUTING.md#pull-request-process) documentation.

## Locally running the reference implementation in order to run Test Suite

When you are making changes to [OpenActive.Server.NET](.), please run the **reference implementation** ([BookingSystem.AspNetCore](./Examples/BookingSystem.AspNetCore/)) and [Test Suite](#test-suite) on your machine to check that the changes work before submitting a [pull request](#pull-request-process).

How to run them both locally, using the `dotnet` CLI:

* **Reference Implementation**. Run the [reference implementation's IdentityServer](./Examples/BookingSystem.AspNetCore.IdentityServer/) and the [reference implementation itself](./Examples/BookingSystem.AspNetCore/).
    1. Run the IdentityServer:
        ```sh
        cd ./Examples/BookingSystem.AspNetCore.IdentityServer/
        dotnet run
        ```
    2. Run the reference implementation (See [**Optimizing for controlled mode**](#optimizing-for-controlled-mode) for a quicker way to run this):
        ```sh
        cd ./Examples/BookingSystem.AspNetCore/
        dotnet run
        ```
* **Test Suite**: To run this locally, follow the guidelines in its [project's contribution documentation](https://github.com/openactive/openactive-test-suite/blob/master/CONTRIBUTING.md).

### Optimizing for controlled mode

To speed up your development/testing feedback loop, you can optimize the reference implementation for [**controlled mode**](https://developer.openactive.io/open-booking-api/key-decisions#controlled-mode). In this mode, Test Suite creates all the data that it needs for testing. Test Suite is set to use this mode by default when running locally. With this mode, the reference implementation does not need to generate its own data, which it does by default, and so it will start up more quickly.

To do this, run reference implementation like this:

```sh
cd ./Examples/BookingSystem.AspNetCore/
export OPPORTUNITY_COUNT=1
dotnet run
```
