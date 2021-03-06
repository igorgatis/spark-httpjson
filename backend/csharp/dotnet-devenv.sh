#!/bin/bash

set -e

pushd "$( dirname "${BASH_SOURCE[0]}" )"

sudo docker run -it --rm \
  -v $PWD:/app \
  -w /app \
  --network=host \
  mcr.microsoft.com/dotnet/sdk:3.1 \
  /bin/bash $@
