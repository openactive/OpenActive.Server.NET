# BookingSystem.AspNetCore.IdentityServer

This project has been adapted from the [IdentityServer4 Quickstart project](https://github.com/IdentityServer/IdentityServer4/tree/main/src/IdentityServer4/host/):

- `./Quickstart` has been adapted from https://github.com/IdentityServer/IdentityServer4/tree/main/src/IdentityServer4/host/Quickstart
- `./Views` has been adapted from https://github.com/IdentityServer/IdentityServer4/tree/main/src/IdentityServer4/host/Views
- `./wwwroot` has been adapted from https://github.com/IdentityServer/IdentityServer4/tree/main/src/IdentityServer4/host/wwwroot
- `./Custom` has been added based on guidance found at http://docs.identityserver.io/en/latest/index.html

The project primarily adds the following features to the Quickstart:
- ClientStore, PersistedGrantStore, and UserRepository, allowing the OpenID Connect clients, grants and users to be stored in a database.
- An example Booking Partners management console, where the Seller and Booking System owner can manage Booking Partners.
- Endpoint for Dymanic Client Registration.
- Custom claims for the Open Booking API 'openactive-identity' scope via IdentityResources.
- Access Control scopes for Open Booking APIs via ApiScopes.
