
# OpenActive.Server.NET [![Nuget](https://img.shields.io/nuget/v/OpenActive.Server.NET.svg)](https://www.nuget.org/packages/OpenActive.Server.NET/) [![OpenActive.Server.NET.Test](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive.Server.NET.Tests/badge.svg)](https://github.com/openactive/OpenActive.Server.NET/actions?query=workflow%3AOpenActive.Server.NET.Tests)

The Open Booking SDK for .NET provides components that aid the implementation of the OpenActive specificiations, including the [Open Booking API](https://openactive.io/open-booking-api/EditorsDraft/).

A readme is available within the [`OpenActive.Server.NET`](https://github.com/openactive/OpenActive.Server.NET/tree/master/OpenActive.Server.NET) library project.

Further documentation, including a step-by-step tutorial, can be found at https://tutorials.openactive.io/open-booking-sdk/.

# OpenActive Reference Implementation [![OpenActive Test Suite](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive%20Test%20Suite/badge.svg)](https://openactive.io/OpenActive.Server.NET/certification/)
[`BookingSystem.AspNetCore`](https://github.com/openactive/OpenActive.Server.NET/tree/master/Examples/BookingSystem.AspNetCore) provides an example use of the OpenActive.Server.NET library, as a fully standards compliant reference implementation of the OpenActive specifications, including the Open Booking API.

This is designed to have its code copied-and-pasted to provide a quick working starting point for any implementation.

# OpenActive.FakeDatabase.NET [![Nuget](https://img.shields.io/nuget/v/OpenActive.FakeDatabase.NET.svg)](https://www.nuget.org/packages/OpenActive.FakeDatabase.NET/)
This is an in-memory database that is used by BookingSystem.AspNetCore for illustration purposes. It can be added as a dependency to your project during the initial stages of implementation, to get a conformant test implementation as a starting position.
