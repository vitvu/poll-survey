# 🚀 Deploy lên Render.com

Guide đầy đủ để deploy Poll Survey application lên Render.com (Free tier).

---

## 📋 Prerequisites

- GitHub account
- Render account ([render.com](https://render.com))
- MySQL database (có thể dùng free tier từ các provider như:
  - [PlanetScale](https://planetscale.com) - Free 5GB
  - [Railway](https://railway.app) - Free 500MB
  - [Aiven](https://aiven.io) - Free 25MB
  - Hoặc external host hiện tại của bạn)

---

## 🎯 Tổng Quan Deploy

Bạn sẽ tạo **4 services** trên Render:

1. **poll-service** - Web Service (ASP.NET)
2. **vote-service** - Web Service (ASP.NET)
3. **ocelot-gateway** - Web Service (ASP.NET)
4. **poll-client** - Static Site (Vue build)

---

## 📝 Bước 1: Chuẩn Bị Repository

### 1.1 Push Code lên GitHub

```bash
cd poll-survey

# Initialize git (nếu chưa có)
git init

# Add remote
git remote add origin https://github.com/YOUR_USERNAME/poll-survey.git

# Commit và push
git add .
git commit -m "Ready for Render deployment"
git push -u origin main
```

### 1.2 Verify Files

Đảm bảo có các file sau trong repo:
- ✅ `PollService/Dockerfile`
- ✅ `VoteService/Dockerfile`
- ✅ `OcelotGateway/Dockerfile`
- ✅ `client/Dockerfile`
- ✅ `OcelotGateway/ocelot.Production.json`
- ✅ `.dockerignore`
- ✅ `render.yaml` (sẽ tạo ở bước sau)

---

## 🗄️ Bước 2: Setup MySQL Database

### Option A: Sử dụng External MySQL hiện tại

Nếu bạn đã có MySQL host (như `103.153.64.170`), bạn có thể dùng luôn. Chỉ cần đảm bảo:
- Firewall cho phép kết nối từ Render IPs
- Có 2 databases: `tautotik1mooo_PollDB` và `tautotik1mooo_VoteDB`

### Option B: Tạo MySQL mới trên PlanetScale (Free)

1. Đăng ký tại [planetscale.com](https://planetscale.com)
2. Create new database: `poll-survey-db`
3. Tạo 2 branches (tương đương databases):
   - `polldb` 
   - `votedb`
4. Lấy connection strings cho mỗi branch
5. Lưu lại để dùng ở bước sau

**Connection String Format:**
```
Server=PLANET_SCALE_HOST;Database=poll-survey-db/polldb;User=USER;Password=PASSWORD;SslMode=VerifyFull;
```

---

## 🔧 Bước 3: Tạo render.yaml

Tạo file `render.yaml` trong project root:

```yaml
services:
  # PollService
  - type: web
    name: poll-service
    env: docker
    dockerfilePath: ./PollService/Dockerfile
    dockerContext: .
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://+:8080
      - key: ConnectionStrings__DefaultConnection
        sync: false  # Set manually in dashboard
      - key: AllowedOrigins__0
        fromService:
          type: web
          name: poll-client
          envVarKey: RENDER_EXTERNAL_URL

  # VoteService
  - type: web
    name: vote-service
    env: docker
    dockerfilePath: ./VoteService/Dockerfile
    dockerContext: .
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://+:8080
      - key: ConnectionStrings__DefaultConnection
        sync: false  # Set manually in dashboard
      - key: Services__PollServiceUrl
        fromService:
          type: web
          name: poll-service
          envVarKey: RENDER_EXTERNAL_URL
      - key: AllowedOrigins__0
        fromService:
          type: web
          name: poll-client
          envVarKey: RENDER_EXTERNAL_URL

  # OcelotGateway
  - type: web
    name: ocelot-gateway
    env: docker
    dockerfilePath: ./OcelotGateway/Dockerfile
    dockerContext: .
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://+:8080
      - key: PollServiceUrl
        fromService:
          type: web
          name: poll-service
          envVarKey: RENDER_INTERNAL_URL
      - key: VoteServiceUrl
        fromService:
          type: web
          name: vote-service
          envVarKey: RENDER_INTERNAL_URL

  # Frontend Client
  - type: web
    name: poll-client
    env: docker
    dockerfilePath: ./client/Dockerfile
    dockerContext: ./client
    buildCommand: |
      export VUE_APP_API_BASE_URL="https://ocelot-gateway.onrender.com"
      export VUE_APP_VOTE_SERVICE_URL="https://vote-service.onrender.com"
      npm install
      npm run build
```

**Lưu ý:** Replace `https://ocelot-gateway.onrender.com` và `https://vote-service.onrender.com` bằng URLs thực tế sau khi tạo services.

---

## 🚀 Bước 4: Deploy từ Render Dashboard

### 4.1 Connect GitHub Repository

1. Login vào [render.com](https://render.com)
2. Click **"New +"** → **"Blueprint"**
3. Connect GitHub account
4. Select repository: `poll-survey`
5. Render sẽ tự động phát hiện `render.yaml`

### 4.2 Configure Environment Variables

Sau khi blueprint deploy, vào từng service và set các biến:

#### poll-service Environment Variables

| Key | Value |
|-----|-------|
| `ConnectionStrings__DefaultConnection` | `Server=YOUR_MYSQL_HOST;Port=3306;Database=tautotik1mooo_PollDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;` |

#### vote-service Environment Variables

| Key | Value |
|-----|-------|
| `ConnectionStrings__DefaultConnection` | `Server=YOUR_MYSQL_HOST;Port=3306;Database=tautotik1mooo_VoteDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;` |

### 4.3 Update ocelot.Production.json

Sau khi có service URLs, update file `OcelotGateway/ocelot.Production.json`:

```json
{
  "GlobalConfiguration": {
    "BaseUrl": "https://ocelot-gateway.onrender.com"
  },
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/polls/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "DownstreamPathTemplate": "/api/Polls/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [ 
        { "Host": "poll-service.onrender.com", "Port": 443 } 
      ]
    },
    {
      "UpstreamPathTemplate": "/api/polls",
      "UpstreamHttpMethod": [ "GET", "POST" ],
      "DownstreamPathTemplate": "/api/Polls",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [ 
        { "Host": "poll-service.onrender.com", "Port": 443 } 
      ]
    },
    {
      "UpstreamPathTemplate": "/api/votes/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "DELETE" ],
      "DownstreamPathTemplate": "/api/Votes/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [ 
        { "Host": "vote-service.onrender.com", "Port": 443 } 
      ]
    },
    {
      "UpstreamPathTemplate": "/api/votes",
      "UpstreamHttpMethod": [ "GET", "POST", "DELETE" ],
      "DownstreamPathTemplate": "/api/Votes",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [ 
        { "Host": "vote-service.onrender.com", "Port": 443 } 
      ]
    },
    {
      "UpstreamPathTemplate": "/hubs/vote",
      "UpstreamHttpMethod": [ "GET", "POST", "OPTIONS" ],
      "DownstreamPathTemplate": "/hubs/vote",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [ 
        { "Host": "vote-service.onrender.com", "Port": 443 } 
      ]
    }
  ]
}
```

Push changes:
```bash
git add OcelotGateway/ocelot.Production.json
git commit -m "Update Render URLs"
git push
```

Render sẽ tự động redeploy.

---

## 🔍 Bước 5: Verify Deployment

### Check Service Status

1. Vào Render Dashboard
2. Verify tất cả 4 services status = **"Live"**
3. Check logs không có errors

### Test Services

1. **Test Gateway:**
   ```bash
   curl https://ocelot-gateway.onrender.com/api/polls/check/123456
   ```
   Expected: `404` hoặc `400` (poll không tồn tại)

2. **Test Frontend:**
   - Open `https://poll-client.onrender.com`
   - Create poll
   - Vote
   - Check analytics

### Test SignalR

1. Open Analytics page trong 2 browsers
2. Vote ở browser B
3. Verify browser A nhận instant update

---

## ⚙️ Configuration Details

### Dockerfile Configuration

Tất cả Dockerfiles đã được config để chạy trên port `8080` (Render requirement).

**Example PollService Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PollService/PollService.csproj", "PollService/"]
RUN dotnet restore "PollService/PollService.csproj"
COPY PollService/ PollService/
WORKDIR "/src/PollService"
RUN dotnet build "PollService.csproj" -c Release -o /app/build
RUN dotnet publish "PollService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PollService.dll"]
```

### CORS Configuration

Services đã config `AllowedOrigins` để accept requests từ Render URLs:

```csharp
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:8080" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

Set trong Environment Variables:
```
AllowedOrigins__0=https://poll-client.onrender.com
```

---

## 💰 Cost Estimate (Free Tier)

Render Free Tier includes:
- ✅ 750 hours/month per service (4 services = 3000 hours)
- ✅ Unlimited bandwidth
- ⚠️ Services sleep after 15 mins inactivity (cold start ~30s)
- ⚠️ 1 concurrent build

**Total Cost:** $0/month với free tier

**Upgrade Options:**
- Starter Plan: $7/month per service (no sleep)
- Pro Plan: $25/month per service (better performance)

---

## 🐛 Troubleshooting

### Service Won't Start

**Check Logs:**
```
Render Dashboard → Service → Logs tab
```

Common issues:
- Database connection failed → Check connection string
- Port mismatch → Ensure `ASPNETCORE_URLS=http://+:8080`
- Missing environment variables → Check dashboard settings

### Database Connection Timeout

**Solution:**
- Verify MySQL host allows Render IPs
- Test connection from local:
  ```bash
  mysql -h YOUR_HOST -u YOUR_USER -p
  ```
- Check firewall rules

### CORS Errors

**Solution:**
```csharp
// In appsettings.json (or Environment Variables)
"AllowedOrigins": [
  "https://poll-client.onrender.com"
]
```

### SignalR WebSocket Failed

**Cause:** HTTPS/WSS mismatch

**Solution:** Ensure `VUE_APP_VOTE_SERVICE_URL` uses `https://`:
```env
VUE_APP_VOTE_SERVICE_URL=https://vote-service.onrender.com
```

---

## 🔄 Update Deployment

### Deploy Code Changes

```bash
git add .
git commit -m "Update feature"
git push
```

Render tự động detect changes và redeploy.

### Force Redeploy

Render Dashboard → Service → **"Manual Deploy"** → **"Deploy latest commit"**

### Rollback

Render Dashboard → Service → **"Events"** → Click previous deploy → **"Redeploy"**

---

## 📊 Monitoring

### View Logs

```
Render Dashboard → Service → Logs tab
```

Real-time logs with filters:
- All logs
- Info
- Warn
- Error

### Metrics

```
Render Dashboard → Service → Metrics tab
```

Shows:
- CPU usage
- Memory usage
- Request count
- Response time

---

## 🔐 Security Best Practices

1. **Không commit sensitive data:**
   - Add `.env` to `.gitignore`
   - Use Render Environment Variables

2. **Use HTTPS:**
   - Render tự động provision SSL certificate

3. **Database Credentials:**
   - Store trong Environment Variables (encrypted at rest)
   - Rotate passwords định kỳ

4. **CORS:**
   - Chỉ allow specific origins (không dùng `AllowAnyOrigin` production)

---

## 📝 Checklist

- [ ] Code pushed to GitHub
- [ ] `render.yaml` created
- [ ] MySQL database accessible
- [ ] 4 services created on Render
- [ ] Environment variables configured
- [ ] `ocelot.Production.json` updated with Render URLs
- [ ] All services status = "Live"
- [ ] Frontend accessible
- [ ] Can create poll
- [ ] Can vote
- [ ] Real-time updates working
- [ ] CORS configured
- [ ] SSL working (https://)

---

## 🎉 Done!

Your Poll Survey app is now live on Render!

**Next Steps:**
- Share your app URL
- Monitor logs and metrics
- Consider upgrading to paid plan to avoid cold starts

**Questions?** Check [Render Docs](https://render.com/docs) hoặc [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core).

---

**Built with ❤️ deployed on Render**
