# ⚡ Quick Start Guide

Get Poll Survey running locally in 5 minutes!

---

## 📋 Prerequisites

- ✅ .NET 8 SDK installed
- ✅ Node.js 18+ installed
- ✅ MySQL database accessible (local hoặc external)

---

## 🚀 5-Minute Setup

### Step 1: Clone & Configure (1 min)

```bash
git clone https://github.com/YOUR_USERNAME/poll-survey.git
cd poll-survey
```

**Edit Database Connection:**

`PollService/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=103.153.64.170;Port=3306;Database=tautotik1mooo_PollDB;User=tautotik1mooo_amd;Password=vudinhviet123;AllowPublicKeyRetrieval=true;"
  }
}
```

`VoteService/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=103.153.64.170;Port=3306;Database=tautotik1mooo_VoteDB;User=tautotik1mooo_amd;Password=vudinhviet123;AllowPublicKeyRetrieval=true;"
  }
}
```

> **Note:** Database tables tự động được tạo khi service start lần đầu.

---

### Step 2: Start Backend Services (2 min)

**Mở 3 terminals:**

**Terminal 1 - Gateway:**
```bash
cd OcelotGateway
dotnet run
```
Wait for: `Now listening on: https://localhost:5000`

**Terminal 2 - PollService:**
```bash
cd PollService
dotnet run
```
Wait for: `Now listening on: https://localhost:5001`

**Terminal 3 - VoteService:**
```bash
cd VoteService
dotnet run
```
Wait for: `Now listening on: https://localhost:5002`

---

### Step 3: Start Frontend (2 min)

**Terminal 4 - Client:**
```bash
cd client
npm install
npm run dev
```
Wait for: `Local: http://localhost:8081`

---

### Step 4: Open & Test!

1. **Open:** http://localhost:8081
2. **Create Poll:**
   - Click "Create Poll"
   - Fill form → Submit
   - Copy poll code (e.g., 143594)
3. **Vote:**
   - Open http://localhost:8081/vote/143594
   - Select option → Vote
4. **View Results:**
   - Go to Analytics page
   - See real-time updates!

---

## ✅ Verify Everything Works

### Check Services Running

Open these URLs in browser:

- ✅ Frontend: http://localhost:8081
- ✅ Gateway: https://localhost:5000/swagger (hoặc API endpoint)
- ✅ PollService: https://localhost:5001/swagger
- ✅ VoteService: https://localhost:5002/swagger

### Test Real-Time

1. Open Analytics page trong Browser A
2. Open Vote page trong Browser B (incognito)
3. Vote ở B → See instant update ở A ✨

---

## 🐛 Troubleshooting

### MySQL Connection Error

```
MySqlConnector.MySqlException: Unable to connect...
```

**Fix:**
1. Verify MySQL accessible: `mysql -h YOUR_HOST -u YOUR_USER -p`
2. Check connection string correct in `appsettings.json`

### Port Already in Use

```
Failed to bind to address... Address already in use
```

**Fix:**
```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:5001 | xargs kill -9
```

### ERR_EMPTY_RESPONSE

**Cause:** Gateway không kết nối được services

**Fix:** Ensure all 3 backend services running on correct ports (5000, 5001, 5002).

---

## 📚 Next Steps

- Read full [README.md](./README.md) for architecture details
- Check [RENDER_DEPLOYMENT.md](./RENDER_DEPLOYMENT.md) to deploy to cloud
- Explore API docs: https://localhost:5001/swagger

---

## 🎯 Project Structure

```
poll-survey/
├── PollService/       → https://localhost:5001
├── VoteService/       → https://localhost:5002
├── OcelotGateway/     → https://localhost:5000
└── client/            → http://localhost:8081
```

---

## 💡 Tips

- Use **Visual Studio** để run multiple projects cùng lúc
- Use **VS Code** với split terminal cho 4 services
- Database tables tự động tạo khi service start (EnsureCreated)
- Dùng Swagger để test API trước khi test qua frontend

---

**Enjoy building polls! 🚀**
