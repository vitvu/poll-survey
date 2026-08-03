# 📊 Poll & Survey Builder — Complete Documentation

> **Real-time polling app with ASP.NET Core microservices, Vue 3, SignalR, and Docker**

---

## 📑 Table of Contents

1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Technology Stack](#technology-stack)
4. [Database Schema](#database-schema)
5. [Backend Services](#backend-services)
6. [API Gateway](#api-gateway)
7. [Frontend Application](#frontend-application)
8. [Real-Time Features](#real-time-features)
9. [Installation & Setup](#installation--setup)
10. [Docker Deployment](#docker-deployment)
11. [Migration Between Machines](#migration-between-machines)
12. [Testing Scenarios](#testing-scenarios)
13. [Project Structure](#project-structure)

---

## 🎯 Project Overview

**Poll & Survey Builder** là ứng dụng tạo và quản lý poll real-time không cần đăng nhập. Người dùng có thể tạo poll với 4 loại câu hỏi (Multiple Choice, Yes/No, Rating, Open Text), chia sẻ link/QR code, và xem kết quả cập nhật trực tiếp qua SignalR WebSocket.

### ✨ Key Features

- ✅ **Không cần đăng nhập** — voter chỉ cần browser token
- ✅ **4 loại câu hỏi** — Multiple Choice, Yes/No, Rating (1-5 sao), Open Text
- ✅ **Real-time results** — SignalR broadcast kết quả ngay khi có người vote
- ✅ **Poll expiry** — tự động hoặc custom deadline
- ✅ **QR code sharing** — tạo QR cho mỗi poll
- ✅ **Creator dashboard** — xem analytics, đóng/xóa poll
- ✅ **Microservices architecture** — PollService + VoteService + Gateway
- ✅ **Docker containerized** — triển khai dễ dàng với docker-compose

### 🎓 Assignment Requirements Met

| Requirement | Implementation |
|------------|---------------|
| ASP.NET Core Web API | ✅ 3 services (Poll, Vote, Gateway) |
| Vue SPA | ✅ Vue 3 + Vue Router |
| Relational Database | ✅ SQL Server với 2 DB (PollDB, VoteDB) |
| REST API | ✅ Tất cả endpoint theo chuẩn REST |
| SignalR Real-time | ✅ VoteHub broadcast kết quả live |
| One vote per user | ✅ VoterToken + localStorage |
| Poll expiry | ✅ Custom deadline hoặc no limit |
| Unit tests | ⚠️ Chưa implement (optional) |

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLIENT (Vue 3 SPA)                         │
│                        http://localhost:8081                        │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               │ HTTP/HTTPS + SignalR WebSocket
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    OcelotGateway (API Gateway)                      │
│                      https://localhost:5000                         │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │  Routes:                                                       │ │
│  │  /api/polls/*    → PollService:5001                           │ │
│  │  /api/votes/*    → VoteService:5002                           │ │
│  │  /hubs/vote      → VoteService:5002  (SignalR)                │ │
│  └───────────────────────────────────────────────────────────────┘ │
└───────────────────┬──────────────────────────┬──────────────────────┘
                    │                          │
         ┌──────────┴──────────┐    ┌─────────┴──────────┐
         │                     │    │                     │
         ↓                     │    ↓                     │
┌─────────────────┐            │ ┌─────────────────┐     │
│  PollService    │            │ │  VoteService    │     │
│  :5001          │            │ │  :5002          │     │
│                 │            │ │                 │     │
│  Controllers:   │            │ │  Controllers:   │     │
│  - Polls CRUD   │            │ │  - Submit vote  │     │
│  - Validation   │            │ │  - Get results  │     │
│                 │            │ │                 │     │
│  Database:      │            │ │  Hubs:          │     │
│  - PollDB       │            │ │  - VoteHub      │     │
│    • Polls      │            │ │    (SignalR)    │     │
│    • Options    │            │ │                 │     │
│                 │            │ │  Database:      │     │
│                 │◄───────────┼─┤  - VoteDB       │     │
│                 │ Inter-     │ │    • Votes      │     │
│                 │ service    │ │                 │     │
│                 │ calls      │ │                 │     │
└─────────────────┘            │ └─────────────────┘     │
                               │                         │
                               └─────────┬───────────────┘
                                         │
                                         ↓
                               ┌──────────────────┐
                               │  SQL Server 2022 │
                               │    :1433         │
                               │                  │
                               │  Databases:      │
                               │  - PollDB        │
                               │  - VoteDB        │
                               └──────────────────┘
```

### 🔄 Request Flow Example: User Votes

1. **User clicks "Submit Vote"** trên VoteView.vue
2. **Frontend gọi** `POST https://localhost:5000/api/votes` (qua Gateway)
3. **Ocelot Gateway** forward request → `VoteService:5002/api/votes`
4. **VotesController** nhận request:
   - Validate `VoterToken` + `PollCode` chưa vote
   - Gọi `PollService:5001/api/polls/check/{code}` xác minh poll hợp lệ
   - Lưu vote vào `VoteDB.Votes` table
   - Query tổng votes từ DB
   - **Broadcast qua SignalR** → `VoteHub.Clients.Group("poll_{code}").SendAsync("VoteUpdated")`
5. **SignalR push** kết quả mới về **tất cả client** đang xem trang Analytics
6. **AnalyticsView.vue** nhận event `VoteUpdated` → cập nhật bar chart không cần reload

---

## 🛠️ Technology Stack

### Backend (.NET 8)

| Package | Version | Purpose | Why Use It |
|---------|---------|---------|------------|
| **ASP.NET Core** | 8.0 | Web API framework | Modern, cross-platform, high performance |
| **Entity Framework Core** | 8.0 | ORM for SQL Server | Code-first DB, migrations, LINQ queries |
| **Microsoft.EntityFrameworkCore.SqlServer** | 8.0 | SQL Server provider | Connect EF Core to SQL Server |
| **Microsoft.EntityFrameworkCore.Design** | 8.0 | Migration tools | Enable `dotnet ef migrations` commands |
| **Newtonsoft.Json** | 8.0 | JSON serializer | Custom datetime format (UTC timezone) |
| **SignalR** | Built-in | WebSocket library | Real-time push notifications to clients |
| **Swashbuckle (Swagger)** | 6.6.2 | API documentation | Auto-generate OpenAPI spec for testing |
| **Ocelot** | Latest | API Gateway | Route aggregation, single entry point |

**Cài đặt packages:**
```bash
# PollService
cd PollService
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson
dotnet add package Swashbuckle.AspNetCore

# VoteService
cd VoteService
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Swashbuckle.AspNetCore

# OcelotGateway
cd OcelotGateway
dotnet add package Ocelot
```

### Frontend (Vue 3)

| Package | Version | Purpose | Install Command |
|---------|---------|---------|-----------------|
| **vue** | 3.2.13 | Core framework | `npm install vue@3.2.13` |
| **vue-router** | 4.6.4 | SPA routing | `npm install vue-router@4.6.4` |
| **axios** | 1.19.0 | HTTP client | `npm install axios@1.19.0` |
| **@microsoft/signalr** | 10.0.0 | SignalR client | `npm install @microsoft/signalr@10.0.0` |
| **vue-toastification** | 2.0.0-rc.5 | Toast notifications | `npm install vue-toastification@2.0.0-rc.5` |
| **qrcode** | 1.5.4 | QR code generator | `npm install qrcode@1.5.4` |
| **lucide-vue-next** | 1.0.0 | Icon library | `npm install lucide-vue-next@1.0.0` |
| **tailwindcss** | 3.4.19 | CSS framework | `npm install -D tailwindcss@3.4.19` |

**Setup frontend từ đầu:**
```bash
cd client
npm install
npm run serve   # Dev mode: http://localhost:8080
npm run build   # Production build
```

---

## 📊 Database Schema

### 🗄️ PollDB (PollService)

#### **Table: Polls**
Lưu thông tin poll (câu hỏi, loại, trạng thái, deadline)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| **Id** | int | PK, Identity(1,1) | Auto-increment primary key |
| **Code** | nvarchar(max) | NOT NULL | 6-digit unique code (e.g., "143594") |
| **Question** | nvarchar(max) | NOT NULL | Poll question text |
| **QuestionType** | nvarchar(max) | NOT NULL | "Multiple Choice" \| "Yes / No" \| "Rating" \| "Open Text" |
| **Status** | nvarchar(max) | NOT NULL, Default: "Active" | "Active" or "Closed" |
| **ExpireAt** | datetime2 | NOT NULL | Poll deadline (UTC) |
| **CreatedAt** | datetime2 | NOT NULL | Creation timestamp (UTC) |

#### **Table: Options**
Lưu các lựa chọn của poll (chỉ dùng cho Multiple Choice/Yes-No)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| **Id** | int | PK, Identity(1,1) | Auto-increment primary key |
| **PollId** | int | FK → Polls.Id, NOT NULL | Foreign key to Polls table |
| **Text** | nvarchar(max) | NOT NULL | Option text (e.g., "Vue", "React") |

**Relationship:** `Polls` 1 → N `Options` (Cascade delete: xóa poll → xóa tất cả options)

**Index:** `IX_Options_PollId` trên cột `PollId` để tăng tốc JOIN query

---

### 🗳️ VoteDB (VoteService)

#### **Table: Votes**
Lưu mỗi phiếu vote của user

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| **Id** | int | PK, Identity(1,1) | Auto-increment primary key |
| **PollCode** | nvarchar(max) | NOT NULL | Poll code (link với Polls.Code qua string, không FK) |
| **OptionId** | int | NOT NULL | ID option được chọn (0 nếu Rating/Open Text) |
| **VoteValue** | nvarchar(max) | NOT NULL | "1"-"5" (Rating) hoặc free text (Open Text) |
| **VoterToken** | nvarchar(max) | NOT NULL | Browser token để chặn vote 2 lần |
| **CreatedAt** | datetime2 | NOT NULL | Vote timestamp |

**Không có FK giữa VoteDB và PollDB** vì 2 service độc lập, chỉ link qua `PollCode` string.

**Duplicate prevention:** Backend query `WHERE PollCode = @code AND VoterToken = @token` → nếu có row → reject vote.

---

### 🔗 Relationship Diagram

```
PollDB:
┌────────────────┐
│     Polls      │
├────────────────┤
│ Id (PK)        │─┐
│ Code           │ │
│ Question       │ │
│ QuestionType   │ │        ┌────────────────┐
│ Status         │ │        │    Options     │
│ ExpireAt       │ │        ├────────────────┤
│ CreatedAt      │ └───────→│ Id (PK)        │
└────────────────┘  1:N     │ PollId (FK)    │
                            │ Text           │
                            └────────────────┘

VoteDB:
┌────────────────┐
│     Votes      │     ⚠️ Không có FK, link qua PollCode string
├────────────────┤
│ Id (PK)        │
│ PollCode       │ ────────╳ (No FK, inter-service link)
│ OptionId       │
│ VoteValue      │
│ VoterToken     │
│ CreatedAt      │
└────────────────┘
```

---

## 🔌 Backend Services

### 🏢 PollService (Port 5001)

**Trách nhiệm:** Quản lý polls và options (CRUD operations)

#### **Endpoints**

| Method | Route | Request Body | Response | Logic |
|--------|-------|--------------|----------|-------|
| **POST** | `/api/polls` | `{ code, question, questionType, expireAt, options[] }` | `201 Created` + poll object | Tạo poll mới. Backend tự generate options nếu Yes/No. Validate: question not empty, expireAt > now, code unique. |
| **GET** | `/api/polls/code/{code}` | - | `200 OK` + poll with options | Lấy full thông tin poll + options. Used by: Analytics page. |
| **GET** | `/api/polls/check/{code}` | - | `200 OK` + poll object (nếu active & not expired)<br>`400 Bad Request` (nếu closed/expired)<br>`404 Not Found` (nếu không tồn tại) | Validate poll hợp lệ trước khi vote. VoteService gọi endpoint này. |
| **PUT** | `/api/polls/code/{code}` | `{ status, question, expireAt }` | `204 No Content` | Update poll. Nếu đổi status → "Closed", gọi VoteService để broadcast SignalR. |
| **DELETE** | `/api/polls/code/{code}` | - | `204 No Content` | Xóa poll + cascade delete options. Sau đó gọi VoteService để xóa tất cả votes của poll này. |

#### **Inter-Service Calls**


PollService gọi VoteService khi:
1. **Update poll status = "Closed"** → `POST https://localhost:5002/api/votes/broadcast-poll-closed` để SignalR broadcast event `PollClosed`
2. **Delete poll** → `DELETE https://localhost:5002/api/votes/by-poll-code/{code}` để xóa tất cả votes

**Hardcoded URL:** `const string voteServiceUrl = "https://localhost:5002";` trong PollsController.cs

---

### 🗳️ VoteService (Port 5002)

**Trách nhiệm:** Quản lý votes, real-time broadcast qua SignalR

#### **Endpoints**

| Method | Route | Request Body | Response | Logic |
|--------|-------|--------------|----------|-------|
| **POST** | `/api/votes` | `{ pollCode, voterToken, optionId, voteValue }` | `200 OK`<br>`400 Bad Request` (already voted / poll invalid) | 1. Check duplicate: `PollCode + VoterToken` đã vote chưa<br>2. Validate poll: gọi PollService `/check/{code}`<br>3. Lưu vote vào DB<br>4. Query tổng votes<br>5. **SignalR broadcast** `VoteUpdated` event |
| **GET** | `/api/votes/result/{pollCode}` | - | `200 OK` + `[{ optionId, voteCount }]` | Trả về số phiếu của từng option (Multiple Choice/Yes-No). Used by: AnalyticsView để vẽ bar chart. |
| **GET** | `/api/votes/total/{pollCode}` | - | `200 OK` + `{ pollCode, totalVotes }` | Trả về tổng số phiếu. Used by: AnalyticsView stat card. |
| **GET** | `/api/votes/list/{pollCode}` | - | `200 OK` + `[{ optionId, voteValue, createdAt }]` | Trả về list từng phiếu (Rating/Open Text). Used by: AnalyticsView để hiển thị stars/text responses. |
| **DELETE** | `/api/votes/by-poll-code/{pollCode}` | - | `204 No Content` | Xóa tất cả votes của poll. Được gọi bởi PollService khi delete poll. |
| **POST** | `/api/votes/broadcast-poll-closed` | `{ pollCode }` | `200 OK` | SignalR broadcast event `PollClosed`. Được gọi bởi PollService khi đóng poll. |

#### **SignalR Hub: VoteHub**

**Endpoint:** `wss://localhost:5002/hubs/vote`

**Server Methods (client gọi):**
- `JoinPollRoom(pollCode)` — Add client vào group `poll_{code}`
- `LeavePollRoom(pollCode)` — Remove client khỏi group

**Client Events (server push):**
- `VoteUpdated` — Payload: `{ pollCode, totalVotes, voteResults: [{ optionId, voteCount }] }`
- `PollClosed` — Payload: `{ pollCode, status: "Closed" }`

**Flow:**
1. AnalyticsView mount → gọi `connection.invoke('JoinPollRoom', '123456')`
2. User khác vote → VotesController broadcast: `Clients.Group("poll_123456").SendAsync("VoteUpdated", data)`
3. AnalyticsView nhận event → `onVoteUpdated(data)` → update bar chart

---

### 🌐 OcelotGateway (Port 5000)

**Trách nhiệm:** API Gateway, route requests đến đúng service

#### **Route Configuration**

**Development (`ocelot.json`):**
```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/polls/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5001 }]
    },
    {
      "UpstreamPathTemplate": "/api/votes/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5002 }]
    },
    {
      "UpstreamPathTemplate": "/hubs/vote",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5002 }]
    }
  ]
}
```

**Production (`ocelot.Production.json`):** Thay `localhost:500X` → `poll-service:8080` và `vote-service:8080`

**Request Example:**
```
Client: GET https://localhost:5000/api/polls/code/143594
  ↓
Ocelot: GET https://localhost:5001/api/Polls/code/143594
  ↓
PollService: Return poll data
```

**CORS:** `AllowAnyOrigin` để frontend localhost:8080 gọi được

**Static Files:** Serve built Vue app từ `wwwroot/` (file `index.html` fallback cho SPA routing)

---

## 🎨 Frontend Application (Vue 3)

### 📁 File Structure

```
client/
├── public/
│   └── index.html              # HTML template (chứa <div id="app">)
├── src/
│   ├── assets/                 # Images, fonts
│   ├── views/                  # Page components
│   │   ├── HomeView.vue        # Trang chủ: Join/Create poll
│   │   ├── CreatePollView.vue  # Form tạo poll
│   │   ├── VoteView.vue        # Trang bỏ phiếu
│   │   └── AnalyticsView.vue   # Dashboard creator (results, QR, close/delete)
│   ├── router/
│   │   └── index.js            # Vue Router config
│   ├── api.js                  # Axios instance + API wrapper
│   ├── voterToken.js           # Generate/get browser token
│   ├── usePollHub.js           # SignalR composable
│   ├── App.vue                 # Root component
│   └── main.js                 # App entry point
├── package.json
└── Dockerfile
```

---

### 📄 Key Frontend Files

#### **1. `src/main.js` — App Entry Point**

```javascript
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import './assets/main.css'  // Tailwind CSS

createApp(App)
  .use(router)                // Enable routing
  .use(Toast, { timeout: 3000 })  // Toast notifications
  .mount('#app')              // Mount to <div id="app"> in index.html
```

**Flow:** `index.html` → load `main.js` → create Vue app → mount vào DOM → render `App.vue` → `<router-view>` hiển thị page

---

#### **2. `src/api.js` — HTTP Client**

**Purpose:** Centralize API calls, handle errors, configure base URL

```javascript
import axios from 'axios'

const apiClient = axios.create({
  baseURL: 'https://localhost:5000',  // Ocelot Gateway
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000  // 10s timeout
})

// Interceptor: Chuẩn hóa error message
apiClient.interceptors.response.use(
  res => res,
  err => {
    const msg = err.response?.data?.message || err.message || 'Server error'
    return Promise.reject(new Error(msg))
  }
)

export const pollApi = {
  getPollByCode: code => apiClient.get(`/api/polls/code/${code}`),
  checkPoll: code => apiClient.get(`/api/polls/check/${code}`),
  createPoll: data => apiClient.post('/api/polls', data),
  updatePoll: (code, data) => apiClient.put(`/api/polls/code/${code}`, data),
  deletePoll: code => apiClient.delete(`/api/polls/code/${code}`),
  submitVote: data => apiClient.post('/api/votes', data),
  getVoteResults: code => apiClient.get(`/api/votes/result/${code}`),
  getVoteTotal: code => apiClient.get(`/api/votes/total/${code}`),
  getVoteList: code => apiClient.get(`/api/votes/list/${code}`)
}
```

**Usage trong component:**
```javascript
import { pollApi } from '../api'
const response = await pollApi.checkPoll('143594')
console.log(response.data)  // poll object
```

---

#### **3. `src/voterToken.js` — Browser Fingerprint**

**Purpose:** Tạo token định danh thiết bị để chặn vote 2 lần (không cần login)

```javascript
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token')
  if (!token) {
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      randomPart += Math.floor(Math.random() * 10)  // Random 8 chữ số
    }
    token = 'voter_' + randomPart  // e.g., "voter_47291038"
    localStorage.setItem('poll_voter_token', token)
  }
  return token
}
```

**Hạn chế:** Xóa localStorage → mất token → vote lại được. Nhưng đủ yêu cầu "no login required".

---

#### **4. `src/usePollHub.js` — SignalR Composable**

**Purpose:** Kết nối SignalR Hub để nhận real-time updates

```javascript
import { ref, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

export function usePollHub(pollCode, onVoteUpdated) {
  const connected = ref(false)
  let connection = null

  const start = async () => {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:5002/hubs/vote')
      .withAutomaticReconnect([0, 1000, 3000, 5000])  // Retry delays
      .build()

    connection.on('VoteUpdated', data => {
      if (data.pollCode === pollCode) onVoteUpdated(data)
    })

    await connection.start()
    await connection.invoke('JoinPollRoom', pollCode)
    connected.value = true
  }

  const stop = async () => {
    if (connection) {
      await connection.invoke('LeavePollRoom', pollCode)
      await connection.stop()
    }
  }

  onUnmounted(stop)  // Auto cleanup khi component unmount

  return { connected, start, stop }
}
```

**Usage trong AnalyticsView:**
```javascript
const { connected, start } = usePollHub(pollCode, (data) => {
  totalVotes.value = data.totalVotes  // Update UI ngay
})

onMounted(() => start())
```

---

### 🖼️ Frontend Pages

#### **1. HomeView.vue** — Landing Page

**Features:**
- Form "Join Poll": Nhập 6-digit code → validate → redirect `/vote/{code}`
- Card "Create Poll": Link to `/create`
- Section "How It Works" (3 steps)

**Logic:**
```javascript
const joinPoll = async () => {
  if (code.value.length < 6) {
    codeError.value = 'Please enter all 6 digits'
    return
  }
  await pollApi.checkPoll(code.value)  // Validate poll tồn tại
  router.push(`/vote/${code.value}`)
}
```

---

#### **2. CreatePollView.vue** — Poll Creation Form

**Form Fields:**
- `question` — Required text input
- `questionType` — Radio: Multiple Choice / Yes-No / Rating / Open Text
- `expireAt` — "No Limit" (100 năm) hoặc custom datetime-local input
- `options[]` — Dynamic list (2-6 options, chỉ Multiple Choice)

**Create Flow:**
1. User fill form → bấm "Create Poll"
2. Generate random 6-digit code: `Math.floor(100000 + Math.random() * 900000)`
3. POST `/api/polls` với payload:
```json
{
  "code": "143594",
  "question": "Favorite framework?",
  "questionType": "Multiple Choice",
  "expireAt": "2026-08-10T15:30:00Z",
  "options": [
    { "text": "Vue" },
    { "text": "React" }
  ]
}
```
4. Backend trả về poll object với `Id`
5. Lưu code vào `localStorage.createdPolls`: `["143594", ...]`
6. Redirect `/analytics?code=143594`

**Yes/No Auto-Options:** Nếu chọn Yes/No, backend tự generate 2 options "Yes" và "No", frontend không gửi `options[]`.

---

#### **3. VoteView.vue** — Voting Page

**URL Patterns:**
- `/vote/143594` → Load poll với code `143594`
- `/vote` → Hiện form nhập code thủ công

**Voting Flow:**

**Step 1: Load Poll**
```javascript
onMounted(async () => {
  const response = await pollApi.checkPoll(pollCode)  // Validate poll active
  poll.value = response.data

  // Check localStorage: đã vote chưa
  if (localStorage.getItem(`voted_${pollCode}`) === 'true') {
    alreadyVoted.value = true  // Hiện "Already Voted"
  }
})
```

**Step 2: User Chọn/Nhập Vote**
- **Multiple Choice/Yes-No:** Click radio button → `selectedOptionId.value = option.id`
- **Rating:** Click sao → `voteValue.value = '4'`
- **Open Text:** Type text → `voteValue.value = 'My response'`

**Step 3: Submit Vote**
```javascript
const submitVote = async () => {
  // Validate
  if (questionType === 'Multiple Choice' && !selectedOptionId.value) {
    hasSubmitError.value = true; return
  }

  await pollApi.submitVote({
    pollCode: poll.value.code,
    voterToken: getVoterToken(),       // "voter_47291038"
    optionId: selectedOptionId.value || 0,
    voteValue: voteValue.value
  })

  localStorage.setItem(`voted_${pollCode}`, 'true')  // Mark voted
  voteSubmitted.value = true  // Show "Vote Recorded!"
}
```

**Backend Response:**
- `200 OK` → Success
- `400 "already voted"` → Show "Already Voted"
- `400 "poll invalid"` → Show "Poll Closed"

---

#### **4. AnalyticsView.vue** — Creator Dashboard

**Access Control:** Chỉ creator (có code trong `localStorage.createdPolls`) mới xem được. Nếu không → "Access Denied".

**Features:**
- **Header Card:** Code, status badge, question, QR code thumbnail, share link
- **Stats Card:** Total votes (số lớn)
- **Results Section:** Bar chart (Multiple Choice/Yes-No) / Star list (Rating) / Text responses (Open Text)
- **Actions:** Copy link, Stop poll, Delete poll

**Real-Time Updates:**
```javascript
const { connected: isHubConnected, start: startHub } = usePollHub(pollCode, async (hubData) => {
  totalVotes.value = hubData.totalVotes  // Update stat card
  await loadResults()                     // Re-fetch vote breakdown
})

onMounted(() => {
  loadPoll()      // GET /api/polls/code/{code}
  loadResults()   // GET /api/votes/total + /result hoặc /list
  startHub()      // Connect SignalR
})
```

**Stop Poll:**
```javascript
const stopPoll = async () => {
  await pollApi.updatePoll(pollCode, { ...poll.value, status: 'Closed' })
  poll.value.status = 'Closed'  // Update local
}
```
Backend nhận PUT request → set status = "Closed" → gọi VoteService broadcast `PollClosed` event.

**Delete Poll:**
```javascript
const deletePoll = async () => {
  await pollApi.deletePoll(pollCode)  // Backend cascade delete options + votes
  
  // Remove from localStorage
  const codes = JSON.parse(localStorage.getItem('createdPolls') || '[]')
  localStorage.setItem('createdPolls', JSON.stringify(codes.filter(c => c !== pollCode)))
  
  router.push('/')
}
```

**QR Code Generation:**
```javascript
import QRCode from 'qrcode'

const renderQRCode = async (canvasElement, size) => {
  await QRCode.toCanvas(canvasElement, shareLink(), {
    width: size,  // 100 (thumbnail) or 320 (modal)
    margin: 2,
    color: { dark: '#1e293b', light: '#ffffff' }
  })
}
```

---

## ⚡ Real-Time Features (SignalR)

### 🔄 How SignalR Works

**SignalR** = ASP.NET Core library cho WebSocket real-time communication. Server push data đến client mà không cần client poll.

**Alternative (without SignalR):** Client phải polling mỗi 2s: `setInterval(() => fetchResults(), 2000)` → lãng phí bandwidth, delay 2s.

**With SignalR:** Server broadcast ngay khi có vote mới → client nhận instant, không delay, không polling.

---

### 🏗️ SignalR Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    VoteService (Backend)                        │
│                                                                 │
│  VotesController.SubmitVote():                                 │
│    1. Save vote to DB                                          │
│    2. Query total votes                                        │
│    3. IHubContext<VoteHub>.Clients                             │
│         .Group("poll_143594")                                  │
│         .SendAsync("VoteUpdated", { totalVotes, results })     │
│                                                                 │
│  VoteHub (SignalR Hub):                                        │
│    - JoinPollRoom(pollCode) → Groups.AddToGroupAsync()         │
│    - LeavePollRoom(pollCode) → Groups.RemoveFromGroupAsync()   │
└────────────────────┬────────────────────────────────────────────┘
                     │ WebSocket (wss://)
                     │ Persistent Connection
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│              Client A (AnalyticsView.vue)                       │
│  connection.on('VoteUpdated', data => {                        │
│    totalVotes.value = data.totalVotes  // Update UI ngay       │
│  })                                                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│              Client B (VoteView.vue) — Just voted               │
│  submitVote() → POST /api/votes → Backend broadcast            │
└─────────────────────────────────────────────────────────────────┘
```

---

### 📡 SignalR Connection Flow

**1. Client Connects (AnalyticsView mounts):**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:5002/hubs/vote')
  .withAutomaticReconnect([0, 1000, 3000, 5000])  // Retry delays (ms)
  .build()

connection.on('VoteUpdated', (data) => {
  console.log('New vote!', data)  // { pollCode, totalVotes, voteResults }
})

connection.on('PollClosed', (data) => {
  console.log('Poll closed!', data.pollCode)
})

await connection.start()  // Open WebSocket connection
await connection.invoke('JoinPollRoom', '143594')  // Subscribe to poll group
```

**2. User B Votes:**
```javascript
// VoteView.vue
await pollApi.submitVote({ pollCode: '143594', voterToken: 'voter_xxx', optionId: 2 })
```

**3. Backend Broadcast:**
```csharp
// VotesController.cs
await _signalRHubContext.Clients
    .Group($"poll_{voteData.PollCode}")
    .SendAsync("VoteUpdated", new {
        pollCode = voteData.PollCode,
        totalVotes = totalVotesForThisPoll,
        voteResults = allVotesForThisPoll  // [{ optionId: 1, voteCount: 5 }, ...]
    });
```

**4. Client A Receives:**
```javascript
// AnalyticsView.vue
connection.on('VoteUpdated', (data) => {
  if (data.pollCode === '143594') {
    totalVotes.value = data.totalVotes  // Vue reactivity → UI update tức thì
    // Hoặc gọi loadResults() để fetch breakdown mới
  }
})
```

---

### 🔌 SignalR vs HTTP Polling

| Feature | SignalR WebSocket | HTTP Polling (setInterval) |
|---------|-------------------|----------------------------|
| **Latency** | ~50ms (instant push) | 2000ms (poll mỗi 2s) |
| **Bandwidth** | Chỉ gửi khi có data mới | Gửi request liên tục dù không có data |
| **Scalability** | 1 connection/client | N requests/minute/client |
| **Server Load** | Thấp (event-driven) | Cao (query DB mỗi poll) |
| **Complexity** | SignalR Hub setup | Simple `setInterval` |

**Conclusion:** SignalR phù hợp cho real-time apps (chat, voting, dashboard), HTTP polling phù hợp cho data ít thay đổi.

---

### 🛡️ SignalR Reconnection Strategy

**Problem:** User mất mạng 5s → WebSocket disconnect → mất events.

**Solution:** `withAutomaticReconnect([0, 1000, 3000, 5000])`

**Behavior:**
1. Connection lost → retry ngay (0ms)
2. Fail → retry sau 1s
3. Fail → retry sau 3s
4. Fail → retry sau 5s
5. Fail → stop reconnecting → `connection.onclose()` fired

**Handle reconnection:**
```javascript
connection.onreconnecting(() => {
  connected.value = false  // Show "Connecting..." badge
})

connection.onreconnected(async () => {
  connected.value = true
  await connection.invoke('JoinPollRoom', pollCode)  // Re-join group
})
```

**Fallback Polling:** Nếu SignalR offline lâu, dùng `setInterval` polling:
```javascript
const fallbackInterval = setInterval(() => {
  if (!connected.value) {
    loadResults()  // Fetch từ REST API thay vì chờ SignalR
  }
}, 6000)

onUnmounted(() => clearInterval(fallbackInterval))
```

---

## 🚀 Installation & Setup

### 📋 Prerequisites

| Software | Version | Download Link | Purpose |
|----------|---------|---------------|---------|
| **.NET SDK** | 8.0+ | https://dotnet.microsoft.com/download | Build & run backend services |
| **Node.js** | 20+ | https://nodejs.org | Build Vue frontend |
| **SQL Server** | 2022 | https://www.microsoft.com/sql-server/sql-server-downloads | Database (hoặc dùng Docker) |
| **Docker Desktop** | Latest | https://www.docker.com/products/docker-desktop | Container orchestration (optional) |
| **Git** | Latest | https://git-scm.com/downloads | Clone repository |

---

### 🖥️ Local Development Setup (Without Docker)

#### **Step 1: Clone Repository**
```bash
git clone https://github.com/yourusername/poll-survey.git
cd poll-survey
```

#### **Step 2: Setup SQL Server**

**Option A: SQL Server LocalDB (Windows only)**
```bash
# Đã có sẵn với Visual Studio
# Connection string: Server=(localdb)\mssqllocaldb;...
```

**Option B: SQL Server Express**
```bash
# Download từ: https://www.microsoft.com/sql-server/sql-server-downloads
# Chọn "Express" edition → Install
# Connection string: Server=localhost\SQLEXPRESS;...
```

**Option C: Docker SQL Server**
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

#### **Step 3: Setup Backend Services**

**PollService:**
```bash
cd PollService

# Restore packages
dotnet restore

# Apply migrations (tạo database + tables)
dotnet ef database update

# Run service
dotnet run  # https://localhost:5001
```

**VoteService:**
```bash
cd VoteService
dotnet restore
dotnet ef database update
dotnet run  # https://localhost:5002
```

**OcelotGateway:**
```bash
cd OcelotGateway
dotnet restore
dotnet run  # https://localhost:5000
```

#### **Step 4: Setup Frontend**
```bash
cd client

# Install dependencies
npm install

# Run dev server
npm run serve  # http://localhost:8080
```

**⚠️ CORS Issue:** Nếu frontend `localhost:8080` không gọi được backend `localhost:5000`, check CORS config trong `Program.cs`:
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required cho SignalR
    });
});
```

#### **Step 5: Test Application**
1. Mở browser: `http://localhost:8080`
2. Click "Create Poll" → Fill form → Submit
3. Copy poll code → Open incognito window → "Join Poll" → Vote
4. Quay lại tab đầu (Analytics page) → xem kết quả update real-time

---

### 🐳 Docker Development Setup

#### **Step 1: Create `.env` File**
```bash
# Copy từ .env.example
cp .env.example .env

# Edit .env:
SA_PASSWORD=YourStrong@Passw0rd
```

**⚠️ Password Requirements:**
- Ít nhất 8 ký tự
- Phải có: chữ hoa, chữ thường, số, ký tự đặc biệt
- Ví dụ hợp lệ: `MyP@ssw0rd123`

#### **Step 2: Build & Run Containers**
```bash
# Build all services
docker-compose build

# Start all containers (detached mode)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all containers
docker-compose down
```

**Containers Started:**
- `sql-server` (port 1433) — SQL Server 2022
- `poll-service` (port 5001) — PollService
- `vote-service` (port 5002) — VoteService
- `ocelot-gateway` (port 5000) — API Gateway
- `poll-client` (port 8081) — Vue frontend

#### **Step 3: Access Application**
- **Frontend:** http://localhost:8081
- **Gateway API:** http://localhost:5000
- **Swagger (PollService):** http://localhost:5001/swagger
- **Swagger (VoteService):** http://localhost:5002/swagger

#### **Step 4: Database Migrations**

Migrations tự động chạy khi container start lần đầu (nếu có config trong `Dockerfile`). Nếu không:

```bash
# Connect vào container
docker exec -it poll-service bash

# Run migration
dotnet ef database update

# Exit container
exit
```

---

### 🔧 Troubleshooting

#### **Issue 1: SQL Server Connection Failed**

**Error:** `Login failed for user 'sa'`

**Fix:**
1. Check `SA_PASSWORD` trong `.env` đủ mạnh chưa
2. Restart SQL Server container:
```bash
docker-compose restart sqlserver
docker-compose logs sqlserver  # Check logs
```

#### **Issue 2: Port Already In Use**

**Error:** `bind: address already in use`

**Fix:**
```bash
# Windows: Check port 5000
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID_NUMBER> /F

# Hoặc đổi port trong docker-compose.yml:
ports:
  - "5010:8080"  # Map host 5010 → container 8080
```

#### **Issue 3: SignalR Connection Failed**

**Error:** `Failed to start connection: Error: WebSocket failed`

**Fix:**
1. Check VoteService đang chạy: `curl https://localhost:5002/hubs/vote`
2. Check CORS config trong `VoteService/Program.cs`:
```csharp
.WithOrigins("http://localhost:8080", "https://localhost:5173")
.AllowCredentials()  // Phải có dòng này
```

#### **Issue 4: Frontend Build Error**

**Error:** `Module not found: Can't resolve 'axios'`

**Fix:**
```bash
cd client
rm -rf node_modules package-lock.json
npm install
npm run serve
```

---

## 📦 Migration Between Machines

### 🔄 Cách Chuyển Project Sang Máy Khác

#### **Option 1: Git Clone (Recommended)**

**Trên máy cũ:**
```bash
cd poll-survey
git add .
git commit -m "Update project"
git push origin main
```

**Trên máy mới:**
```bash
git clone https://github.com/yourusername/poll-survey.git
cd poll-survey

# Backend
cd PollService && dotnet restore && dotnet ef database update
cd ../VoteService && dotnet restore && dotnet ef database update
cd ../OcelotGateway && dotnet restore

# Frontend
cd client && npm install
```

#### **Option 2: Copy Folder (No Git)**

**Files cần copy:**
- Toàn bộ source code (không cần `bin/`, `obj/`, `node_modules/`)
- `.env` file (chứa `SA_PASSWORD`)

**Trên máy mới:**
```bash
# Backend: Restore packages
cd PollService && dotnet restore
cd ../VoteService && dotnet restore
cd ../OcelotGateway && dotnet restore

# Frontend: Install dependencies
cd client && npm install

# Database: Apply migrations
cd PollService && dotnet ef database update
cd ../VoteService && dotnet ef database update
```

#### **Option 3: Docker Image Export/Import**

**Trên máy cũ: Export images**
```bash
docker save -o poll-services.tar poll-service vote-service ocelot-gateway poll-client
```

**Copy file `poll-services.tar` sang máy mới**

**Trên máy mới: Import images**
```bash
docker load -i poll-services.tar
docker-compose up -d
```

---

### 🗃️ Database Migration Giữa Các Máy

#### **Export Database (SQL Server)**

**Option A: SQL Script**
```bash
# Từ SQL Server Management Studio (SSMS):
# Right-click database → Tasks → Generate Scripts
# Chọn "Schema and data" → Export to .sql file

# Hoặc dùng CLI:
sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -d PollDB \
  -Q "SELECT * FROM Polls" -o polls.csv
```

**Option B: Backup File**
```sql
-- Trong SSMS hoặc Azure Data Studio:
BACKUP DATABASE PollDB TO DISK = 'C:\backup\PollDB.bak'
BACKUP DATABASE VoteDB TO DISK = 'C:\backup\VoteDB.bak'
```

**Restore trên máy mới:**
```sql
RESTORE DATABASE PollDB FROM DISK = 'C:\backup\PollDB.bak'
RESTORE DATABASE VoteDB FROM DISK = 'C:\backup\VoteDB.bak'
```

#### **Reset Database (Clean Start)**

Nếu muốn xóa data cũ và tạo DB mới:

```bash
# Xóa database hiện tại
dotnet ef database drop --force

# Tạo lại từ migrations
dotnet ef database update
```

---

### 🔑 Environment Variables Cần Thiết

**Backend (`appsettings.json`):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PollDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Services": {
    "PollServiceUrl": "https://localhost:5001",
    "VoterServiceUrl": "https://localhost:5002"
  }
}
```

**Frontend (`client/src/api.js`):**
```javascript
const apiClient = axios.create({
  baseURL: 'https://localhost:5000',  // Ocelot Gateway URL
  // ...
})
```

**Docker (`.env`):**
```bash
SA_PASSWORD=YourStrong@Passw0rd
```

**⚠️ Lưu ý:** Khi deploy production, đổi URLs thành domain thực (ví dụ: `https://api.myapp.com`)

---

## 🧪 Testing Scenarios

### Test Case 1: Create Multiple Choice Poll

**Steps:**
1. Mở `http://localhost:8080` → Click "Create Poll"
2. Question: "Favorite framework?"
3. Type: Multiple Choice
4. Options: Vue, React, Angular, Svelte
5. Duration: Set deadline 1 ngày sau
6. Click "Create Poll"

**Expected:**
- ✅ Redirect to `/analytics?code=XXXXXX`
- ✅ Poll code được generate (6 digits)
- ✅ Code lưu vào `localStorage.createdPolls`
- ✅ QR code hiển thị
- ✅ "Total Votes" = 0
- ✅ Status badge = "Live"

**Database Check:**
```sql
SELECT * FROM Polls WHERE Code = 'XXXXXX'
-- ExpireAt phải = now + 1 day
-- Status = 'Active'

SELECT * FROM Options WHERE PollId = (SELECT Id FROM Polls WHERE Code = 'XXXXXX')
-- Phải có 4 rows: Vue, React, Angular, Svelte
```

---

### Test Case 2: Vote on Poll (Multiple Users)

**Steps:**
1. User A: Copy poll link từ Analytics page
2. User B: Mở incognito window → paste link → chọn "Vue" → Submit
3. User C: Mở browser khác → paste link → chọn "React" → Submit
4. User A: Check Analytics page (không reload)

**Expected:**
- ✅ User B: "Vote Recorded!" screen
- ✅ `localStorage.voted_XXXXXX = "true"` (User B)
- ✅ User C: Vote thành công
- ✅ **User A: Bar chart tự update** (SignalR push)
  - Vue: 1 vote
  - React: 1 vote
  - Total Votes: 2

**Database Check:**
```sql
SELECT * FROM Votes WHERE PollCode = 'XXXXXX'
-- 2 rows với VoterToken khác nhau
-- OptionId = 1 (Vue), OptionId = 2 (React)
```

---

### Test Case 3: Prevent Duplicate Vote

**Steps:**
1. User B đã vote → Reload trang `/vote/XXXXXX`
2. Chọn option khác → Submit

**Expected:**
- ✅ localStorage check: `voted_XXXXXX = "true"` → Show "Already Voted" ngay
- ❌ Không cho submit nữa

**Nếu clear localStorage:**
1. User B clear browser data → Reload `/vote/XXXXXX`
2. Form hiện lại (vì localStorage mất)
3. Chọn option → Submit

**Expected:**
- ❌ Backend reject: `400 "You have already voted"`
- ✅ Frontend show "Already Voted"

**Database Check:**
```sql
SELECT COUNT(*) FROM Votes WHERE PollCode = 'XXXXXX' AND VoterToken = 'voter_12345678'
-- Phải = 1 (không tăng lên)
```

---

### Test Case 4: Close Poll

**Steps:**
1. Creator mở Analytics page → Click "Stop"
2. Confirm modal → Click "Stop Now"
3. User B reload voting page

**Expected:**
- ✅ Creator: Status badge đổi sang "Closed", nút "Stop" biến mất
- ✅ **SignalR broadcast** `PollClosed` event → tất cả client xem Analytics nhận event
- ✅ User B: Hiện banner "This poll has ended", form bị ẩn
- ❌ User C cố submit vote → Backend reject: `400 "Poll is closed"`

**Database Check:**
```sql
SELECT Status FROM Polls WHERE Code = 'XXXXXX'
-- Status = 'Closed'
```

---

### Test Case 5: Delete Poll

**Steps:**
1. Creator mở Analytics page → Click "Delete"
2. Confirm modal → Click "Delete"

**Expected:**
- ✅ Backend xóa poll từ `Polls` table
- ✅ **Cascade delete:** Tất cả `Options` của poll bị xóa
- ✅ Backend gọi VoteService → xóa tất cả `Votes`
- ✅ Code bị xóa khỏi `localStorage.createdPolls`
- ✅ Redirect về homepage
- ❌ User B reload voting page → `404 Poll Not Found`

**Database Check:**
```sql
SELECT * FROM Polls WHERE Code = 'XXXXXX'    -- 0 rows
SELECT * FROM Options WHERE PollId = ...     -- 0 rows
SELECT * FROM Votes WHERE PollCode = 'XXXXXX' -- 0 rows
```

---

### Test Case 6: Rating Poll

**Steps:**
1. Create poll → Type: Rating → Question: "Rate this app (1-5 stars)"
2. User A: Click 4 sao → Submit
3. User B: Click 5 sao → Submit
4. Creator: Check Analytics

**Expected:**
- ✅ Analytics hiển thị list các phiếu với sao đầy
- ✅ Vote được lưu với `VoteValue = "4"` và `"5"`
- ✅ `OptionId = 0` (không dùng options cho Rating)

**Database Check:**
```sql
SELECT VoteValue FROM Votes WHERE PollCode = 'XXXXXX'
-- 2 rows: "4", "5"
```

---

### Test Case 7: Open Text Poll

**Steps:**
1. Create poll → Type: Open Text → Question: "What features do you want?"
2. User A: Type "Dark mode" → Submit
3. User B: Type "Mobile app" → Submit

**Expected:**
- ✅ Analytics hiển thị list text responses
- ✅ Vote lưu với `VoteValue = "Dark mode"`, `"Mobile app"`
- ✅ `OptionId = 0`

**Database Check:**
```sql
SELECT VoteValue FROM Votes WHERE PollCode = 'XXXXXX'
-- "Dark mode", "Mobile app"
```

---

### Test Case 8: Poll Expiry

**Steps:**
1. Create poll → Duration: Custom → Set 1 phút sau
2. Wait 1 phút
3. User A reload voting page

**Expected:**
- ✅ Status badge đổi "Closed" (check `ExpireAt <= DateTime.UtcNow`)
- ✅ Form ẩn, hiện banner "This poll has ended"
- ❌ Submit vote → Backend reject: `400 "Poll has expired"`

**⚠️ Lưu ý:** Backend check expiry trong `ValidatePoll` endpoint, không tự động update `Status` column. Status chỉ đổi khi creator bấm "Stop".

---

## 📂 Project Structure & File Explanations

### Backend Services

```
PollService/
├── Controllers/
│   └── PollsController.cs       # CRUD endpoints cho polls
├── Data/
│   └── PollDbContext.cs         # EF Core DbContext (Polls, Options tables)
├── Models/
│   ├── Poll.cs                  # Poll entity (Id, Code, Question, ...)
│   └── Option.cs                # Option entity (Id, PollId, Text)
├── Migrations/
│   └── 20260724153429_InitialCreate.cs  # Migration tạo tables Polls & Options
├── Program.cs                   # Entry point, configure services (EF, CORS, Swagger)
├── appsettings.json             # Connection strings, config
├── Dockerfile                   # Multi-stage build: SDK → publish → runtime
└── PollService.csproj           # Project file, NuGet packages

VoteService/
├── Controllers/
│   └── VotesController.cs       # Submit vote, get results, delete votes
├── Hubs/
│   └── VoteHub.cs               # SignalR Hub (JoinPollRoom, LeavePollRoom)
├── Data/
│   └── VoteDbContext.cs         # EF Core DbContext (Votes table)
├── Models/
│   └── Vote.cs                  # Vote entity (Id, PollCode, OptionId, VoterToken, ...)
├── Migrations/
│   └── 20260724153440_InitialCreate.cs  # Migration tạo table Votes
├── Program.cs                   # Entry point, configure SignalR, EF, CORS
├── appsettings.json             # Connection string + inter-service URLs
├── Dockerfile                   # Multi-stage build
└── VoteService.csproj           # Project file, NuGet packages

OcelotGateway/
├── Program.cs                   # Load ocelot.json, configure Ocelot middleware
├── ocelot.json                  # Development routes (localhost:500X)
├── ocelot.Production.json       # Production routes (docker service names)
├── Dockerfile                   # Multi-stage build
└── OcelotGateway.csproj         # Project file, Ocelot package
```

---

### Migration Files Chi Tiết

**Migrations** = EF Core's way để version control database schema. Mỗi migration = 1 snapshot thay đổi DB.

#### **Cấu trúc Migration File:**

```csharp
// PollService/Migrations/20260724153429_InitialCreate.cs
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Code tạo tables khi chạy "dotnet ef database update"
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),  // Auto-increment
                Code = table.Column<string>(nullable: false),
                // ...
            });
        
        migrationBuilder.CreateTable(
            name: "Options",
            columns: table => new { /* ... */ },
            constraints: table =>
            {
                table.ForeignKey(
                    name: "FK_Options_Polls_PollId",
                    column: x => x.PollId,
                    principalTable: "Polls",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade  // Xóa poll → xóa options
                );
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Code rollback (xóa tables) khi chạy "dotnet ef database update <previous_migration>"
        migrationBuilder.DropTable(name: "Options");
        migrationBuilder.DropTable(name: "Polls");
    }
}
```

#### **Tại sao cần Migrations?**

1. **Version Control Schema:** Git track được schema changes
2. **Team Collaboration:** Dev A tạo migration → push → Dev B pull → apply migration → DB sync
3. **Production Deploy:** Apply migrations lên production DB mà không cần manually run SQL scripts

#### **Commands:**

```bash
# Tạo migration mới (khi thay đổi Model)
dotnet ef migrations add AddStatusColumn

# Apply tất cả pending migrations
dotnet ef database update

# Rollback về migration cụ thể
dotnet ef database update InitialCreate

# Xóa migration chưa apply
dotnet ef migrations remove

# Generate SQL script (không apply trực tiếp)
dotnet ef migrations script > migration.sql
```

---

### Frontend Structure

```
client/
├── public/
│   ├── index.html               # HTML entry point (<div id="app">)
│   └── favicon.ico
├── src/
│   ├── assets/
│   │   └── main.css             # Tailwind CSS, custom styles
│   ├── views/
│   │   ├── HomeView.vue         # Trang chủ: Join/Create cards
│   │   ├── CreatePollView.vue   # Form tạo poll (question, type, options, expiry)
│   │   ├── VoteView.vue         # Voting interface (4 loại question)
│   │   └── AnalyticsView.vue    # Creator dashboard (results, QR, actions)
│   ├── router/
│   │   └── index.js             # Vue Router config (/, /create, /vote/:code, /analytics)
│   ├── api.js                   # Axios instance + pollApi methods
│   ├── voterToken.js            # getVoterToken() - generate/retrieve browser token
│   ├── usePollHub.js            # SignalR composable (connect, subscribe, disconnect)
│   ├── App.vue                  # Root component (<router-view>, transition wrapper)
│   └── main.js                  # App entry (createApp, use router/toast, mount)
├── .env                         # Environment variables (VUE_APP_API_BASE_URL)
├── package.json                 # Dependencies, scripts (serve, build, lint)
├── vue.config.js                # Vue CLI config (optional custom settings)
├── tailwind.config.js           # TailwindCSS config (colors, themes)
└── Dockerfile                   # Multi-stage: build Vue app → nginx serve
```

#### **File Chi Tiết:**

**`main.js`** — Vue app initialization
```javascript
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import Toast from 'vue-toastification'

createApp(App)
  .use(router)     // Enable routing
  .use(Toast)      // Enable toast notifications
  .mount('#app')   // Mount vào <div id="app"> trong index.html
```

**`App.vue`** — Root wrapper
```vue
<template>
  <div class="app-wrap">
    <router-view v-slot="{ Component }">
      <transition name="fade" mode="out-in">
        <component :is="Component" />  <!-- Hiện page tương ứng route -->
      </transition>
    </router-view>
  </div>
</template>
```

**`router/index.js`** — Routing config
```javascript
const routes = [
  { path: '/', component: () => import('../views/HomeView.vue') },
  { path: '/create', component: () => import('../views/CreatePollView.vue') },
  { path: '/vote/:code?', component: () => import('../views/VoteView.vue') },
  { path: '/analytics', component: () => import('../views/AnalyticsView.vue') },
  { path: '/:pathMatch(.*)*', redirect: '/' }  // Catch-all 404
]
```

**`api.js`** — HTTP wrapper
```javascript
// Axios instance với base URL + timeout + error interceptor
const apiClient = axios.create({ baseURL: 'https://localhost:5000', timeout: 10000 })

// Wrapper functions
export const pollApi = {
  checkPoll: code => apiClient.get(`/api/polls/check/${code}`),
  createPoll: data => apiClient.post('/api/polls', data),
  // ...
}
```

---

### Docker Files

#### **`Dockerfile` (Backend Services)**

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PollService/PollService.csproj", "PollService/"]
RUN dotnet restore "PollService/PollService.csproj"
COPY . .
WORKDIR "/src/PollService"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PollService.dll"]
```

**Tại sao multi-stage?**
- Stage 1 (build): Cần full SDK (>1GB) để compile
- Stage 2 (final): Chỉ cần runtime (~200MB) → image nhẹ hơn

#### **`Dockerfile` (Frontend)**

```dockerfile
# Stage 1: Build Vue app
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci  # Clean install (faster than npm install)
COPY . .
RUN npm run build  # Output: dist/

# Stage 2: Serve với nginx
FROM nginx:stable-alpine
COPY --from=build /app/dist /usr/share/nginx/html
# Config nginx: fallback to index.html for SPA routing
RUN printf 'server {\n  listen 80;\n  location / {\n    try_files $uri /index.html;\n  }\n}' \
  > /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

#### **`docker-compose.yml`**

```yaml
version: "3.9"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "${SA_PASSWORD}"  # Từ .env file
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${SA_PASSWORD}' -C -Q 'SELECT 1'"]
      interval: 15s
      retries: 10

  poll-service:
    build:
      context: .
      dockerfile: PollService/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=PollDB;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
    ports: ["5001:8080"]
    depends_on:
      sqlserver: { condition: service_healthy }  # Chờ SQL Server ready

  vote-service:
    build: { context: ., dockerfile: VoteService/Dockerfile }
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=VoteDB;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
      Services__PollServiceUrl: "http://poll-service:8080"
    ports: ["5002:8080"]
    depends_on:
      sqlserver: { condition: service_healthy }
      poll-service: { condition: service_started }

  gateway:
    build: { context: ., dockerfile: OcelotGateway/Dockerfile }
    environment:
      ASPNETCORE_ENVIRONMENT: Production  # Load ocelot.Production.json
    ports: ["5000:8080"]
    depends_on: [poll-service, vote-service]

  client:
    build:
      context: ./client
      args:
        VUE_APP_API_BASE_URL: "http://localhost:5000"
    ports: ["8081:80"]
    depends_on: [gateway]
```

**Dependency Graph:**
```
sqlserver (start first)
  ↓
poll-service (wait sql healthy)
  ↓
vote-service (wait sql + poll-service)
  ↓
gateway (wait both services)
  ↓
client (wait gateway)
```

---

## 🎯 Summary

### ✅ What This Project Demonstrates

| Skill | Implementation |
|-------|----------------|
| **Microservices** | 3 độc lập services (Poll, Vote, Gateway) giao tiếp qua HTTP |
| **API Gateway** | Ocelot routing, CORS, single entry point |
| **Real-Time** | SignalR WebSocket push notifications |
| **Database Design** | Normalized schema, foreign keys, migrations |
| **SPA Frontend** | Vue 3 Composition API, router, state management |
| **Docker** | Multi-stage builds, compose orchestration, healthchecks |
| **REST API** | Proper HTTP verbs, status codes, JSON payloads |
| **Security** | VoterToken duplicate prevention, CORS config |

### 📚 Learning Resources

- **ASP.NET Core:** https://learn.microsoft.com/aspnet/core
- **Entity Framework Core:** https://learn.microsoft.com/ef/core
- **SignalR:** https://learn.microsoft.com/aspnet/core/signalr
- **Vue 3:** https://vuejs.org/guide
- **Ocelot:** https://ocelot.readthedocs.io
- **Docker:** https://docs.docker.com/get-started

---

## 📝 License

MIT License - Free to use for educational purposes

---

## 👤 Author

**Your Name**
- GitHub: [@yourusername](https://github.com/yourusername)
- Email: your.email@example.com

---

**⭐ If you found this helpful, please star the repo!**

