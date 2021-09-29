#!/bin/bash

set -e

pushd "$( dirname "${BASH_SOURCE[0]}" )"

pushd ../backend/csharp
./build.sh
popd
mkdir -p csharp/lib
cp ../backend/csharp/Spark.HttpJson.AspNetCore/bin/Debug/netstandard2.1/*.dll csharp/lib/

pushd ../spark-httpjson
./build.sh
popd
mkdir -p job/lib
mkdir -p job/project
cp ../spark-httpjson/target/scala-2.12/*.jar  job/lib/

sudo docker-compose up --build
