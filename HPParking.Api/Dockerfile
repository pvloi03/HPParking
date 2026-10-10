# 1. Base Runtime .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5051
ENV ASPNETCORE_URLS=http://+:5051

# 2. Build Stage (Dùng SDK để restore và build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files để tận dụng Docker layer caching
COPY ["HPParking.Api/HPParking.Api.csproj", "HPParking.Api/"]
COPY ["HPParking.Core/HPParking.Core.csproj", "HPParking.Core/"]
RUN dotnet restore "HPParking.Api/HPParking.Api.csproj"

# Copy mã nguồn Api và Core
COPY HPParking.Api/ HPParking.Api/
COPY HPParking.Core/ HPParking.Core/

WORKDIR "/src/HPParking.Api"
RUN dotnet build "HPParking.Api.csproj" -c Release -o /app/build

# 3. Publish Stage
FROM build AS publish
RUN dotnet publish "HPParking.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Tạo thư mục storage lưu ảnh mặc định
RUN mkdir -p /app/storage

ENTRYPOINT ["dotnet", "HPParking.Api.dll"]
