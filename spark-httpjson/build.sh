#!/bin/bash

pushd "$( dirname "${BASH_SOURCE[0]}" )"

sudo docker run -it --rm \
  -v $PWD:/app \
  -w /app \
  mozilla/sbt \
  sbt package

