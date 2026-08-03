# ✅ Pre-Deployment Checklist

Use this checklist trước khi deploy để đảm bảo everything configured correctly.

---

## 📋 Local Development Checklist

### Database Configuration
- [ ] MySQL host accessible (test: `mysql -h HOST -u USER -p`)
- [ ] `PollService/appsettings.json` có connection string đúng
- [ ] `VoteService/appsettings.json` có connection string đúng
- [ ] Database credentials correct (host, port, user, password)

### Service Configuration
- [ ] `OcelotGateway/ocelot.json` uses `https` scheme
- [ ] `VoteService/appsettings.json` has `Services__PollServiceUrl = https://localhost:5001`
- [ ] All `appsettings.json` include frontend URL in `AllowedOrigins`

### Frontend Configuration
- [ ] `client/.env.local` exists
- [ ] `VUE_APP_API_BASE_URL=https://localhost:5000`
- [ ] `VUE_APP_VOTE_SERVICE_URL=https://localhost:5002`

### Testing
- [ ] All 3 backend services start without errors
- [ ] Frontend starts on http://localhost:8081
- [ ] Can create poll
- [ ] Can vote on poll
- [ ] Real-time updates working (test với 2 browsers)
- [ ] Swagger accessible (https://localhost:5001/swagger, :5002/swagger)

---

## 🐳 Docker Deployment Checklist

### Configuration Files
- [ ] `.env` file exists (copy from `.env.example`)
- [ ] `POLL_DB_CONNECTION` configured correctly
- [ ] `VOTE_DB_CONNECTION` configured correctly
- [ ] `GATEWAY_PUBLIC_URL=http://localhost:5000`
- [ ] `VOTE_SERVICE_PUBLIC_URL=http://localhost:5002`
- [ ] `FRONTEND_URL=http://localhost:8081`

### Docker Files
- [ ] `docker-compose.yml` exists
- [ ] `OcelotGateway/ocelot.Production.json` exists
- [ ] All Dockerfiles exist:
  - `PollService/Dockerfile`
  - `VoteService/Dockerfile`
  - `OcelotGateway/Dockerfile`
  - `client/Dockerfile`
- [ ] `.dockerignore` exists

### Docker Build & Run
- [ ] `docker-compose build` succeeds
- [ ] `docker-compose up -d` starts all 4 services
- [ ] `docker-compose ps` shows all services "Up"
- [ ] No errors in logs: `docker-compose logs`

### Testing
- [ ] Frontend accessible: http://localhost:8081
- [ ] Gateway accessible: http://localhost:5000
- [ ] Can create poll
- [ ] Can vote
- [ ] Real-time updates working

---

## ☁️ Render Deployment Checklist

### Prerequisites
- [ ] Code pushed to GitHub
- [ ] GitHub repository public hoặc Render connected
- [ ] MySQL database accessible from internet
- [ ] MySQL firewall allows connections from Render

### Files Prepared
- [ ] `render.yaml` exists in project root
- [ ] `OcelotGateway/ocelot.Production.json` ready for update
- [ ] `.env.example` updated với production notes

### Render Setup
- [ ] Render account created
- [ ] GitHub repository connected to Render
- [ ] Blueprint imported from `render.yaml`

### Service Configuration

**poll-service:**
- [ ] Environment variable `ConnectionStrings__DefaultConnection` set
- [ ] Environment variable `AllowedOrigins__0` set to frontend URL
- [ ] Service status = "Live"

**vote-service:**
- [ ] Environment variable `ConnectionStrings__DefaultConnection` set
- [ ] Environment variable `Services__PollServiceUrl` set
- [ ] Environment variable `AllowedOrigins__0` set to frontend URL
- [ ] Service status = "Live"

**ocelot-gateway:**
- [ ] Environment variables configured
- [ ] Service status = "Live"
- [ ] Can access via browser (test endpoint)

**poll-client:**
- [ ] Build environment variables set:
  - `VUE_APP_API_BASE_URL`
  - `VUE_APP_VOTE_SERVICE_URL`
- [ ] Service status = "Live"
- [ ] Can access via browser

### ocelot.Production.json Updated
- [ ] All URLs updated với Render URLs:
  - `BaseUrl` = gateway URL
  - `DownstreamHostAndPorts` = service URLs
  - Port = `443` for HTTPS
- [ ] Changes committed and pushed to GitHub
- [ ] Services auto-redeployed

### Testing Production
- [ ] Frontend loads
- [ ] Can create poll
- [ ] Can vote
- [ ] Real-time updates working
- [ ] No CORS errors in browser console
- [ ] No 500 errors in Render logs

---

## 🔐 Security Checklist

### Credentials
- [ ] `.env` file in `.gitignore`
- [ ] No hardcoded passwords in code
- [ ] MySQL credentials only in:
  - `appsettings.json` (local dev, gitignored if needed)
  - `.env` (Docker, gitignored)
  - Render Environment Variables (encrypted)

### CORS
- [ ] `AllowedOrigins` NOT using `AllowAnyOrigin` in production
- [ ] Only specific frontend URL(s) allowed
- [ ] No `*` wildcards in production CORS

### HTTPS
- [ ] Local dev uses HTTPS (development certificates)
- [ ] Render automatically provisions SSL
- [ ] Frontend uses `https://` URLs in production

---

## 📊 Performance Checklist

### Database
- [ ] Indexes created (auto via EF Core)
- [ ] Connection pooling enabled (default)
- [ ] Connection string has reasonable timeout

### Services
- [ ] No memory leaks (test local first)
- [ ] Proper error handling
- [ ] Logging configured

### Frontend
- [ ] Production build optimized (`npm run build`)
- [ ] No console.log in production
- [ ] Assets minified

---

## 📝 Documentation Checklist

### README Files
- [ ] `README.md` updated with:
  - Project overview
  - Setup instructions
  - Architecture diagram
  - Tech stack
- [ ] `QUICKSTART.md` has working 5-minute guide
- [ ] `RENDER_DEPLOYMENT.md` has complete deploy steps

### Code Comments
- [ ] Complex logic commented
- [ ] API endpoints documented
- [ ] Configuration explained

### Repository
- [ ] `.gitignore` includes:
  - `.env`
  - `bin/`, `obj/`
  - `node_modules/`
  - `dist/`, `build/`
- [ ] `LICENSE` file (optional)
- [ ] Contributing guidelines (optional)

---

## 🚀 Final Steps Before Deploy

### Local Testing
1. [ ] Clean test từ scratch:
   ```bash
   git clone <repo>
   # Configure appsettings.json
   # Run all services
   # Test all features
   ```

### Docker Testing
1. [ ] Clean Docker test:
   ```bash
   docker-compose down -v
   docker-compose build --no-cache
   docker-compose up -d
   # Test all features
   ```

### Pre-Render Deploy
1. [ ] Commit all changes
2. [ ] Push to GitHub
3. [ ] Verify all files present in GitHub
4. [ ] Ready to import to Render!

---

## 🎉 Deploy!

Once all checkboxes ✅, you're ready to deploy:

1. Import `render.yaml` blueprint
2. Configure environment variables
3. Wait for builds to complete
4. Test production URLs
5. 🎊 Celebrate!

---

**Good luck with your deployment! 🚀**
