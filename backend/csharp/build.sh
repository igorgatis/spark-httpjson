#!/bin/bash

pushd "$( dirname "${BASH_SOURCE[0]}" )"

sudo docker run -it --rm \
  -v $PWD:/app \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:3.1 \
  dotnet build app.sln