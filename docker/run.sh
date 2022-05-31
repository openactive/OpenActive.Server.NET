#!/bin/bash

set -e

# You must run this script from the repo root directory
docker build -f docker/Dockerfile -t open-active-reference .
docker run --name open-active-reference --rm -it -p 5001:5001 -p 5002:5002 -p 5003:5003 open-active-reference
