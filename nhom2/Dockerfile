# Giai đoạn 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj và restore các thư viện (dependencies)
COPY ["nhom2.csproj", "./"]
RUN dotnet restore "./nhom2.csproj"

# Copy toàn bộ code còn lại và build (publish)
COPY . .
RUN dotnet publish "nhom2.csproj" -c Release -o /app/publish

# Giai đoạn 2: Chạy ứng dụng (Dùng image nhẹ hơn chỉ chứa runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Mở cổng mặc định (Render thường dùng cổng 80 hoặc 8080)
EXPOSE 8080

# Lệnh khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "nhom2.dll"]