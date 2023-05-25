# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY Examples/BookingSystem.AspNetCore.IdentityServer/BookingSystem.AspNetCore.IdentityServer.csproj ./Examples/BookingSystem.AspNetCore.IdentityServer/
COPY Examples/BookingSystem.AspNetCore/BookingSystem.AspNetCore.csproj ./Examples/BookingSystem.AspNetCore/
COPY Fakes/OpenActive.FakeDatabase.NET/OpenActive.FakeDatabase.NET.csproj ./Fakes/OpenActive.FakeDatabase.NET/
COPY Fakes/OpenActive.FakeDatabase.NET.Tests/OpenActive.FakeDatabase.NET.Tests.csproj ./Fakes/OpenActive.FakeDatabase.NET.Tests/
COPY OpenActive.Server.NET/OpenActive.Server.NET.csproj ./OpenActive.Server.NET/
COPY OpenActive.Server.NET.Tests/OpenActive.Server.NET.Tests.csproj ./OpenActive.Server.NET.Tests/
COPY Examples/BookingSystem.AspNetFramework/BookingSystem.AspNetFramework.csproj ./Examples/BookingSystem.AspNetFramework/
COPY Examples/BookingSystem.AspNetFramework.Tests/BookingSystem.AspNetFramework.Tests.csproj ./Examples/BookingSystem.AspNetFramework.Tests/
RUN dotnet restore

# Copy everything else
COPY Examples/. ./Examples/
COPY /Fakes/OpenActive.FakeDatabase.NET/. ./Fakes/OpenActive.FakeDatabase.NET/
COPY /OpenActive.Server.NET/. ./OpenActive.Server.NET/

# Build .NET Core Authentication Authority Reference Implementation
WORKDIR /source/Examples/BookingSystem.AspNetCore.IdentityServer
RUN dotnet publish -c release -o /app-id --no-restore

# Build .NET Core Booking Server Reference Implementation
WORKDIR /source/Examples/BookingSystem.AspNetCore
RUN dotnet publish -c release -o /app --no-restore

# target for identity-server
# See https://learn.microsoft.com/en-us/aspnet/core/security/docker-https to run
FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS identity-server
WORKDIR /app-id
COPY --from=build /app-id ./
ENTRYPOINT ["dotnet", "BookingSystem.AspNetCore.IdentityServer.dll"]

# default target
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "BookingSystem.AspNetCore.dll"]