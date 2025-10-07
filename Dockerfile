ARG DOTNET_VERSION=8.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

ENV HOME=/app
ENV PATH="${PATH}:${HOME}/.dotnet/tools"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# This installs the necessary ICU libraries that provide culture data (like pt-BR) for Alpine Linux.
RUN apk add --no-cache icu-libs

# Copy the solution file
COPY *.sln .

# Copy the source code (this creates /src/TecChallenge.Application, etc.)
COPY src/ ./src/
COPY TechChallengeGame.Tests/ ./TechChallengeGame.Tests/

# Restore dependencies for the entire solution
RUN dotnet restore "TechChallengeGame.sln"

# Publish the application
RUN dotnet publish src/TechChallengeGame.Application/TechChallengeGame.Application.csproj -c Release -o /app/publish --no-restore

RUN dotnet tool install --global dotnet-ef

# --- Final Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV HOME=/app

RUN apk add --no-cache icu-libs

COPY --from=build /app/publish .

RUN chown -R 0:0 /app && \
    chmod -R g+w /app

EXPOSE 80

# The entrypoint should now correctly point to your application's DLL
ENTRYPOINT ["dotnet", "TechChallengeGame.Application.dll"]