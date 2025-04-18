﻿# OpenActive.Server.NET [![Nuget](https://img.shields.io/nuget/v/OpenActive.Server.NET.svg)](https://www.nuget.org/packages/OpenActive.Server.NET/) [![OpenActive.Server.NET.Test](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive.Server.NET.Tests/badge.svg?branch=master)](https://github.com/openactive/OpenActive.Server.NET/actions?query=workflow%3AOpenActive.Server.NET.Tests)

The Open Booking SDK for .NET provides components that aid the implementation of the OpenActive specifications, including the [Open Booking API](https://openactive.io/open-booking-api/EditorsDraft/).

A readme is available within the [`OpenActive.Server.NET`](https://github.com/openactive/OpenActive.Server.NET/tree/master/OpenActive.Server.NET) library project.

Further documentation, including a step-by-step tutorial, can be found at https://tutorials.openactive.io/open-booking-sdk/.

# OpenActive Reference Implementation [![OpenActive Test Suite](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive%20Reference%20Implementation/badge.svg?branch=master)](https://certificates.reference-implementation.openactive.io/examples/all-features/controlled/)
[`BookingSystem.AspNetCore`](https://github.com/openactive/OpenActive.Server.NET/tree/master/Examples/BookingSystem.AspNetCore) provides an example use of the OpenActive.Server.NET library, as a fully standards compliant reference implementation of the OpenActive specifications, including the Open Booking API.

This is designed to have its code copied-and-pasted to provide a quick working starting point for any implementation.

# OpenActive.FakeDatabase.NET [![Nuget](https://img.shields.io/nuget/v/OpenActive.FakeDatabase.NET.svg)](https://www.nuget.org/packages/OpenActive.FakeDatabase.NET/) [![OpenActive.FakeDatabase.NET.Tests](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive.FakeDatabase.NET.Tests/badge.svg?branch=master)](https://github.com/openactive/OpenActive.Server.NET/actions?query=workflow%3AOpenActive.FakeDatabase.NET.Tests)
[`OpenActive.FakeDatabase.NET`](https://github.com/openactive/OpenActive.Server.NET/tree/master/Fakes/OpenActive.FakeDatabase.NET) is an in-memory database that is used by BookingSystem.AspNetCore for illustration purposes. It can be added as a dependency to your project during the initial stages of implementation, to get a conformant test implementation as a starting position.

Env vars to use when running OpenActive.FakeDatabase.NET:

- `SQLITE_DB_PATH`: (optional) The path to the SQLite database file. If not
  provided, a temporary file will be created. Be sure to provide this if you
  want to persist data between runs.
- `PERSIST_PREVIOUS_DATABASE`: (optional - default `false`) If set to `false`,
  the database will be recreated from scratch with each run. If set to `true`,
  and the database file already has data in it, this data will be preserved.
- `PERIODICALLY_REFRESH_DATA`: (optional - default `false`) If set to `true`,
  the database will be periodically refreshed, deleting past data and replacing
  it with future data.
