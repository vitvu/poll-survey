# 📊 Poll Survey — Real-time Polling Application

> Ứng dụng tạo và tham gia khảo sát/bình chọn trực tuyến theo thời gian thực.  
> Không cần đăng ký tài khoản. Kết quả cập nhật tức thì qua WebSocket (SignalR).

---

## 📋 Mục lục

1. [Tổng quan kiến trúc](#-tổng-quan-kiến-trúc)
2. [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
3. [Cấu trúc thư mục dự án](#-cấu-trúc-thư-mục-dự-án)
4. [Cơ sở dữ liệu — Schema chi tiết](#-cơ-sở-dữ-liệu--schema-chi-tiết)
5. [Backend Services — Chi tiết từng service](#-backend-services--chi-tiết-từng-service)
6. [API Gateway — Ocelot](#-api-gateway--ocelot)
7. [Frontend — Vue.js 3](#-frontend--vuejs-3)
8. [Luồng hoạt động (Flow)](#-luồng-hoạt-động-flow)
9. [Realtime với SignalR](#-realtime-với-signalr)
10. [Xác thực người dùng — VoterToken](#-xác-thực-người-dùng--votertoken)
11. [Hướng dẫn chạy dự án](#-hướng-dẫn-chạy-dự-án)
12. [Ports & URLs](#-ports--urls)

---

## 🏗 Tổng quan kiến trúc

Dự án sử dụng kiến trúc **Microservices** — mỗi chức năng chính được tách thành một service độc lập, giao tiếp qua **API Gateway** (Ocelot).

```
┌─────────────────────────────────────────────────────────┐
│                    Vue.js 3 Client                       │
│              http://localhost:8080                       │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP (Axios)
                       ▼
┌─────────────────────────────────────────────────────────┐
│               OcelotGateway  :5000                       │
│         (API Gateway + Static file server)               │
│  Nhận toàn bộ request từ client, forward đến service    │
│  tương ứng dựa trên URL pattern                         │
└────────┬──────────────────┬──────────────────┬──────────┘
         │                  │                  │
         ▼                  ▼                  ▼
┌────────────────┐ ┌────────────────┐ ┌────────────────────┐
│  PollService   │ │  VoteService   │ │  AnalyticsService  │
│    :5001       │ │    :5002       │ │      :5003         │
│                │ │                │ │                    │
│  Quản lý Poll  │ │  Quản lý Vote  │ │  Lưu log thống kê  │
│  & Options     │ │  + SignalR Hub │ │  (fire & forget)   │
└───────┬────────┘ └───────┬────────┘ └────────────────────┘
        │                  │
        ▼                  ▼
   [PollDB]           [VoteDB]            [AnalyticsDB]
  SQL Server         SQL Server            SQL Server
```

> **Lưu ý quan trọng:** Kết nối SignalR từ frontend đi **trực tiếp** đến VoteService `:5002`  
> (không qua Ocelot), vì Ocelot có giới hạn với WebSocket long-polling.

---

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Frontend | Vue.js 3 (Composition API), Vue Router 4, TailwindCSS 3 |
| HTTP Client | Axios |
| Realtime | Microsoft SignalR (`@microsoft/signalr`) |
| UI Icons | Lucide Vue |
| Notifications | vue-toastification |
| QR Code | qrcode.js |
| Backend | ASP.NET Core 8 (Web API) |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB cho dev) |
| API Gateway | Ocelot 23.3.3 |
| JSON Serializer | Newtonsoft.Json (xử lý DateTime timezone) |

---

## 📁 Cấu trúc thư mục dự án

```
poll-survey/
│
├── PollSurvey.sln              ← Solution file Visual Studio (chứa tất cả project)
│
├── OcelotGateway/              ← API Gateway, điểm vào duy nhất từ client
│   ├── Program.cs
│   ├── ocelot.json             ← Cấu hình routing: URL → Service nào, port nào
│   └── OcelotGateway.csproj   ← Copy client/dist vào wwwroot khi build
│
├── PollService/                ← Service quản lý Poll & Options
│   ├── Controllers/
│   │   └── PollsController.cs
│   ├── Models/
│   │   ├── Poll.cs
│   │   └── Option.cs
│   ├── Data/
│   │   └── PollDbContext.cs
│   ├── Migrations/             ← EF Core migration (tạo bảng DB tự động)
│   ├── appsettings.json        ← Connection string → PollDB
│   └── Program.cs
│
├── VoteService/                ← Service quản lý Vote + SignalR Hub
│   ├── Controllers/
│   │   └── VotesController.cs
│   ├── Hubs/
│   │   └── VoteHub.cs          ← SignalR Hub xử lý realtime
│   ├── Models/
│   │   └── Vote.cs
│   ├── Data/
│   │   └── VoteDbContext.cs
│   ├── Migrations/
│   ├── appsettings.json        ← Connection string → VoteDB + URL PollService/Analytics
│   └── Program.cs
│
├── AnalyticsService/           ← Service nhận log vote để thống kê
│   ├── Controllers/
│   │   └── AnalyticsController.cs
│   ├── Models/
│   │   └── Analytics.cs
│   ├── Data/
│   │   └── AnalyticsDbContext.cs
│   ├── Migrations/
│   ├── appsettings.json        ← Connection string → AnalyticsDB
│   └── Program.cs
│
└── client/                     ← Vue.js 3 Frontend
    ├── src/
    │   ├── main.js             ← Entry point, khởi tạo Vue app
    │   ├── App.vue             ← Root component, chứa router-view + fade transition
    │   ├── api.js              ← Axios instance + tất cả hàm gọi API
    │   ├── voterToken.js       ← Tạo/lấy voter token từ localStorage
    │   ├── usePollHub.js       ← Composable kết nối SignalR
    │   ├── router/
    │   │   └── index.js        ← Định nghĩa 4 routes của app
    │   ├── views/
    │   │   ├── HomeView.vue        ← Trang chủ: nhập code / tạo poll
    │   │   ├── CreatePollView.vue  ← Form tạo poll mới
    │   │   ├── VoteView.vue        ← Trang bỏ phiếu
    │   │   └── AnalyticsView.vue   ← Dashboard xem kết quả (chỉ creator)
    │   └── assets/
    │       └── main.css        ← CSS global: design system, components
    ├── package.json
    └── tailwind.config.js
```

---

## 🗄 Cơ sở dữ liệu — Schema chi tiết

Dự án có **3 database riêng biệt**, mỗi service sở hữu DB của mình (nguyên tắc microservices: database per service).

---

### 📦 PollDB — Database của PollService

#### Bảng `Polls`

| Cột | Kiểu | Mô tả |
|---|---|---|
| `Id` | `int` (PK, Auto Increment) | Khóa chính, tự tăng |
| `Code` | `nvarchar(max)` | Mã phòng 6 chữ số (ví dụ: `"482931"`), dùng để chia sẻ |
| `Question` | `nvarchar(max)` | Nội dung câu hỏi của poll |
| `QuestionType` | `nvarchar(max)` | Loại câu hỏi: `"Multiple Choice"` / `"Yes / No"` / `"Rating"` / `"Open Text"` |
| `Status` | `nvarchar(max)` | Trạng thái: `"Active"` (đang mở) / `"Closed"` (đã đóng) |
| `ExpireAt` | `datetime2` | Thời điểm hết hạn. Nếu "No Limit" thì set = 100 năm sau |
| `CreatedAt` | `datetime2` | Thời điểm tạo poll (UTC) |

#### Bảng `Options`

| Cột | Kiểu | Mô tả |
|---|---|---|
| `Id` | `int` (PK, Auto Increment) | Khóa chính |
| `PollId` | `int` (FK → Polls.Id) | Khóa ngoại liên kết với Poll. Cascade delete: xóa Poll → xóa Options |
| `Text` | `nvarchar(max)` | Nội dung lựa chọn (ví dụ: `"Vue.js"`, `"React"`, `"Yes"`, `"No"`) |

> **Lưu ý:** Poll loại `Yes / No` tự động tạo 2 Option (`"Yes"` và `"No"`) phía backend.  
> Poll loại `Rating` và `Open Text` không có Option nào trong bảng này.

---

### 📦 VoteDB — Database của VoteService

#### Bảng `Votes`

| Cột | Kiểu | Mô tả |
|---|---|---|
| `Id` | `int` (PK, Auto Increment) | Khóa chính |
| `PollCode` | `nvarchar(max)` | Mã poll (không dùng FK để giữ loose coupling giữa services) |
| `OptionId` | `int` | ID option được chọn. Với Rating/Open Text thì = `0` |
| `VoteValue` | `nvarchar(max)` | Giá trị vote: `"4"` (sao) hoặc nội dung text. Với Multiple Choice = `""` |
| `VoterToken` | `nvarchar(max)` | Token định danh trình duyệt người vote (từ `localStorage`) |
| `CreatedAt` | `datetime2` | Thời điểm vote |

> **Chống vote 2 lần:** Server kiểm tra `UNIQUE(PollCode, VoterToken)` trước khi lưu.

---

### 📦 AnalyticsDB — Database của AnalyticsService

#### Bảng `Analytics`

| Cột | Kiểu | Mô tả |
|---|---|---|
| `Id` | `int` (PK, Auto Increment) | Khóa chính |
| `PollCode` | `nvarchar(max)` | Mã poll |
| `OptionId` | `int` | Option được chọn (dùng để tính thống kê sau) |
| `VoteTime` | `datetime2` | Thời điểm ghi nhận |

> Analytics là **write-only log** — VoteService ghi vào sau mỗi vote (fire & forget).  
> Frontend hiện tại không đọc trực tiếp Analytics, nhưng có thể mở rộng sau.

---

## ⚙️ Backend Services — Chi tiết từng service

### 1. PollService (port 5001)

Chịu trách nhiệm toàn bộ vòng đời của một Poll: tạo, đọc, cập nhật, xóa.

#### API Endpoints

| Method | URL | Chức năng |
|---|---|---|
| `GET` | `/api/polls/code/{code}` | Lấy thông tin poll đầy đủ kèm danh sách options |
| `GET` | `/api/polls/check/{code}` | Validate poll: còn tồn tại? còn Active? chưa hết hạn? |
| `GET` | `/api/polls/check-option/{optionId}` | Validate option có tồn tại không |
| `POST` | `/api/polls` | Tạo poll mới |
| `PUT` | `/api/polls/code/{code}` | Cập nhật poll (chủ yếu đóng poll: `status: "Closed"`) |
| `DELETE` | `/api/polls/code/{code}` | Xóa poll và tất cả options (cascade) |

#### Logic đặc biệt khi tạo poll (`POST /api/polls`)

```
1. Validate: câu hỏi không rỗng, expireAt phải ở tương lai
2. Kiểm tra Code không trùng trong DB
3. Tự tạo Options theo loại:
   - "Multiple Choice" → dùng options từ frontend (phải ≥ 2)
   - "Yes / No"        → tự tạo ["Yes", "No"], bỏ qua options frontend gửi
   - "Rating"          → không có options
   - "Open Text"       → không có options
4. Lưu vào PollDB
```

#### Logic khi đóng poll (`PUT /api/polls/code/{code}`)

```
1. Cập nhật status = "Closed" trong DB
2. Gọi HTTP POST đến VoteService: /api/votes/broadcast-poll-closed
3. VoteService phát SignalR event "PollClosed" đến tất cả người đang xem
→ VoteView nhận event → hiện thông báo "Poll has ended"
```

#### Xử lý DateTime/Timezone

PollService dùng `Newtonsoft.Json` với `DateTimeZoneHandling.Utc`:
- Frontend gửi ISO string UTC (ví dụ: `"2026-08-02T07:00:00Z"`)
- Backend lưu và trả về đúng UTC
- Frontend parse `new Date("...Z")` → hiển thị giờ local của người dùng tự động

---

### 2. VoteService (port 5002)

Service phức tạp nhất: xử lý vote, điều phối realtime, giao tiếp với 2 service khác.

#### API Endpoints

| Method | URL | Chức năng |
|---|---|---|
| `POST` | `/api/votes` | Submit phiếu bầu |
| `GET` | `/api/votes/result/{pollCode}` | Kết quả nhóm theo option: `[{optionId, count}]` |
| `GET` | `/api/votes/total/{pollCode}` | Tổng số phiếu: `{pollCode, totalVotes}` |
| `GET` | `/api/votes/list/{pollCode}` | Danh sách từng phiếu (Rating/Open Text): `[{voteValue}]` |
| `POST` | `/api/votes/broadcast-poll-closed` | Nhận lệnh từ PollService, phát SignalR "PollClosed" |

#### Flow xử lý khi submit vote (`POST /api/votes`)

```
Client gửi: { pollCode, voterToken, optionId, voteValue }
        │
        ▼
[1] Kiểm tra dữ liệu đầu vào (pollCode, voterToken không rỗng)
        │
        ▼
[2] Truy vấn VoteDB: tồn tại vote nào có (PollCode = X AND VoterToken = Y)?
    → Có: trả 400 "You have already voted."
    → Không: tiếp tục
        │
        ▼
[3] Gọi HTTP GET PollService: /api/polls/check/{pollCode}
    → Poll không tồn tại / đã đóng / hết hạn: trả 400 lỗi
    → OK: tiếp tục
        │
        ▼
[4] Lưu Vote vào VoteDB
        │
        ▼
[5] Tính kết quả mới: GROUP BY OptionId → danh sách {optionId, count}
        │
        ▼
[6] Broadcast qua SignalR: gửi "VoteUpdated" {pollCode, total, results} 
    đến tất cả client đang join room "poll_{pollCode}"
        │
        ▼
[7] Fire & forget: gửi log đến AnalyticsService (không chờ kết quả)
        │
        ▼
[8] Trả 200 OK về client
```

---

### 3. AnalyticsService (port 5003)

Service đơn giản, hoạt động như **write-only audit log** cho mọi lượt vote.

#### API Endpoints

| Method | URL | Chức năng |
|---|---|---|
| `POST` | `/api/analytics` | Nhận log vote từ VoteService, lưu vào AnalyticsDB |
| `GET` | `/api/analytics/summary/{pollCode}` | Trả về `{totalVotes, topOption}` |

> VoteService gọi `POST /api/analytics` theo kiểu **fire & forget** (không await) — nghĩa là dù Analytics lỗi hay không phản hồi, VoteService vẫn trả kết quả về user bình thường.

---

## 🔀 API Gateway — Ocelot

**Ocelot** là API Gateway đứng trước tất cả backend services. Client **chỉ biết 1 địa chỉ duy nhất** là `https://localhost:5000`, Ocelot sẽ tự forward request đến đúng service.

### Cách Ocelot hoạt động

```
Client gọi:  GET https://localhost:5000/api/polls/check/482931
                 │
                 ▼
         OcelotGateway nhận request
         Tìm route khớp "/api/polls/{everything}"
                 │
                 ▼
         Forward đến: GET https://localhost:5001/api/Polls/check/482931
                 │
                 ▼
         PollService xử lý, trả response
                 │
                 ▼
         Ocelot trả response về client (trong suốt)
```

### Bảng routing (`ocelot.json`)

| Upstream (Client gọi vào :5000) | Downstream (Ocelot forward đến) |
|---|---|
| `GET/POST/PUT/DELETE /api/polls/{everything}` | PollService `:5001` |
| `GET/POST /api/polls` | PollService `:5001` |
| `GET/POST /api/votes/{everything}` | VoteService `:5002` |
| `POST /api/votes` | VoteService `:5002` |
| `GET/POST /api/analytics/{everything}` | AnalyticsService `:5003` |
| `GET/POST/OPTIONS /hubs/vote` | VoteService `:5002` (SignalR handshake) |

### Ngoài routing, OcelotGateway còn làm gì?

OcelotGateway cũng kiêm luôn **static file server** cho Vue.js frontend:

```csharp
// Program.cs của OcelotGateway
app.UseDefaultFiles();   // Serve index.html khi truy cập /
app.UseStaticFiles();    // Serve tất cả file trong wwwroot/
app.MapFallbackToFile("index.html");  // SPA fallback: /vote/123 → index.html
```

Khi build production, Vue build output (`dist/`) được copy vào `wwwroot/` của OcelotGateway thông qua cấu hình trong `.csproj`:

```xml
<Content Include="..\client\**" CopyToOutputDirectory="PreserveNewest">
  <Link>wwwroot\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Content>
```

→ Khi deploy production, **chỉ cần chạy OcelotGateway** là đủ để serve cả frontend lẫn forward API.

---

## 🖥 Frontend — Vue.js 3

### Cách frontend tổ chức

Frontend là **Single Page Application (SPA)** — chỉ có 1 file HTML (`index.html`). Vue Router giả lập chuyển trang bằng cách ẩn/hiện component theo URL, không reload toàn trang.

```
main.js
  └── createApp(App)
        ├── use(router)   ← Điều hướng URL
        ├── use(Toast)    ← Thông báo góc màn hình
        └── mount('#app') ← Gắn vào <div id="app"> trong index.html
```

### Routes

| URL | Component | Mô tả |
|---|---|---|
| `/` | `HomeView.vue` | Trang chủ |
| `/create` | `CreatePollView.vue` | Tạo poll mới |
| `/vote/:code?` | `VoteView.vue` | Bỏ phiếu (`:code?` là tùy chọn) |
| `/analytics?code=XXX` | `AnalyticsView.vue` | Xem kết quả (chỉ creator) |
| `/*` | redirect `→ /` | Bắt tất cả URL không hợp lệ |

---

### View 1: `HomeView.vue` — Trang chủ

**Giao diện:**
- Header hero: tiêu đề + mô tả ngắn
- 2 card dạng lưới:
  - **Card trái "Join Poll":** Ô nhập 6 chữ số (font monospace, letter-spacing lớn) + nút `Join Room`
  - **Card phải "Create Poll":** Nền xanh `--blue`, list tính năng, nút `Create Poll` chuyển sang `/create`
- Section "How It Works": 3 bước có số tròn + chevron phân cách

**Logic:**
```
User nhập code 6 số → bấm Join Room
  → validate độ dài ≥ 6
  → GET /api/polls/check/{code}  (qua Ocelot → PollService)
  → Thành công: router.push('/vote/{code}')
  → Thất bại: hiện lỗi "Poll not found" dưới ô nhập
```

---

### View 2: `CreatePollView.vue` — Tạo Poll

**Giao diện:**
- Input câu hỏi (bắt buộc)
- 4 card chọn loại câu hỏi (2×2 grid):
  - `Multiple Choice` — icon BarChart, chọn 1 trong nhiều
  - `Yes / No` — icon ToggleLeft, chỉ 2 lựa chọn
  - `Star Rating` — icon Star, 1–5 sao
  - `Open Text` — icon MessageSquare, nhập tự do
- Chọn thời hạn:
  - `No Limit` — set expireAt = 100 năm sau
  - `Set Deadline` — hiện `<input type="datetime-local">`
- Khu vực Options (chỉ hiện khi chọn Multiple Choice):
  - Tối thiểu 2, tối đa 6 options
  - Nút `+` thêm, nút `X` xóa (disabled khi còn 2)
- Nút `Create Poll` (spinner khi loading)

**Logic submit:**
```
1. Validate: question không rỗng, Multiple Choice có ≥ 2 options
2. Tạo roomCode ngẫu nhiên: Math.floor(100000 + Math.random() * 900000)
3. Chuyển expireAt từ giờ local → UTC ISO string
4. POST /api/polls {code, question, questionType, expireAt, options}
5. Lưu code vào localStorage['createdPolls'] (để biết mình là creator)
6. router.push('/analytics?code={code}')  → chuyển sang xem kết quả
```

---

### View 3: `VoteView.vue` — Bỏ Phiếu

**Có 5 trạng thái hiển thị:**

| Trạng thái | Điều kiện | Giao diện |
|---|---|---|
| Form nhập code | URL không có code (`/vote`) | Input 6 số + nút Join |
| Form vote | Poll hợp lệ, chưa vote | Câu hỏi + form theo loại |
| Poll đã đóng | `status !== Active` hoặc quá `expireAt` | Banner đỏ "This poll has ended" |
| Đã vote rồi | `localStorage['voted_{code}'] = 'true'` | Card xanh "Already Voted" |
| Vote thành công | Sau khi submit OK | Card xanh "Vote Recorded!" |
| Poll không tìm thấy | API trả lỗi | Card đỏ "Poll Not Found" |

**Form vote theo từng loại:**
- **Multiple Choice / Yes No:** Danh sách radio button tự vẽ (không dùng input radio mặc định). Bấm chọn → border xanh + radio filled
- **Rating:** 5 nút ngôi sao, bấm sao số N → tô vàng sao 1..N
- **Open Text:** `<textarea>` nhập tự do

**Logic submit vote:**
```
1. Validate: đã chọn option / sao / nhập text chưa?
2. POST /api/votes {pollCode, voterToken, optionId, voteValue}
3. Thành công:
   - localStorage['voted_{code}'] = 'true'
   - Hiện "Vote Recorded!"
4. Lỗi "already voted": hiện "Already Voted"
5. Lỗi khác: hiện lỗi form
```

---

### View 4: `AnalyticsView.vue` — Dashboard Kết Quả (Creator)

**Kiểm tra quyền truy cập:**
```
Vào /analytics?code=482931
  → Đọc localStorage['createdPolls'] = '["482931", ...]'
  → Không có code này: hiện màn hình "Access Denied" với icon Lock
  → Có: tiếp tục load
```

**Giao diện (chỉ hiện khi là creator):**

1. **Header Card:**
   - Badge: `{code}` (xanh) + `Open/Closed` (xanh/đỏ, có live-dot) + loại câu hỏi (xám)
   - Tiêu đề câu hỏi
   - Nút: `Copy Link`, `Vote Page` (mở tab mới), `Stop` (đỏ), `Delete` (nguy hiểm)
   - Badge kết nối: `Live` (xanh) / `Connecting...` (xám) — trạng thái SignalR
   - Input hiển thị link chia sẻ + nút copy
   - Ngày tạo + ngày hết hạn
   - **QR Code thumbnail** (100×100px, bấm vào phóng to modal)

2. **Stat Card:** Số tổng phiếu to (font 40px, màu xanh)

3. **Results Card** (cập nhật realtime qua SignalR):
   - **Multiple Choice / Yes No:** Thanh progress bar ngang, mỗi bar = 1 option, độ rộng = `(count/total * 100)%`, animation smooth 0.55s
   - **Rating:** Hiện từng phiếu dưới dạng hàng sao (tô vàng theo giá trị)
   - **Open Text:** Mỗi câu trả lời trong 1 card riêng

4. **Modal QR phóng to** (320×320px)
5. **Modal xác nhận Stop** (confirm trước khi đóng poll)
6. **Modal xác nhận Delete** (confirm trước khi xóa vĩnh viễn)

**Fallback khi SignalR mất kết nối:**
```javascript
// Cứ 6 giây: nếu SignalR offline → tự gọi REST API để lấy kết quả mới
setInterval(() => {
  if (!isHubConnected.value) loadResults()
}, 6000)
```

---

## 🔄 Luồng hoạt động (Flow)

### Flow 1: Tạo Poll

```
Creator                  Vue Frontend              PollService
   │                          │                        │
   │── nhập question ────────>│                        │
   │── chọn loại câu hỏi ───>│                        │
   │── chọn thời hạn ────────>│                        │
   │── nhập options (MC) ────>│                        │
   │── bấm "Create Poll" ───>│                        │
   │                          │── POST /api/polls ────>│
   │                          │   {code, question,     │
   │                          │    questionType,        │
   │                          │    expireAt, options}   │
   │                          │                        │── lưu vào PollDB
   │                          │                        │── tạo Options
   │                          │<── 201 Created {poll} ─│
   │                          │                        │
   │                          │── lưu code vào         │
   │                          │   localStorage         │
   │                          │                        │
   │<── redirect /analytics ──│                        │
```

---

### Flow 2: Tham gia & Vote

```
Voter                    Vue Frontend   OcelotGateway  VoteService   PollService
  │                           │               │              │             │
  │── mở /vote/482931 ───────>│               │              │             │
  │                           │── GET /api/polls/check/── ──>│             │
  │                           │               │              │── forward ─>│
  │                           │               │              │<─ poll data ─│
  │                           │<── poll data ─│              │             │
  │<── hiển thị form vote ────│               │              │             │
  │                           │               │              │             │
  │── chọn option / nhập ────>│               │              │             │
  │── bấm "Submit Vote" ─────>│               │              │             │
  │                           │── POST /api/votes ──────────>│             │
  │                           │   {pollCode, voterToken,     │             │
  │                           │    optionId, voteValue}      │             │
  │                           │                              │── check dup │
  │                           │                              │── GET /api/ ┤
  │                           │                              │   polls/    │
  │                           │                              │   check/    │
  │                           │                              │<── OK ──────│
  │                           │                              │── lưu Vote  │
  │                           │                              │── broadcast │
  │                           │                              │   SignalR   │
  │                           │                              │── analytics │
  │                           │<── 200 OK ──────────────────│             │
  │<── "Vote Recorded!" ──────│               │              │             │
```

---

### Flow 3: Xem kết quả realtime (Creator)

```
Creator            AnalyticsView       VoteService (SignalR)    Voter khác
   │                    │                      │                    │
   │── mở /analytics ──>│                      │                    │
   │                    │── GET poll info ─────────────────────────>│
   │                    │── GET vote total/results ────────────────>│
   │                    │<── data ────────────────────────────────── │
   │<── hiện dashboard ─│                      │                    │
   │                    │── connect SignalR ───>│                    │
   │                    │── JoinPollRoom("482931")                   │
   │                    │                      │                    │
   │                    │                      │<── Voter submit ───│
   │                    │                      │── broadcast ───────│
   │                    │<── "VoteUpdated" event│                   │
   │                    │   {total, results}    │                    │
   │<── UI cập nhật ────│                      │                    │
   │   bar chart tự động│                      │                    │
   │   điều chỉnh độ rộng                      │                    │
```

---

### Flow 4: Đóng Poll

```
Creator           AnalyticsView     PollService     VoteService   VoterView
   │                   │                 │                │            │
   │── bấm "Stop" ────>│                 │                │            │
   │                   │── confirm modal │                │            │
   │── confirm ───────>│                 │                │            │
   │                   │── PUT /api/polls/code/{code} ───>│            │
   │                   │   {status: "Closed"}             │            │
   │                   │                 │── cập nhật DB  │            │
   │                   │                 │── POST /api/votes/          │
   │                   │                 │   broadcast-poll-closed ───>│
   │                   │                 │                │── SignalR  │
   │                   │                 │                │   "PollClosed"
   │                   │                 │                │            │
   │                   │<── 204 No Content               │<── event ──│
   │<── badge "Closed" ─                                  │            │
   │                                                      │<── "This poll
   │                                                           has ended"
```

---

## ⚡ Realtime với SignalR

### Tổng quan

SignalR là thư viện của Microsoft cho phép server **chủ động push dữ liệu** xuống client qua WebSocket (hoặc long-polling khi không có WebSocket).

### VoteHub (`VoteService/Hubs/VoteHub.cs`)

```
                    VoteHub
                       │
          ┌────────────┼────────────┐
          │            │            │
     Group              Group        Group
  "poll_482931"    "poll_193847"  "poll_xxxxxx"
     │                               │
  [AnalyticsView]              [AnalyticsView]
  [VoteView x2]                [VoteView x1]
```

**Các method của Hub:**

| Method | Ai gọi | Tác dụng |
|---|---|---|
| `JoinPollRoom(pollCode)` | Client (frontend) | Join vào group `poll_{code}` để nhận events |
| `LeavePollRoom(pollCode)` | Client (frontend) | Rời group khi thoát trang |
| `BroadcastVoteUpdate(pollCode, data)` | Có thể gọi từ client | Broadcast data tới group |

**Events server push về client:**

| Event | Khi nào | Data |
|---|---|---|
| `VoteUpdated` | Sau mỗi vote mới | `{ pollCode, total, results: [{optionId, count}] }` |
| `PollClosed` | Sau khi creator đóng poll | `{ pollCode, status: "Closed" }` |
| `JoinedRoom` | Sau khi join thành công | `pollCode` |

### `usePollHub.js` — Composable kết nối SignalR

```javascript
// Kết nối trực tiếp đến VoteService (không qua Ocelot)
const VOTE_SERVICE_URL = 'https://localhost:5002'

// Cơ chế tự reconnect khi mất kết nối:
// Thử reconnect sau: 0ms, 1s, 3s, 5s
.withAutomaticReconnect([0, 1000, 3000, 5000])

// Lifecycle:
// start()    → kết nối + JoinPollRoom
// onVoteUpdated callback → gọi khi nhận "VoteUpdated"
// stop()     → LeavePollRoom + disconnect (gọi khi component unmount)
```

---

## 🔐 Xác thực người dùng — VoterToken

Dự án **không yêu cầu đăng nhập**, nhưng vẫn cần ngăn 1 người vote 2 lần. Cơ chế dùng `localStorage`:

### Cách hoạt động

```
Lần đầu mở app:
  localStorage['poll_voter_token'] không tồn tại
  → Tạo token ngẫu nhiên: "voter_47291038"
  → Lưu vào localStorage

Mỗi lần vote:
  → Đọc token từ localStorage
  → Gửi cùng request: { voterToken: "voter_47291038", ... }
  → Server kiểm tra: đã có (PollCode="482931", VoterToken="voter_47291038") chưa?
    → Rồi: 400 "You have already voted."
    → Chưa: lưu vote

Sau khi vote thành công:
  → localStorage['voted_482931'] = 'true'
  → Lần sau vào /vote/482931: phát hiện ngay từ localStorage → hiện "Already Voted"
```

### Dữ liệu lưu trong `localStorage`

| Key | Giá trị | Mục đích |
|---|---|---|
| `poll_voter_token` | `"voter_47291038"` | Token định danh thiết bị |
| `voted_{code}` | `"true"` | Đánh dấu đã vote poll này rồi |
| `createdPolls` | `'["482931","193847"]'` | Danh sách poll mình đã tạo (kiểm tra quyền creator) |

### Hạn chế (chấp nhận được cho dự án này)

- Xóa cache trình duyệt → mất token → có thể vote lại
- Dùng thiết bị/trình duyệt khác → token khác → vote lại được
- Đây là hạn chế chung của mọi giải pháp không có tài khoản

---

## 🚀 Hướng dẫn chạy dự án

### Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 18+](https://nodejs.org/) và npm
- SQL Server hoặc SQL Server LocalDB (đi kèm Visual Studio)
- Visual Studio 2022 hoặc VS Code

---

### Bước 1: Chuẩn bị Database

Mỗi service cần migrate database riêng. Mở terminal, chạy lần lượt:

```bash
# PollService → tạo PollDB
cd PollService
dotnet ef database update

# VoteService → tạo VoteDB
cd ../VoteService
dotnet ef database update

# AnalyticsService → tạo AnalyticsDB
cd ../AnalyticsService
dotnet ef database update
```

> Nếu chưa có `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

---

### Bước 2: Khởi động các Backend Services

Mở **4 terminal riêng biệt**, chạy mỗi service trong một terminal:

```bash
# Terminal 1 — PollService
cd PollService
dotnet run
# Chạy tại: https://localhost:5001

# Terminal 2 — VoteService
cd VoteService
dotnet run
# Chạy tại: https://localhost:5002

# Terminal 3 — AnalyticsService
cd AnalyticsService
dotnet run
# Chạy tại: https://localhost:5003

# Terminal 4 — OcelotGateway
cd OcelotGateway
dotnet run
# Chạy tại: https://localhost:5000
```

> **Thứ tự khuyến nghị:** Khởi động PollService và VoteService trước, sau đó mới OcelotGateway.

---

### Bước 3: Khởi động Frontend

```bash
cd client
npm install     # Chỉ cần chạy lần đầu
npm run serve   # Dev server tại http://localhost:8080
```

---

### Bước 4: Sử dụng ứng dụng

Mở trình duyệt và truy cập `http://localhost:8080`

**Test nhanh:**
1. Bấm **Create Poll** → tạo poll Multiple Choice → bấm Create
2. Mở tab mới → truy cập link chia sẻ được hiện trong AnalyticsView
3. Vote → quay về tab AnalyticsView → xem kết quả cập nhật realtime

---

### Chạy bằng Visual Studio (cách khác)

Mở file `PollSurvey.sln` trong Visual Studio 2022.  
Cấu hình **Multiple Startup Projects** (chuột phải Solution → Properties):
- `OcelotGateway` — Start
- `PollService` — Start
- `VoteService` — Start
- `AnalyticsService` — Start

Bấm `F5` để chạy tất cả cùng lúc.

---

### Swagger API Docs (khi chạy ở Development)

| Service | Swagger URL |
|---|---|
| PollService | `https://localhost:5001/swagger` |
| VoteService | `https://localhost:5002/swagger` |
| AnalyticsService | `https://localhost:5003/swagger` |

---

## 🌐 Ports & URLs

| Service | Port | Vai trò |
|---|---|---|
| OcelotGateway | `:5000` | API Gateway + Static file server |
| PollService | `:5001` | Quản lý Poll & Options |
| VoteService | `:5002` | Quản lý Vote + SignalR Hub |
| AnalyticsService | `:5003` | Audit log thống kê |
| Vue.js Dev Server | `:8080` | Frontend (chỉ khi develop) |

**URL quan trọng:**

| Mục đích | URL |
|---|---|
| App (production qua Gateway) | `https://localhost:5000` |
| App (development frontend) | `http://localhost:8080` |
| SignalR Hub | `https://localhost:5002/hubs/vote` |
| Tạo poll | `http://localhost:8080/create` |
| Vote (với code) | `http://localhost:8080/vote/{code}` |
| Xem kết quả | `http://localhost:8080/analytics?code={code}` |

---

## 📝 Ghi chú bổ sung

### Tại sao không có docker-compose?

Dự án chạy trực tiếp bằng .NET CLI / Visual Studio. Mỗi service có `Dockerfile` riêng để có thể containerize độc lập nếu cần.

### Tại sao SignalR không đi qua Ocelot?

Ocelot có hỗ trợ WebSocket nhưng cần cấu hình phức tạp hơn và đôi khi gặp vấn đề với long-polling fallback. Frontend kết nối SignalR **trực tiếp** đến VoteService `:5002` để đảm bảo ổn định. CORS trên VoteService đã được cấu hình cho phép `localhost:8080`.

### Xử lý loại câu hỏi

| Loại | OptionId gửi | VoteValue gửi | Hiển thị kết quả |
|---|---|---|---|
| Multiple Choice | ID của option được chọn | `""` | Bar chart theo count |
| Yes / No | ID của Yes hoặc No | `""` | Bar chart 2 thanh |
| Rating | `0` | `"1"` đến `"5"` | Danh sách hàng sao |
| Open Text | `0` | Nội dung text | Danh sách card text |
