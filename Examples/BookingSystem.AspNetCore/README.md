# BookingSystem.AspNetCore

An example OpenActive.Server.NET implementation.

This implementation is also used as a reference implementation for the [Test Suite](https://github.com/openactive/openactive-test-suite) to run its tests against and therefore is often to as Reference Implementation.
Until there are more reference implementations, all references to Reference Implementation refer to this implementation and Reference Implementation and BookingSystem.AspNetCore can be used interchangeably.

## Running Locally using Visual Studio

In Visual Studio, run the BookingSystem.AspNetCore project

When it's finished building, it will open a page in your browser on port 5001.

Head to `http://localhost:5001/openactive` to check that the project is running correctly. You should see an Open Data landing page.

See the [project contribution documentation](/CONTRIBUTING.md) for details on how to run BookingSystem.AspNetCore locally.

## Running Locally using the CLI

Open a terminal in `Examples/BookingSystem.AspNetCore` directory

Run:

```sh
dotnet run
```

If you want to start BookingSystem.AspNetCore in a specific environment run the following:

```sh
ASPNETCORE_ENVIRONMENT=no-auth dotnet run --no-launch-profile --project ./BookingSystem.AspNetCore.csproj --configuration Release --no-build
```

The above example starts the BookingSystem.AspNetCore in `no-auth` mode.

## BookingSystem.AspNetCore Data Generation

BookingSystem.AspNetCore has three main uses that make it very important in the OpenActive ecosystem:
- For data publishers / booking systems: It is used to demonstrate the properties and shape of data and APIs, according to the OpenActive specifications
- For data users / brokers: It is used as a trial integration where testing can be done with no ramifications
- For contributors: It is used to ensure the Test Suite tests are correct and passing, for different combinations of Open Booking API features.

The data for the sample feeds are generated in two places:
- BookingSystem.AspNetCore/Feeds/*Feeds.cs
- OpenActive.FakeDatabase.NET/Fakes/FakeBookingSystem.cs

The FakeBookingSystem within OpenActive.FakeDatabase.NET acts as the interface to an example database.
The example Feeds within BookingSystem.AspNetCore query this interface and translate the data to conform with the OpenActive Modelling Spec.

Due to this split of functionality, the sample data in the feeds are created/transformed in both files, depending on whether they are important to booking
or not. For example, `Price` is important to booking and there is generated in FakeBookingSystem at startup and stored in the in-memory database. However `Terms Of Service` is not
needed for booking, and therefore is generated at request time.

### Lorem Fitsum mode
When BookingSystem.AspNetCore is run in Lorem Fitsum (a play on [Lorem Ipsum](https://en.wikipedia.org/wiki/Lorem_ipsum)) mode, the data generated contains all the possible fields specified by the OpenActive Modelling Specification.
They are unrealistic representations of data, and the presence of all the fields should not be relied on when developing front-end representations of the data.
However it is very useful for data consumers and deciding on how to present the data to the users.

Lorem Fitsum mode can be running by setting the environment variable `IS_LOREM_FITSUM_MODE` to `true`.
In Visual Studio this can be done in Properties > BookingSystem.AspNetCore Properties > Run > Default > Environment Variables.
In the CLI this can be done by running the following command for example:

```sh
IS_LOREM_FITSUM_MODE=true dotnet run --no-launch-profile --project ./BookingSystem.AspNetCore.csproj --configuration Release --no-build
```

### Golden Records
Golden records are randomly generated records that have maximally enriched properties in the generated data. For example where a record might have one image normally, a golden record will have four.

