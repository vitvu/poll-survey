# 📊 Poll & Survey Builder

> Real-time polling application với ASP.NET Core microservices, Vue 3, SignalR, và MySQL

---

## 🎯 Tổng Quan

**Poll & Survey Builder** là ứng dụng tạo và quản lý poll real-time không cần đăng nhập. Người dùng có thể tạo poll với 4 loại câu hỏi (Multiple Choice, Yes/No, Rating, Open Text), chia sẻ link/QR code, và xem kết quả cập nhật trực tiếp qua SignalR WebSocket.

### ✨ Tính Năng Chính

- ✅ **Không cần đăng nhập** — voter chỉ cần browser token
- ✅ **4 loại câu hỏi** — Multiple Choice, Yes/No, Rating (1-5 sao), Open Text
- ✅ **Real-time results** — SignalR broadcast kết quả ngay khi có người vote
- ✅ **Poll expiry** — tự động hoặc custom deadline
- ✅ **QR code sharing** — tạo QR cho mỗi poll
- ✅ **Creator dashboard** — xem analytics, đóng/xóa poll
- ✅ **Microservices architecture** — PollService + VoteService + Gateway
- ✅ **Docker & Cloud ready** — Deploy dễ dàng lên Render.com

---

## 🏗️ Kiến Trúc Hệ Thống

```
┌─────────────────────────────────────────────────────────────┐
│                    Vue 3 Client (SPA)                       │
│                   https://localhost:8081                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         │ HTTPS + SignalR WebSocket
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              OcelotGateway (API Gateway)                    │
│                 https://localhost:5000                      │
│   Routes:                                                   │
│   /api/polls/*  → PollService:5001                         │
│   /api/votes/*  → VoteService:5002                         │
│   /hubs/vote    → VoteService:5002 (SignalR)               │
└───────────┬──────────────────────────┬──────────────────────┘
            │                          │
            ↓                          ↓
┌──────────────────────┐    ┌──────────────────────┐
│   PollService:5001   │    │  VoteService:5002    │
│                      │    │                      │
│  • Polls CRUD        │    │  • Submit votes      │
│  • Validation        │    │  • Get results       │
│  • MySQL: PollDB     │◄───┤  • SignalR Hub       │
│    - Polls table     │    │  • MySQL: VoteDB     │
│    - Options table   │    │    - Votes table     │
└──────────────────────┘    └──────────────────────┘
            │                          │
            └──────────┬───────────────┘
                       ↓
               ┌────────────────┐
               │ MySQL Server   │
               │ (External Host)│
               │ - PollDB       │
               │ - VoteDB       │
               └────────────────┘
```

---

## 🛠️ Tech Stack

### Backend
- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core 8.0** - ORM
- **MySQL** - Database (hỗ trợ external hosting)
- **SignalR** - Real-time WebSocket communication
- **Ocelot** - API Gateway
- **Swagger** - API documentation

### Frontend
- **Vue 3** - Progressive JavaScript framework
- **Vue Router** - SPA routing
- **Axios** - HTTP client
- **@microsoft/signalr** - SignalR client library
- **Tailwind CSS** - Utility-first CSS framework
- **QRCode.js** - QR code generation
- **Lucide Icons** - Icon library

---

## 📊 Database Schema

### PollDB (PollService)

**Polls Table:**
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment |
| Code | varchar(255) | 6-digit unique code |
| Question | varchar(500) | Poll question |
| QuestionType | varchar(50) | Multiple Choice / Yes No / Rating / Open Text |
| Status | varchar(50) | Active / Closed |
| ExpireAt | datetime | Poll deadline (UTC) |
| CreatedAt | datetime | Creation timestamp (UTC) |

**Options Table:**
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment |
| PollId | int (FK) | Foreign key → Polls.Id |
| Text | varchar(255) | Option text |

### VoteDB (VoteService)

**Votes Table:**
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment |
| PollCode | varchar(255) | Link to Poll code |
| OptionId | int | Selected option ID |
| VoteValue | varchar(500) | Rating value or open text |
| VoterToken | varchar(255) | Browser fingerprint token |
| CreatedAt | datetime | Vote timestamp (UTC) |

---

## 🚀 Local Development Setup

### Prerequisites
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **MySQL Server** - Local hoặc external host

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/poll-survey.git
cd poll-survey
```

### 2. Cấu Hình Database

**Option A: Sử dụng MySQL External Host (Recommended)**

Edit `PollService/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_HOST;Port=3306;Database=YOUR_POLLDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;"
  }
}
```

Edit `VoteService/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_HOST;Port=3306;Database=YOUR_VOTEDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;"
  }
}
```

**Database sẽ tự động được tạo** khi service khởi động lần đầu (`EnsureCreated()` trong `Program.cs`).

### 3. Chạy Backend Services

**Terminal 1 - OcelotGateway:**
```bash
cd OcelotGateway
dotnet run
# Listening on: https://localhost:5000
```

**Terminal 2 - PollService:**
```bash
cd PollService
dotnet run
# Listening on: https://localhost:5001
```

**Terminal 3 - VoteService:**
```bash
cd VoteService
dotnet run
# Listening on: https://localhost:5002
```

### 4. Chạy Frontend

**Terminal 4 - Vue Client:**
```bash
cd client
npm install
npm run dev
# Running at: http://localhost:8081
```

### 5. Truy Cập Application

- **Frontend:** http://localhost:8081
- **Gateway:** https://localhost:5000
- **PollService Swagger:** https://localhost:5001/swagger
- **VoteService Swagger:** https://localhost:5002/swagger

---

## 🐳 Docker Deployment

### Prerequisites
- Docker Desktop installed
- External MySQL database accessible from Docker containers

### 1. Cấu Hình Environment Variables

Edit `.env` file:
```env
# MySQL External Hosting
POLL_DB_CONNECTION=Server=YOUR_HOST;Port=3306;Database=YOUR_POLLDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;
VOTE_DB_CONNECTION=Server=YOUR_HOST;Port=3306;Database=YOUR_VOTEDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;

# URLs
GATEWAY_PUBLIC_URL=http://localhost:5000
VOTE_SERVICE_PUBLIC_URL=http://localhost:5002
FRONTEND_URL=http://localhost:8081
```

### 2. Build và Run
```bash
docker-compose up -d --build
```

### 3. Verify Services
```bash
docker-compose ps

# Expected output:
# NAME             STATUS
# ocelot-gateway   Up
# poll-service     Up
# vote-service     Up
# poll-client      Up
```

### 4. Access Application
- Frontend: http://localhost:8081
- Gateway: http://localhost:5000

### Stop Services
```bash
docker-compose down
```

---

## ☁️ Deploy lên Render.com

Xem file [RENDER_DEPLOYMENT.md](./RENDER_DEPLOYMENT.md) để biết chi tiết.

**Quick Steps:**
1. Push code lên GitHub
2. Tạo 4 Web Services trên Render:
   - `poll-service` (ASP.NET)
   - `vote-service` (ASP.NET)
   - `ocelot-gateway` (ASP.NET)
   - `poll-client` (Static Site)
3. Cấu hình environment variables
4. Deploy!

---

## 📁 Project Structure

```
poll-survey/
├── PollService/              # Poll management service
│   ├── Controllers/
│   │   └── PollsController.cs
│   ├── Data/
│   │   └── PollDbContext.cs
│   ├── Models/
│   │   ├── Poll.cs
│   │   └── Option.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── VoteService/              # Vote management service
│   ├── Controllers/
│   │   └── VotesController.cs
│   ├── Data/
│   │   └── VoteDbContext.cs
│   ├── Hubs/
│   │   └── VoteHub.cs        # SignalR Hub
│   ├── Models/
│   │   └── Vote.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── OcelotGateway/            # API Gateway
│   ├── ocelot.json           # Local routing
│   ├── ocelot.Production.json # Docker routing
│   ├── Program.cs
│   └── Dockerfile
├── client/                   # Vue 3 Frontend
│   ├── src/
│   │   ├── views/
│   │   │   ├── HomeView.vue
│   │   │   ├── CreatePollView.vue
│   │   │   ├── VoteView.vue
│   │   │   └── AnalyticsView.vue
│   │   ├── api.js            # Axios API wrapper
│   │   ├── usePollHub.js     # SignalR composable
│   │   └── voterToken.js     # Token generator
│   ├── package.json
│   └── Dockerfile
├── docker-compose.yml        # Docker orchestration
├── .env                      # Environment variables
├── .env.example
└── README.md
```

---

## 🔌 API Endpoints

### PollService (Port 5001)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/polls` | Tạo poll mới |
| GET | `/api/polls/code/{code}` | Lấy thông tin poll + options |
| GET | `/api/polls/check/{code}` | Validate poll active |
| PUT | `/api/polls/code/{code}` | Update poll (status, expiry) |
| DELETE | `/api/polls/code/{code}` | Xóa poll |

### VoteService (Port 5002)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/votes` | Submit vote |
| GET | `/api/votes/{pollCode}` | Get all vote data |
| DELETE | `/api/votes?pollCode={code}` | Delete all votes (internal) |

### SignalR Hub

| Endpoint | `/hubs/vote` |
|----------|--------------|
| **Server Methods** | `JoinPollRoom(pollCode)`, `LeavePollRoom(pollCode)` |
| **Client Events** | `VoteUpdated`, `PollClosed` |

---

## 🧪 Testing

### Manual Testing Flow

1. **Create Poll:** http://localhost:8081/create
   - Fill form → Submit → Get code (e.g., 143594)
   
2. **Vote:** http://localhost:8081/vote/143594
   - Select option → Submit
   
3. **View Results:** http://localhost:8081/analytics?code=143594
   - See real-time updates when others vote
   
4. **Test Real-time:**
   - Open Analytics page in browser A
   - Open Vote page in browser B (incognito)
   - Vote in B → See instant update in A

### API Testing với Swagger

- PollService: https://localhost:5001/swagger
- VoteService: https://localhost:5002/swagger

---

## 🐛 Troubleshooting

### MySQL Connection Error
```
MySqlConnector.MySqlException: Unable to connect to any of the specified MySQL hosts
```

**Fix:**
1. Verify MySQL host accessible: `mysql -h YOUR_HOST -u YOUR_USER -p`
2. Check connection string in `appsettings.json`
3. Ensure databases exist (tự động tạo nếu chưa có)

### ERR_EMPTY_RESPONSE từ Gateway
**Cause:** Services chạy HTTPS nhưng `ocelot.json` config HTTP

**Fix:** Đã fix trong `ocelot.json`:
```json
{
  "DownstreamScheme": "https",
  "DangerousAcceptAnyServerCertificateValidator": true
}
```

### SignalR Connection Failed
**Cause:** CORS hoặc wrong URL

**Fix:** Check `client/.env.local`:
```env
VUE_APP_VOTE_SERVICE_URL=https://localhost:5002
```

---

## 📝 Configuration Files

### Local Development

| File | Purpose |
|------|---------|
| `appsettings.json` | Database connection, service URLs |
| `ocelot.json` | Gateway routing (localhost) |
| `client/.env.local` | Frontend API URLs |

### Production/Docker

| File | Purpose |
|------|---------|
| `.env` | Docker environment variables |
| `ocelot.Production.json` | Gateway routing (container names) |
| `docker-compose.yml` | Service orchestration |

---

## 🤝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open Pull Request

---

## 📄 License

This project is licensed under the MIT License.

---

## 👨‍💻 Author

**Your Name**
- GitHub: [@yourusername](https://github.com/yourusername)
- Email: your.email@example.com

---

## 🙏 Acknowledgments

- ASP.NET Core Documentation
- Vue.js Documentation
- Ocelot Gateway
- SignalR Documentation
- Tailwind CSS

---

**Built with ❤️ using ASP.NET Core, Vue 3, and SignalR**
