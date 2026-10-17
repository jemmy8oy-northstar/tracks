# Stage 1: Build .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

# Copy solution and project files
COPY Balenthiran.Tracks.slnx .
COPY Balenthiran.Tracks.WebApi/*.csproj ./Balenthiran.Tracks.WebApi/
COPY Balenthiran.Tracks.Abstractions/*.csproj ./Balenthiran.Tracks.Abstractions/
COPY Balenthiran.Tracks.DataModels/*.csproj ./Balenthiran.Tracks.DataModels/
COPY Balenthiran.Tracks.DomainModels/*.csproj ./Balenthiran.Tracks.DomainModels/
COPY Balenthiran.Tracks.Services/*.csproj ./Balenthiran.Tracks.Services/
COPY Balenthiran.Tracks.EntityModels/*.csproj ./Balenthiran.Tracks.EntityModels/ 
COPY Balenthiran.Tracks.Database/*.csproj ./Balenthiran.Tracks.Database/
COPY Balenthiran.Tracks.Tests/*.csproj ./Balenthiran.Tracks.Tests/
COPY Balenthiran.Tracks.SourceGenerators/*.csproj ./Balenthiran.Tracks.SourceGenerators/
# Restore dependencies
# NOTE: `dotnet restore` with no argument restores Balenthiran.Tracks.slnx, so EVERY project
# listed there must have its .csproj copied above — including test projects, which are
# not published. Adding a project to the solution without a COPY line here breaks the
# image build only, and only on a push to main, long after CI has gone green.
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish
RUN dotnet publish Balenthiran.Tracks.WebApi/Balenthiran.Tracks.WebApi.csproj -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (default ASP.NET Core port is 8080 in .NET 8+)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Balenthiran.Tracks.WebApi.dll"]
