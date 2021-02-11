# BookingSystem.AspNetCore

An example OpenActive.Server.NET implementation.

This implementation is also used as a reference implementation for the [Test Suite](https://github.com/openactive/openactive-test-suite) to run its tests against.

## Running Locally for the 1st time

1. In Visual Studio, run the BookingSystem.AspNetCore project

    When it's finished building, it will open a page in your browser with a randomly assigned port e.g. http://localhost:55603/. Make note of this port.

    Head to `http://localhost:{PORT}/openactive` to check that the project is running correctly. You should see an Open Data landing page.
2. Head to BookingSystem.AspNetCore project options and add an env var using the port you made note of earlier:

    `ApplicationHostBaseUrl: http://localhost:{PORT}`
3. Now, re-run the project. You're good to go 👍
