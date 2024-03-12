# BookingSystem.AspNetCore

An example OpenActive.Server.NET implementation.

This implementation is also used as a reference implementation for the [Test Suite](https://github.com/openactive/openactive-test-suite) to run its tests against.

## Running Locally

1. In Visual Studio, run the BookingSystem.AspNetCore project

    When it's finished building, it will open a page in your browser with a randomly assigned port e.g. http://localhost:55603/. Make note of this port.

    Head to `http://localhost:{PORT}/openactive` to check that the project is running correctly. You should see an Open Data landing page.
2. Head to BookingSystem.AspNetCore project options and add an env var using the port you made note of earlier:

    `ApplicationHostBaseUrl: http://localhost:{PORT}`
3. Now, re-run the project. You're good to go 👍

See the [project contribution documentation](/CONTRIBUTING.md) for details on how to run BookingSystem.AspNetCore locally.

## Reference Implementation Data Generation

Reference Implementation has three main uses that make it very important in the OpenActive ecosystem:
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

### Golden Records
Golden records are randomly generated records that contain all possible fields specified by the OpenActive Modelling Specification.
They are unrealistic representations of data, and the presence of all the fields should not be relied on when developing front-end representations of the data.
