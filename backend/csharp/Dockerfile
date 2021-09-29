FROM mcr.microsoft.com/dotnet/sdk:3.1

COPY app.sln /app/
COPY Spark.HttpJson/*.csproj /app/Spark.HttpJson/
COPY Spark.HttpJson.AspNetCore/*.csproj /app/Spark.HttpJson.AspNetCore/

WORKDIR /app

RUN dotnet restore app.sln
