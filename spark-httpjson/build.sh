#!/bin/bash

pushd "$( dirname "${BASH_SOURCE[0]}" )"

sudo docker build . -t spark-httpjson-sbt

sudo docker run -it --rm \
  -v $PWD:/app \
  -w /app \
  spark-httpjson-sbt \
  sbt package
