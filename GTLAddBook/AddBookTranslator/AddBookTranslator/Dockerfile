# Use the official .NET SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory in the container to /app
WORKDIR /app

# Copy the .csproj file and restore any dependencies (via nuget)
COPY *.csproj ./
RUN dotnet restore

# Copy the entire source code into the container
COPY . ./

# Build the application in release mode
RUN dotnet publish -c Release -o /app/out

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=build /app/out .

# Also copy the schema file
COPY add-book-schema.json .

# Set the entrypoint for the container
ENTRYPOINT ["dotnet", "AddBookTranslator.dll"]
