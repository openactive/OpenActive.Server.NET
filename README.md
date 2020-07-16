
# OpenActive.Server.NET [![OpenActive Test Suite](https://github.com/openactive/OpenActive.Server.NET/workflows/OpenActive%20Test%20Suite/badge.svg)](https://openactive.io/openactive-test-suite/example-output/random/summary) [![Nuget](https://img.shields.io/nuget/v/OpenActive.Server.NET.svg)](https://www.nuget.org/packages/OpenActive.Server.NET/)

This .NET implementation of the Open Booking SDK provides components that aid the implementation of the OpenActive Open Booking API.

Further documentation, including a step-by-step tutorial, can be found at https://tutorials.openactive.io/open-booking-sdk/.

## Library structure

The entire library system is designed to be modular:

### StoreBookingEngine
The StoreBookingEngine provides an opinionated implementation of the Open Booking API on top of AbstractBookingEngine.
This is designed to be quick to implement, but may not fit the needs of more complex systems.

It is not designed to be subclassed (it could be sealed?), but instead the implementer is encouraged
to implement and provide an IOpenBookingStore on instantiation. 

### AbstractBookingEngine
The AbstractBookingEngine provides a simple, basic and extremely flexible implementation of Open Booking API.

It is designed to be implemented by systems where StoreBookingEngine is too perscriptive, but who still prefer to use a solid foundation for thier development.

### Helper methods
Helper methods are designed to be used independently, for those who prefer flexibility and have a largely customised implementation.

The helper methods within these are isolated from each other and provide a useful toolbox for any implementation.

## Examples

This provides an example use of the library with a fully standards compliant reference implementation of the OpenActive Open Booking API.

### BookingSystem.AspNetCore
A reference implementation is provided `BookingSystem.AspNetCore` that is designed to have its code copied-and-pasted to provide a quick working starting point for any implementation.

### OpenActive.Server.NET.FakeBookingSystem
This is an in-memory database that is used by BookingSystem.AspNetCore for illustration purposes. It can be added as a dependency to your project during the initial stages of implementation, to get a conformant test implementation as a starting position.
