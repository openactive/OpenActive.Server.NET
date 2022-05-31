FROM mcr.microsoft.com/dotnet/sdk:3.1

# copy from dotnet 2.1 for BookingSystem
COPY --from=mcr.microsoft.com/dotnet/sdk:2.1 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=mcr.microsoft.com/dotnet/sdk:2.1 /usr/share/dotnet/shared /usr/share/dotnet/shared

RUN mkdir -p /app
COPY .. /app

WORKDIR /app

RUN dotnet dev-certs https

EXPOSE 5001 5002 5003
CMD ["/app/docker/cmd.sh"]
