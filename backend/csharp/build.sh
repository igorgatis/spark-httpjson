#!/bin/bash

set -e

pushd "$( dirname "${BASH_SOURCE[0]}" )"

sudo docker build . -t spark-httpjson-backend-csharp

sudo docker run -it --rm \
  -v $PWD:/app \
  -w /app \
  spark-httpjson-backend-csharp \
  dotnet build app.sln