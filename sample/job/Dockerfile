FROM bde2020/spark-scala-template:3.1.1-hadoop3.2

COPY build.sbt /app/

COPY lib/ /app/

COPY project/ /app/

WORKDIR /app

RUN sbt update
