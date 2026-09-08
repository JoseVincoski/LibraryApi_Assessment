FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/LibrarySystem.Api/LibrarySystem.Api.csproj", "src/LibrarySystem.Api/"]
COPY ["src/LibrarySystem.Contracts/LibrarySystem.Contracts.csproj", "src/LibrarySystem.Contracts/"]
COPY ["src/LibrarySystem.Service/LibrarySystem.Service.csproj", "src/LibrarySystem.Service/"]

RUN dotnet restore "src/LibrarySystem.Api/LibrarySystem.Api.csproj"
COPY . .

WORKDIR "/src/src/LibrarySystem.Api"
RUN dotnet publish "LibrarySystem.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LibrarySystem.Api.dll"]