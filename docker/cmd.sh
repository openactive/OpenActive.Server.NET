#!/bin/bash

exit_func() {
  echo "[run.sh] SIGTERM detected"
  exit 1
}
trap exit_func SIGTERM SIGINT

# Start the first process
echo "[run.sh] starting BookingSystem"
cd /app/Examples/BookingSystem.AspNetCore
dotnet run &

# Wait for the first process to initialise
sleep 10

# Start the second process
echo "[run.sh] starting IdentityServer"
cd /app/Examples/BookingSystem.AspNetCore.IdentityServer
dotnet run &

# Wait for any process to exit
wait -n

# Exit with status of process that exited first
exit $?
