# 📊 Poll Survey — Ứng dụng khảo sát thời gian thực

> Tạo và tham gia khảo sát/bình chọn trực tuyến. Không cần đăng ký tài khoản.  
> Kết quả cập nhật tức thì qua WebSocket (SignalR).  
> Kiến trúc Microservices: 3 service backend độc lập + 1 API Gateway + 1 Vue.js SPA.

---

## 📋 Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Ports & URLs](#2-ports--urls)
3. [Cấu trúc thư mục](#3-cấu-trúc-thư-mục)
4. [Thư viện & Dependencies](#4-thư-viện--dependencies)
5. [Database Schema chi tiết](#5-database-schema-chi-tiết)
6. [API chi tiết từng endpoint](#6-api-chi-tiết-từng-endpoint)
7. [Ocelot Gateway — Request routing](#7-ocelot-gateway--request-routing)
8. [Luồng nghiệp vụ chi tiết](#8-luồng-nghiệp-vụ-chi-tiết)
9. [Realtime với SignalR](#9-realtime-với-signalr)
10. [Frontend — Giải thích từng file](#10-frontend--giải-thích-từng-file)
11. [VoterToken — Chống vote 2 lần](#11-votertoken--chống-vote-2-lần)
12. [Migration files — EF Core](#12-migration-files--ef-core)
13. [Dockerfile & Containerization](#13-dockerfile--containerization)
14. [Hướng dẫn chạy dự án](#14-hướng-dẫn-chạy-dự-án)
15. [Chuyển máy / Deploy sang máy khác](#15-chuyển-máy--deploy-sang-máy-khác)

---

## 1. Tổng quan kiến trúc

Dự án dùng kiến trúc **Microservices** — mỗi chức năng chính là một service độc lập, có database riêng. Client chỉ nói chuyện với một địa chỉ duy nhất (OcelotGateway), Ocelot tự forward đến đúng service.

```
┌─────────────────────────────────────────────────┐
│         Vue.js 3 SPA (http://localhost:8080)     │
│         (dev) hoặc qua Ocelot :5000 (prod)       │
└──────────────┬──────────────────┬────────────────┘
               │ HTTP (Axios)     │ WebSocket (SignalR)
               ▼                  │ trực tiếp đến :5002
    ┌──────────────────────┐       │
    │  OcelotGateway :5000 │       │
    │  API Gateway +        │       │
    │  Static file server  │       │
    └──────────┬───────────┘       │
               │ forward theo URL  │
        ┌──────┼──────┐            │
        ▼      ▼      ▼            ▼
  ┌──────────┐ ┌──────────┐ ┌──────────────────┐
  │PollSvc   │ │VoteSvc   │ │AnalyticsSvc      │
  │:5001     │ │:5002     │ │:5003             │
  │CRUD poll │ │Vote +    │ │Audit log vote    │
  │& options │ │SignalR   │ │(write-only)      │
  └────┬─────┘ └────┬─────┘ └────────┬─────────┘
       │            │                │
       ▼            ▼                ▼
   [PollDB]     [VoteDB]       [AnalyticsDB]
   SQL Server   SQL Server     SQL Server
```

**Quy trình một request:**
1. Vue gửi `GET https://localhost:5000/api/polls/check/123456` (qua Axios)
2. OcelotGateway nhận, khớp route `/api/polls/{everything}` → forward đến `https://localhost:5001/api/Polls/check/123456`
3. PollService truy vấn PollDB, trả JSON
4. Ocelot trả JSON đó về cho Vue
5. Vue cập nhật giao diện

**Giao tiếp giữa các service (inter-service):**
- PollService → VoteService: gọi khi đóng poll (broadcast SignalR) hoặc khi xóa poll (xóa votes)
- VoteService → PollService: validate poll trước khi lưu vote
- VoteService → AnalyticsService: ghi log sau mỗi vote (fire-and-forget)

---

## 2. Ports & URLs

| Service | Port | Giao thức | Vai trò |
|---|---|---|---|
| OcelotGateway | `5000` | HTTPS | API Gateway + Serve Vue SPA (production) |
| PollService | `5001` | HTTPS | CRUD Poll & Options |
| VoteService | `5002` | HTTPS | Submit Vote + SignalR Hub |
| AnalyticsService | `5003` | HTTP | Audit log thống kê |
| Vue Dev Server | `8080` | HTTP | Frontend (chỉ khi develop) |

**Các URL quan trọng khi develop:**

| Mục đích | URL |
|---|---|
| Trang chủ (dev) | `http://localhost:8080` |
| Tạo poll | `http://localhost:8080/create` |
| Vote | `http://localhost:8080/vote/123456` |
| Xem kết quả | `http://localhost:8080/analytics?code=123456` |
| Swagger PollService | `https://localhost:5001/swagger` |
| Swagger VoteService | `https://localhost:5002/swagger` |
| Swagger AnalyticsService | `https://localhost:5003/swagger` |
| SignalR Hub (kết nối trực tiếp) | `https://localhost:5002/hubs/vote` |

> SignalR frontend kết nối **trực tiếp đến VoteService `:5002`**, không qua OcelotGateway,  
> vì Ocelot cần cấu hình phức tạp cho WebSocket và dễ xảy ra vấn đề với long-polling fallback.

---

## 3. Cấu trúc thư mục

```
poll-survey/
│
├── PollSurvey.sln                 ← Solution Visual Studio (gom tất cả project)
├── BACKEND_FLOW.md                ← Tài liệu kỹ thuật luồng backend
│
├── OcelotGateway/                 ← API Gateway, điểm vào duy nhất từ client
│   ├── Program.cs                 ← Cấu hình CORS, Ocelot, static files, SPA fallback
│   ├── ocelot.json                ← Bảng routing: URL pattern → service nào, port nào
│   ├── appsettings.json           ← Logging config
│   ├── OcelotGateway.csproj       ← Packages: Ocelot, Swagger + copy client vào wwwroot
│   └── Dockerfile                 ← Container image cho Gateway
│
├── PollService/                   ← Quản lý Poll & Options
│   ├── Controllers/
│   │   └── PollsController.cs     ← 6 endpoint: GET, POST, PUT, DELETE poll
│   ├── Models/
│   │   ├── Poll.cs                ← Entity: Id, Code, Question, QuestionType, Status, ExpireAt, CreatedAt, Options
│   │   └── Option.cs              ← Entity: Id, PollId (FK), Text
│   ├── Data/
│   │   └── PollDbContext.cs       ← EF Core DbContext: DbSet<Poll>, DbSet<Option>
│   ├── Migrations/                ← EF Core auto-generated migration files
│   ├── appsettings.json           ← Connection string → PollDB (LocalDB)
│   ├── Program.cs                 ← CORS, EF Core, Newtonsoft.Json, Swagger
│   ├── PollService.csproj         ← Packages: EF Core, Newtonsoft.Json, Swagger
│   └── Dockerfile
│
├── VoteService/                   ← Submit Vote + Realtime
│   ├── Controllers/
│   │   └── VotesController.cs     ← 6 endpoint: POST vote, GET results, DELETE, broadcast
│   ├── Hubs/
│   │   └── VoteHub.cs             ← SignalR Hub: JoinRoom, LeaveRoom, BroadcastVoteUpdate
│   ├── Models/
│   │   └── Vote.cs                ← Entity: Id, PollCode, OptionId, VoteValue, VoterToken, CreatedAt
│   ├── Data/
│   │   └── VoteDbContext.cs       ← EF Core DbContext: DbSet<Vote>
│   ├── Migrations/
│   ├── appsettings.json           ← Connection string → VoteDB + URLs của PollService & AnalyticsService
│   ├── Program.cs                 ← CORS (AllowCredentials), EF Core, SignalR, HttpClient
│   ├── VoteService.csproj         ← Packages: EF Core, Swagger
│   └── Dockerfile
│
├── AnalyticsService/              ← Audit log vote (write-only)
│   ├── Controllers/
│   │   └── AnalyticsController.cs ← 2 endpoint: POST nhận log, GET summary
│   ├── Models/
│   │   └── Analytics.cs           ← Entity: Id, PollCode, OptionId, VoteTime
│   ├── Data/
│   │   └── AnalyticsDbContext.cs  ← EF Core DbContext: DbSet<Analytics>
│   ├── Migrations/
│   ├── appsettings.json           ← Connection string → AnalyticsDB
│   ├── Program.cs                 ← CORS, EF Core, Swagger
│   ├── AnalyticsService.csproj    ← Packages: EF Core, Swagger
│   └── Dockerfile
│
└── client/                        ← Vue.js 3 SPA Frontend
    ├── src/
    │   ├── main.js                ← Entry point: tạo app, cài Router, Toast, mount
    │   ├── App.vue                ← Root component: router-view + fade transition
    │   ├── api.js                 ← Axios instance + tất cả hàm gọi HTTP
    │   ├── voterToken.js          ← Tạo/lấy voter token từ localStorage
    │   ├── usePollHub.js          ← Composable kết nối SignalR realtime
    │   ├── router/index.js        ← 4 routes + browser title + scroll behavior
    │   ├── views/
    │   │   ├── HomeView.vue       ← Trang chủ: nhập code / tạo poll
    │   │   ├── CreatePollView.vue ← Form tạo poll (4 loại câu hỏi)
    │   │   ├── VoteView.vue       ← Trang bỏ phiếu (5 trạng thái UI)
    │   │   └── AnalyticsView.vue  ← Dashboard kết quả realtime (chỉ creator)
    │   └── assets/main.css        ← Design system: CSS variables, components
    ├── package.json               ← Dependencies + scripts
    ├── tailwind.config.js         ← Cấu hình TailwindCSS
    └── vue.config.js              ← Dev server proxy config
```

---

## 4. Thư viện & Dependencies

### 4.1 Backend — NuGet Packages

#### Microsoft.EntityFrameworkCore.SqlServer `8.0.0`
- **Dùng ở:** PollService, VoteService, AnalyticsService
- **Tác dụng:** ORM (Object-Relational Mapper) — ánh xạ class C# ↔ bảng SQL Server. Thay vì viết SQL thủ công, dùng LINQ: `_db.Polls.Where(p => p.Code == code).FirstOrDefaultAsync()` → EF Core tự sinh câu SQL.
- **Vì sao dùng:** .NET 8 chuẩn, tích hợp sẵn dependency injection, hỗ trợ migrations tự động tạo/cập nhật schema DB.
- **Cài:** Đã có trong `.csproj`, restore tự động bằng `dotnet restore`.

#### Microsoft.EntityFrameworkCore.Design `8.0.0`
- **Dùng ở:** Cả 3 service
- **Tác dụng:** Package hỗ trợ thiết kế, **chỉ dùng khi generate migration** (`dotnet ef migrations add`), không cần ở runtime. `PrivateAssets=all` để không bị đóng gói vào output.
- **Vì sao dùng:** Bắt buộc để chạy lệnh `dotnet ef`.

#### Microsoft.EntityFrameworkCore.Tools `8.0.0`
- **Dùng ở:** Cả 3 service
- **Tác dụng:** CLI tools cho EF Core — cho phép chạy `dotnet ef database update`, `dotnet ef migrations add` từ terminal.
- **Vì sao dùng:** Quản lý schema DB bằng code (code-first migrations), không cần tay tạo bảng trong SQL.

#### Swashbuckle.AspNetCore `6.6.2` (Swagger)
- **Dùng ở:** PollService, VoteService, AnalyticsService, OcelotGateway
- **Tác dụng:** Tự động generate trang Swagger UI tại `/swagger` — cho phép test API ngay trên trình duyệt mà không cần Postman. Scan tất cả `[HttpGet]`, `[HttpPost]`... trong Controllers và tạo documentation tương tác.
- **Vì sao dùng:** Dễ test và debug API trong quá trình phát triển.
- **Chỉ bật khi Development:** `if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }`

#### Microsoft.AspNetCore.Mvc.NewtonsoftJson `8.0.0`
- **Dùng ở:** PollService (và PollService **chỉ mình PollService** dùng)
- **Tác dụng:** Thay thế serializer JSON mặc định của ASP.NET Core (`System.Text.Json`) bằng `Newtonsoft.Json` (Json.NET). Lý do cụ thể: cấu hình `DateTimeZoneHandling.Utc` để mọi DateTime trong JSON response tự động thêm suffix `Z` (chỉ rõ UTC timezone).
- **Vì sao cần:** `System.Text.Json` mặc định serialize `DateTime` không có `Z` → frontend nhận về như `"2026-08-02T07:00:00"` → JavaScript không biết timezone → có thể lệch giờ. Với Newtonsoft và `DateTimeZoneHandling.Utc` → output là `"2026-08-02T07:00:00Z"` → JS `new Date("...Z")` parse chuẩn UTC rồi hiển thị giờ local tự động.
- **Cài đặt trong `Program.cs`:**
  ```csharp
  builder.Services.AddControllers()
      .AddNewtonsoftJson(options => {
          options.SerializerSettings.DateTimeZoneHandling =
              Newtonsoft.Json.DateTimeZoneHandling.Utc;
      });
  ```

#### Ocelot `23.3.3`
- **Dùng ở:** OcelotGateway
- **Tác dụng:** API Gateway — nhận tất cả request từ client, đọc `ocelot.json` để biết URL nào cần forward đến service nào. Hoạt động như reverse proxy.
- **Vì sao dùng:** Client chỉ cần biết 1 địa chỉ (`localhost:5000`), không biết có bao nhiêu service backend. Dễ thêm/bớt/thay đổi service mà không cần sửa client.
- **Đăng ký:** `builder.Services.AddOcelot()` + `await app.UseOcelot()`

#### Microsoft.VisualStudio.Azure.Containers.Tools.Targets `1.22.1`
- **Dùng ở:** Cả 4 service
- **Tác dụng:** Hỗ trợ debug container trong Visual Studio (Container Tools). Cho phép F5 debug trực tiếp trong Docker container từ IDE.
- **Vì sao dùng:** Tiện cho development với Docker, không ảnh hưởng production build.

---

### 4.2 Frontend — npm Packages

#### vue `^3.2.13`
- **Tác dụng:** Framework UI chính. Vue 3 dùng Composition API (`<script setup>`, `ref()`, `onMounted()`) — code gọn hơn Options API của Vue 2. Mọi component `.vue` đều dùng Vue.
- **Vì sao dùng:** Reactive — khi dữ liệu thay đổi, Vue tự cập nhật DOM mà không cần `document.getElementById`. Cú pháp template `v-if`, `v-for`, `v-model` rõ ràng.
- **Cài:** `npm install vue@^3.2.13`

#### vue-router `^4.6.4`
- **Tác dụng:** Quản lý điều hướng trong SPA. Map URL → component mà không reload trang. Hỗ trợ route params (`/vote/:code`), query string (`/analytics?code=xxx`), navigation guards (đổi title tab).
- **Vì sao dùng:** App có 4 trang khác nhau, cần điều hướng mà không mất state hay reload toàn bộ app.
- **Dùng trong code:** `useRouter()` để chuyển trang (`router.push('/vote/123')`), `useRoute()` để đọc URL (`route.params.code`, `route.query.code`).
- **Cài:** `npm install vue-router@^4.6.4`

#### axios `^1.19.0`
- **Tác dụng:** HTTP client gửi request lên server. Tiện hơn `fetch` native: hỗ trợ interceptors (bắt lỗi tập trung), auto-parse JSON, timeout, base URL. File `api.js` tạo 1 instance Axios với `baseURL: 'https://localhost:5000'` → mọi request đều tự gắn prefix này.
- **Vì sao dùng:** Interceptor error giúp chuẩn hóa error message từ backend thành `Error` object có `.message` → component chỉ cần `catch(e) → e.message`.
- **Cài:** `npm install axios@^1.19.0`

#### @microsoft/signalr `^10.0.0`
- **Tác dụng:** Client-side SignalR library. Tạo kết nối WebSocket đến `VoteHub` để nhận events realtime (`VoteUpdated`, `PollClosed`). Tự động thử WebSocket trước, fallback về Server-Sent Events rồi Long Polling nếu WebSocket không dùng được.
- **Vì sao dùng:** Khi có người vote, server cần chủ động push dữ liệu mới đến tất cả dashboard đang mở — HTTP thông thường không làm được điều này.
- **Dùng trong code:** `new signalR.HubConnectionBuilder().withUrl(...).withAutomaticReconnect([0,1000,3000,5000]).build()`
- **Cài:** `npm install @microsoft/signalr@^10.0.0`

#### vue-toastification `^2.0.0-rc.5`
- **Tác dụng:** Hiển thị thông báo "toast" (hộp nhỏ góc màn hình, tự biến mất sau vài giây). Có 3 loại: `toast.success()` (xanh), `toast.error()` (đỏ), `toast.info()` (xanh nhạt).
- **Vì sao dùng:** UX — feedback ngay cho user sau mỗi action (tạo poll thành công, copy link, xóa poll...) mà không cần alert() hay reload trang.
- **Cài:** `npm install vue-toastification@^2.0.0-rc.5`
- **Import CSS bắt buộc:** `import 'vue-toastification/dist/index.css'` trong `main.js`
- **Cấu hình trong `main.js`:**
  ```js
  app.use(Toast, {
    position: 'bottom-right',
    timeout: 2500,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: false,
    hideProgressBar: true,
    closeButton: false,
  })
  ```

#### qrcode `^1.5.4`
- **Tác dụng:** Vẽ mã QR lên phần tử `<canvas>` HTML. Nhận URL cần encode và kích thước → tạo ảnh QR bitmap ngay trong trình duyệt, không cần server.
- **Vì sao dùng:** Cho phép creator chia sẻ poll bằng cách scan QR thay vì gõ code.
- **Dùng trong code:** `await QRCode.toCanvas(canvasElement, shareLink(), { width: 320, margin: 2, color: {...} })`
- **Cài:** `npm install qrcode@^1.5.4`

#### @lucide/vue `^1.28.0`
- **Tác dụng:** Bộ icon SVG dạng Vue component. Import từng icon cần dùng: `import { Check, Trash2, Star } from '@lucide/vue'` rồi dùng như component `<Trash2 :size="16" />`. Mỗi icon là SVG inline → scale không bị mờ, có thể đổi màu bằng CSS.
- **Vì sao dùng:** Nhẹ hơn icon font (Font Awesome), tree-shakeable (chỉ bundle icon nào dùng), style thống nhất.
- **Cài:** `npm install @lucide/vue@^1.28.0`

#### tailwindcss `^3.4.19`
- **Tác dụng:** CSS utility framework — thay vì viết CSS file riêng, dùng class trực tiếp trong template: `class="flex items-center gap-2 text-[14px] font-bold"`. Build tool scan tất cả file, chỉ giữ lại class nào thực sự dùng → file CSS cuối cùng rất nhỏ.
- **Vì sao dùng:** Tốc độ phát triển nhanh, không cần đặt tên class. Tuy nhiên app này còn dùng thêm CSS variables (trong `main.css`) cho design system (`--blue`, `--text`, `--border`...).
- **Cài:** `npm install -D tailwindcss@^3.4.19 autoprefixer@^10.5.4 postcss@^8.5.25`
- **Cấu hình `tailwind.config.js`:** Khai báo `content` để Tailwind scan đúng file:
  ```js
  module.exports = { content: ['./src/**/*.{vue,js,ts}'] }
  ```

---

## 5. Database Schema chi tiết

Có 3 database độc lập (mỗi service quản lý database của mình — nguyên tắc "database per service" trong microservices). Các service **không truy cập chéo database của nhau**, chỉ giao tiếp qua HTTP API.

---

### 5.1 PollDB (PollService)

#### Bảng `Polls`

```
Polls
├── Id          int          NOT NULL  PRIMARY KEY  IDENTITY(1,1)
├── Code        nvarchar(max) NOT NULL              -- "482931" (6 chữ số)
├── Question    nvarchar(max) NOT NULL              -- nội dung câu hỏi
├── QuestionType nvarchar(max) NOT NULL             -- "Multiple Choice" / "Yes / No" / "Rating" / "Open Text"
├── Status      nvarchar(max) NOT NULL              -- "Active" / "Closed"
├── ExpireAt    datetime2    NOT NULL               -- thời điểm hết hạn (UTC)
└── CreatedAt   datetime2    NOT NULL               -- thời điểm tạo (UTC)
```

#### Bảng `Options`

```
Options
├── Id      int           NOT NULL  PRIMARY KEY  IDENTITY(1,1)
├── PollId  int           NOT NULL  FOREIGN KEY → Polls.Id  ON DELETE CASCADE
└── Text    nvarchar(max) NOT NULL              -- "Vue.js", "React", "Yes", "No"
```

**Index:** `IX_Options_PollId` trên cột `PollId` — tăng tốc query `WHERE PollId = ?`.

**Cascade Delete:** Xóa một Poll → tất cả Option của Poll đó tự động bị xóa theo. Không cần xóa thủ công.

**Liên kết `Poll ↔ Option`:**
```
Poll (Id=1, Code="482931", Question="Best framework?")
  ├── Option (Id=1, PollId=1, Text="Vue.js")
  ├── Option (Id=2, PollId=1, Text="React")
  └── Option (Id=3, PollId=1, Text="Angular")
```
Trong EF Core, `Poll.cs` có `public List<Option> Options { get; set; }` — khi query dùng `.Include(p => p.Options)`, EF Core tự JOIN bảng Options để lấy kèm.

**Lưu ý về các loại câu hỏi và Options:**
- `Multiple Choice` → Options = danh sách người tạo nhập (ít nhất 2)
- `Yes / No` → Backend tự tạo 2 Option `"Yes"` và `"No"` (frontend không cần gửi)
- `Rating` → Không có Option nào trong bảng Options
- `Open Text` → Không có Option nào trong bảng Options

---

### 5.2 VoteDB (VoteService)

#### Bảng `Votes`

```
Votes
├── Id          int           NOT NULL  PRIMARY KEY  IDENTITY(1,1)
├── PollCode    nvarchar(max) NOT NULL              -- "482931" (ref đến Poll, không dùng FK)
├── OptionId    int           NOT NULL              -- ID option được chọn; 0 nếu Rating/OpenText
├── VoteValue   nvarchar(max) NOT NULL              -- "4" (sao), "Tôi thích Vue" (text), "" (MC/YN)
├── VoterToken  nvarchar(max) NOT NULL              -- "voter_47291038" từ localStorage
└── CreatedAt   datetime2    NOT NULL               -- thời điểm vote
```

**Không có Foreign Key đến PollDB** — đây là thiết kế có chủ đích trong microservices (loose coupling). VoteService chỉ lưu `PollCode` dạng chuỗi, validate poll bằng cách gọi HTTP sang PollService thay vì JOIN DB.

**Chống vote 2 lần:** Query `AnyAsync(v => v.PollCode == X && v.VoterToken == Y)` trước khi lưu vote mới.

**Mapping loại câu hỏi ↔ dữ liệu lưu:**

| Loại câu hỏi | OptionId | VoteValue | Ý nghĩa |
|---|---|---|---|
| Multiple Choice | ID option (vd: 2) | `""` | Đã chọn option số 2 |
| Yes / No | ID option Yes hoặc No | `""` | Đã chọn Yes hoặc No |
| Rating | `0` | `"4"` | Chọn 4 sao |
| Open Text | `0` | `"Tôi thích Vue"` | Câu trả lời tự do |

---

### 5.3 AnalyticsDB (AnalyticsService)

#### Bảng `Analytics`

```
Analytics
├── Id        int           NOT NULL  PRIMARY KEY  IDENTITY(1,1)
├── PollCode  nvarchar(max) NOT NULL              -- "482931"
├── OptionId  int           NOT NULL              -- option được chọn (để thống kê)
└── VoteTime  datetime2    NOT NULL               -- thời điểm ghi nhận
```

**Vai trò:** Write-only audit log — VoteService ghi vào sau mỗi vote (fire-and-forget). AnalyticsService không đọc hay xóa dữ liệu theo thời gian thực. Hiện tại frontend không đọc trực tiếp bảng này, nhưng endpoint `GET /api/analytics/summary/{code}` có thể dùng mở rộng sau.

**Tại sao tách riêng thay vì đọc VoteDB?** Microservices — AnalyticsService có thể được thay thế bằng tool thống kê khác (Kafka, Elasticsearch...) mà không ảnh hưởng VoteService.

---

## 6. API chi tiết từng endpoint

### 6.1 PollService — `/api/Polls`

---

#### `GET /api/polls/code/{code}`

Lấy thông tin đầy đủ của poll kèm danh sách options.

**Dùng ở:** AnalyticsView khi mở trang (cần options để map tên hiển thị với kết quả vote).

**Request:**
```
GET /api/polls/code/482931
```

**Xử lý backend:**
1. Query `Polls` table với `.Include(p => p.Options)` để lấy kèm Options
2. Nếu không tìm thấy → trả 404

**Response thành công `200 OK`:**
```json
{
  "id": 1,
  "code": "482931",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "status": "Active",
  "expireAt": "2026-08-10T12:00:00Z",
  "createdAt": "2026-08-02T06:30:00Z",
  "options": [
    { "id": 1, "pollId": 1, "text": "Vue.js" },
    { "id": 2, "pollId": 1, "text": "React" },
    { "id": 3, "pollId": 1, "text": "Angular" }
  ]
}
```

**Response thất bại:**
- `404 Not Found` — poll code không tồn tại trong DB

---

#### `GET /api/polls/check/{code}`

Validate poll: còn tồn tại, còn Active, chưa hết hạn. Nhẹ hơn endpoint trên vì chỉ validate.

**Dùng ở:**
- HomeView khi nhập code để join
- VoteView khi load trang
- VoteService khi validate trước khi lưu vote (inter-service call)

**Request:**
```
GET /api/polls/check/482931
```

**Xử lý backend:**
1. Query poll theo code (có `.Include(p => p.Options)`)
2. Nếu không tìm thấy → 404
3. Nếu `status != "Active"` → 400
4. Nếu `expireAt <= DateTime.UtcNow` → 400
5. Nếu hợp lệ → 200 kèm data poll

**Response thành công `200 OK`:** (Cùng format với endpoint trên, kèm options)

**Response thất bại:**
- `404 Not Found` — `{ "message": "Poll does not exist." }`
- `400 Bad Request` — `{ "message": "Poll is closed." }` — poll đã bị đóng tay
- `400 Bad Request` — `{ "message": "Poll has expired." }` — quá thời hạn `expireAt`

---

#### `GET /api/polls/check-option/{optionId}`

Validate một option có tồn tại không.

**Dùng ở:** Hiện tại VoteService có thể gọi để verify option trước khi lưu vote (tùy implementation).

**Request:** `GET /api/polls/check-option/3`

**Response:**
- `200 OK` — `{ "id": 3, "pollId": 1, "text": "Angular" }`
- `404 Not Found` — option không tồn tại

---

#### `POST /api/polls`

Tạo poll mới.

**Request Body:**
```json
{
  "code": "482931",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "expireAt": "2026-08-10T12:00:00Z",
  "options": [
    { "text": "Vue.js" },
    { "text": "React" }
  ]
}
```

**Xử lý backend chi tiết:**
1. Validate `question` không rỗng → nếu rỗng: `400 "Question cannot be empty."`
2. `DateTime.SpecifyKind(pollData.ExpireAt, DateTimeKind.Utc)` — đánh dấu lại là UTC (frontend gửi ISO string nhưng C# deserialize thành `Unspecified` kind)
3. Validate `expireAt > DateTime.UtcNow` → nếu không: `400 "Expiration date must be in the future."`
4. Kiểm tra code chưa tồn tại trong DB → nếu đã có: `400 "Code already exists."`
5. Tự sinh Options theo `questionType`:
   - `"Multiple Choice"` và `options.Count >= 2` → dùng options từ request
   - `"Multiple Choice"` nhưng `options.Count < 2` → `Exception`
   - `"Yes / No"` → bỏ qua options gửi lên, tạo `[{Text:"Yes"}, {Text:"No"}]`
   - `"Rating"` hoặc `"Open Text"` → `options = []`
6. Set `createdAt = DateTime.UtcNow`, `status = "Active"` (nếu không gửi)
7. `_db.Polls.Add(pollData)` + `SaveChangesAsync()` → EF Core INSERT vào Polls + Options

**Response thành công `201 Created`:**
```
Location: /api/Polls/code/482931
Body: object poll đầy đủ kèm options với Id được gán từ DB
```

**Response thất bại:**
- `400 Bad Request` — `{ "message": "Question cannot be empty." }`
- `400 Bad Request` — `{ "message": "Expiration date must be in the future." }`
- `400 Bad Request` — `{ "message": "Code already exists." }` — trùng code (hiếm, random 6 số)
- `500 Server Error` — nếu Multiple Choice gửi < 2 options (Exception chưa handled)

---

#### `PUT /api/polls/code/{code}`

Cập nhật poll — chủ yếu để đóng poll (`status: "Closed"`).

**Request Body:**
```json
{
  "id": 1,
  "code": "482931",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "status": "Closed",
  "expireAt": "2026-08-10T12:00:00Z",
  "options": []
}
```

**Xử lý backend:**
1. Tìm poll theo code → 404 nếu không có
2. Kiểm tra `existingPoll.Status != pollUpdateData.Status` → nếu status thay đổi, đánh dấu `statusIsChanging = true`
3. Cập nhật `Status`, `Question`, `ExpireAt`
4. `SaveChangesAsync()`
5. Nếu `statusIsChanging && newStatus == "Closed"` → gọi HTTP POST đến VoteService: `POST /api/votes/broadcast-poll-closed`
   - VoteService phát SignalR `"PollClosed"` đến tất cả client đang ở group `poll_482931`
   - VoteView nhận event → hiện banner đỏ "This poll has ended"
   - AnalyticsView nhận event → badge đổi sang "Closed"
   - Nếu VoteService không phản hồi → bắt Exception, log warning, tiếp tục (không fail request)

**Response thành công:** `204 No Content`

**Response thất bại:**
- `404 Not Found` — poll không tìm thấy

---

#### `DELETE /api/polls/code/{code}`

Xóa poll vĩnh viễn và tất cả liên quan.

**Xử lý backend:**
1. Tìm poll (kèm `.Include(p => p.Options)`) → 404 nếu không có
2. `_db.Polls.Remove(poll)` + `SaveChangesAsync()` → EF Core DELETE Polls + Options cascade
3. Gọi HTTP DELETE đến VoteService: `DELETE /api/votes/by-poll-code/482931`
   - VoteService xóa tất cả Vote có `PollCode = "482931"` trong VoteDB
   - Nếu VoteService không phản hồi → bắt Exception, log warning, tiếp tục

**Những gì bị xóa:**
- Bảng `Polls`: xóa row poll
- Bảng `Options` (PollDB): xóa tất cả options của poll (cascade)
- Bảng `Votes` (VoteDB): xóa tất cả votes của poll (qua HTTP call)
- Bảng `Analytics` (AnalyticsDB): **KHÔNG xóa** (audit log giữ lại)
- `localStorage['createdPolls']`: frontend xóa code khỏi mảng sau khi API thành công

**Response thành công:** `204 No Content`

**Response thất bại:**
- `404 Not Found` — poll không tìm thấy

---

### 6.2 VoteService — `/api/Votes`

---

#### `POST /api/votes`

Submit phiếu bầu — endpoint phức tạp nhất, giao tiếp với 2 service khác + SignalR.

**Request Body:**
```json
{
  "pollCode": "482931",
  "voterToken": "voter_47291038",
  "optionId": 2,
  "voteValue": ""
}
```

**Xử lý backend step-by-step:**

```
Bước 1 — Validate đầu vào
  pollCode rỗng? → 400 "Missing required data."
  voterToken rỗng? → 400 "Missing required data."

Bước 2 — Chống vote 2 lần
  Query: SELECT * FROM Votes WHERE PollCode='482931' AND VoterToken='voter_47291038'
  Tìm thấy? → 400 "You have already voted."

Bước 3 — Validate poll còn hợp lệ
  HTTP GET https://localhost:5001/api/Polls/check/482931
  PollService trả non-2xx? → 400 "Poll is invalid or has been closed."
  (Poll không tồn tại / đã Closed / quá ExpireAt đều trả non-2xx)

Bước 4 — Lưu vote
  voteData.CreatedAt = DateTime.Now
  INSERT INTO Votes VALUES (...)

Bước 5 — Tính kết quả mới
  SELECT OptionId, COUNT(*) FROM Votes
  WHERE PollCode='482931'
  GROUP BY OptionId
  → allVotesForThisPoll = [{optionId:1, voteCount:3}, {optionId:2, voteCount:5}]
  totalVotes = 8

Bước 6 — Broadcast SignalR
  _signalRHubContext.Clients.Group("poll_482931")
    .SendAsync("VoteUpdated", { pollCode, totalVotes:8, voteResults:[...] })
  → Tất cả AnalyticsView đang mở poll 482931 nhận ngay

Bước 7 — Fire & forget Analytics
  _ = SendVoteAnalyticsAsync(...)
  POST https://localhost:5003/api/Analytics {pollCode, optionId, voteTime}
  (Không await → không ảnh hưởng response dù Analytics bị lỗi)

Bước 8 — Trả kết quả
  200 OK: { "message": "Vote submitted successfully!" }
```

**Response thất bại:**
- `400 Bad Request` — `{ "message": "Missing required data." }` — thiếu pollCode hoặc voterToken
- `400 Bad Request` — `{ "message": "You have already voted." }` — token này đã vote poll này rồi
- `400 Bad Request` — `{ "message": "Poll is invalid or has been closed." }` — poll không còn nhận vote

---

#### `GET /api/votes/result/{pollCode}`

Lấy kết quả nhóm theo option (dùng cho Multiple Choice / Yes-No bar chart).

**Xử lý backend:**
```sql
SELECT OptionId, COUNT(*) as voteCount
FROM Votes
WHERE PollCode = '482931'
GROUP BY OptionId
```

**Response `200 OK`:**
```json
[
  { "optionId": 1, "voteCount": 3 },
  { "optionId": 2, "voteCount": 5 }
]
```
> Chú ý: backend trả `voteCount`, frontend dùng field này dưới tên `count` sau khi map

---

#### `GET /api/votes/total/{pollCode}`

Lấy tổng số phiếu bầu.

**Response `200 OK`:**
```json
{ "pollCode": "482931", "totalVotes": 8 }
```

---

#### `GET /api/votes/list/{pollCode}`

Lấy từng phiếu bầu (dùng cho Rating và Open Text).

**Xử lý backend:**
```sql
SELECT OptionId, VoteValue, CreatedAt
FROM Votes
WHERE PollCode = '482931'
ORDER BY CreatedAt DESC
```

**Response `200 OK`:**
```json
[
  { "optionId": 0, "voteValue": "5", "createdAt": "2026-08-02T07:05:00" },
  { "optionId": 0, "voteValue": "3", "createdAt": "2026-08-02T07:03:00" }
]
```
Frontend dùng `voteValue` để render sao (Rating) hoặc text (Open Text).

---

#### `DELETE /api/votes/by-poll-code/{pollCode}`

Xóa tất cả votes của một poll — được gọi bởi PollService khi xóa poll (inter-service).

**Xử lý:**
1. Query tất cả Votes có `PollCode = X`
2. `RemoveRange(votes)` + `SaveChangesAsync()`
3. `204 No Content`

---

#### `POST /api/votes/broadcast-poll-closed`

Nhận lệnh từ PollService, phát SignalR event "PollClosed".

**Request Body:** `{ "pollCode": "482931" }`

**Xử lý:**
```
pollCode rỗng? → 400
_signalRHubContext.Clients.Group("poll_482931")
  .SendAsync("PollClosed", { pollCode: "482931", status: "Closed" })
→ 200 { "message": "Broadcast sent." }
```

**Kết quả phía client khi nhận event này:**
- VoteView: ẩn form vote, hiện banner đỏ "This poll has ended"
- AnalyticsView: badge trạng thái đổi thành "Closed" (đỏ)

---

### 6.3 AnalyticsService — `/api/Analytics`

---

#### `POST /api/analytics`

Nhận log vote từ VoteService và lưu vào AnalyticsDB.

**Request Body:**
```json
{
  "pollCode": "482931",
  "optionId": 2,
  "voteTime": "2026-08-02T07:05:00"
}
```

**Xử lý:**
1. Nếu `voteTime == default(DateTime)` (không được gửi) → set = `DateTime.Now`
2. INSERT vào `Analytics` table
3. `200 OK`

> Endpoint này được VoteService gọi theo kiểu **fire-and-forget** (không await). Nếu AnalyticsService bị lỗi hay offline, vote vẫn được lưu bình thường trong VoteDB.

---

#### `GET /api/analytics/summary/{pollCode}`

Trả về tổng quan thống kê của poll.

**Xử lý:**
```csharp
var records = await _db.Analytics
    .Where(r => r.PollCode == pollCode)
    .ToListAsync();

var topOptionId = records
    .GroupBy(r => r.OptionId)
    .OrderByDescending(g => g.Count())
    .Select(g => g.Key)
    .FirstOrDefault();
```

**Response `200 OK`:**
```json
{
  "totalVotes": 8,
  "mostVotedOptionId": 2
}
```

> Frontend hiện tại không gọi endpoint này. Dữ liệu Analytics là audit log phục vụ mở rộng sau (vẽ chart theo thời gian, xuất báo cáo...).

---

## 7. Ocelot Gateway — Request routing

OcelotGateway là "cổng vào" duy nhất từ client. File `ocelot.json` định nghĩa bảng routing — mỗi entry là một "Route" gồm `UpstreamPathTemplate` (URL client gọi vào) và `DownstreamPathTemplate` (URL Ocelot forward đến).

### Cách Ocelot xử lý một request

```
Client: GET https://localhost:5000/api/polls/check/482931
   │
   ▼
OcelotGateway nhận request
   │
   ▼
Duyệt qua danh sách Routes trong ocelot.json:
  Route 1: /api/polls/{everything} → match! ("{everything}" = "check/482931")
   │
   ▼
Tạo request mới:
  GET https://localhost:5001/api/Polls/check/482931
  (DownstreamScheme: https, DownstreamHostAndPorts: localhost:5001)
   │
   ▼
Gửi request đến PollService, chờ response
   │
   ▼
Nhận response từ PollService → trả nguyên về client
(Transparent proxy — client không biết request đã đi qua đâu)
```

### Bảng routing chi tiết

| # | Upstream (client gọi vào :5000) | Methods | Downstream (Ocelot forward đến) |
|---|---|---|---|
| 1 | `/api/polls/{everything}` | GET, POST, PUT, DELETE | `localhost:5001/api/Polls/{everything}` |
| 2 | `/api/polls` | GET, POST | `localhost:5001/api/Polls` |
| 3 | `/api/votes/{everything}` | GET, POST, DELETE | `localhost:5002/api/Votes/{everything}` |
| 4 | `/api/votes` | POST | `localhost:5002/api/Votes` |
| 5 | `/api/analytics/{everything}` | GET, POST | `localhost:5003/api/Analytics/{everything}` |
| 6 | `/hubs/vote` | GET, POST, OPTIONS | `localhost:5002/hubs/vote` |

**Lưu ý Route #6:** `/hubs/vote` là endpoint SignalR. Ocelot có route này nhưng **frontend không dùng Ocelot để kết nối SignalR**. Frontend kết nối trực tiếp `https://localhost:5002/hubs/vote`. Route này để lại phòng khi cần dùng qua Gateway trong tương lai.

**Uppercase/Lowercase:** Upstream template dùng `/api/polls` (lowercase), Downstream dùng `/api/Polls` (uppercase P) — đây là convention của ASP.NET Core (tên controller `PollsController` → route `/api/Polls`).

### OcelotGateway kiêm static file server

Ngoài routing API, OcelotGateway còn serve Vue.js frontend:

```csharp
// Program.cs của OcelotGateway
app.UseDefaultFiles();          // /  → tìm index.html trong wwwroot
app.UseStaticFiles();           // /js/app.js, /css/... → serve file trong wwwroot
app.MapFallbackToFile("index.html"); // /vote/123, /analytics → trả index.html
                                     // (SPA routing: Vue Router xử lý URL)
await app.UseOcelot();          // /api/... → forward đến service
```

**SPA Fallback quan trọng:** Khi user gõ `https://localhost:5000/vote/482931` vào browser (hoặc F5 khi đang ở trang đó), server nhận request `/vote/482931` — không phải `/api/...` nên không khớp route Ocelot, không phải file tĩnh nên `UseStaticFiles` không handle. `MapFallbackToFile("index.html")` đảm bảo trả về `index.html` → Vue app load → Vue Router đọc URL → hiển thị VoteView.

**Build Production:** Vue build output (trong `client/dist/`) được copy vào `OcelotGateway/wwwroot/` thông qua cấu hình `.csproj`:
```xml
<Content Include="..\client\**" CopyToOutputDirectory="PreserveNewest">
  <Link>wwwroot\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Content>
```
→ Chạy `dotnet publish OcelotGateway` → tất cả trong 1 folder, deploy 1 service.

---

## 8. Luồng nghiệp vụ chi tiết

### 8.1 Tạo Poll — 4 loại câu hỏi

**Frontend (CreatePollView.vue):**

1. User nhập câu hỏi, chọn loại, chọn thời hạn, nhập options (nếu là MC)
2. Bấm "Create Poll":
   - Validate: question không rỗng, MC phải có ≥ 2 options có nội dung
   - Tạo `roomCode = Math.floor(100000 + Math.random() * 900000).toString()` (ví dụ: `"482931"`)
   - Nếu "No Limit": `expireAt = new Date(); expireAt.setFullYear(expireAt.getFullYear() + 100)` (100 năm sau)
   - Nếu "Set Deadline": convert datetime-local string (giờ local) sang UTC ISO: `new Date(localString).toISOString()`
   - Gọi `POST /api/polls`
3. Thành công → lưu `code` vào `localStorage['createdPolls']` → chuyển sang `/analytics?code=482931`
4. Thất bại → hiện toast error

**Dữ liệu được lưu vào DB:**

Với `Multiple Choice` — "Best framework?", options: Vue.js, React, Angular:
```
Polls: (Id=1, Code="482931", Question="Best framework?", QuestionType="Multiple Choice",
        Status="Active", ExpireAt="2126-08-02T...", CreatedAt="2026-08-02T06:30:00Z")
Options: (Id=1, PollId=1, Text="Vue.js")
         (Id=2, PollId=1, Text="React")
         (Id=3, PollId=1, Text="Angular")
```

Với `Yes / No` — "Do you like Vue?":
```
Polls: (Id=2, Code="193847", Question="Do you like Vue?", QuestionType="Yes / No", ...)
Options: (Id=4, PollId=2, Text="Yes")   ← backend tự tạo
         (Id=5, PollId=2, Text="No")    ← backend tự tạo
```

Với `Rating` — "Rate this presentation":
```
Polls: (Id=3, Code="857291", Question="Rate this presentation", QuestionType="Rating", ...)
Options: (không có gì)
```

Với `Open Text` — "Any suggestions?":
```
Polls: (Id=4, Code="374819", Question="Any suggestions?", QuestionType="Open Text", ...)
Options: (không có gì)
```

---

### 8.2 Vote — cả 4 loại

**Frontend (VoteView.vue):**

1. Load trang `/vote/482931`:
   - `GET /api/polls/check/482931` — nếu lỗi → "Poll Not Found"
   - Check `localStorage['voted_482931'] === 'true'` → nếu đúng: "Already Voted"
   - Poll hợp lệ, chưa vote → hiện form

2. User điền câu trả lời:
   - Multiple Choice / Yes-No: bấm chọn 1 option → `selectedOptionId = option.id`
   - Rating: bấm sao số N → `voteValue = "N"` (chuỗi)
   - Open Text: gõ vào textarea → `voteValue = "nội dung"`

3. Bấm "Submit Vote":
   - Validate: phải chọn/nhập gì đó → `hasSubmitError = true` nếu chưa
   - Gọi `POST /api/votes`:
     ```json
     Multiple Choice: { pollCode:"482931", voterToken:"voter_47291038", optionId:2, voteValue:"" }
     Yes / No:        { pollCode:"193847", voterToken:"voter_47291038", optionId:5, voteValue:"" }
     Rating:          { pollCode:"857291", voterToken:"voter_47291038", optionId:0, voteValue:"4" }
     Open Text:       { pollCode:"374819", voterToken:"voter_47291038", optionId:0, voteValue:"Tôi thích Vue" }
     ```
   - Thành công:
     - `localStorage['voted_482931'] = 'true'`
     - Hiện màn hình "Vote Recorded!"
   - Lỗi "already" (server phát hiện) → hiện "Already Voted"
   - Lỗi khác → bật `hasSubmitError = true`

**Dữ liệu được lưu vào DB sau mỗi loại vote:**

```
Multiple Choice (chọn "React" = Id:2):
  Votes: (PollCode="482931", OptionId=2, VoteValue="", VoterToken="voter_47291038", CreatedAt=now)
  Analytics: (PollCode="482931", OptionId=2, VoteTime=now)

Yes / No (chọn "Yes" = Id:4):
  Votes: (PollCode="193847", OptionId=4, VoteValue="", VoterToken="voter_47291038", CreatedAt=now)
  Analytics: (PollCode="193847", OptionId=4, VoteTime=now)

Rating (chọn 4 sao):
  Votes: (PollCode="857291", OptionId=0, VoteValue="4", VoterToken="voter_47291038", CreatedAt=now)
  Analytics: (PollCode="857291", OptionId=0, VoteTime=now)

Open Text (nhập "Tôi thích Vue"):
  Votes: (PollCode="374819", OptionId=0, VoteValue="Tôi thích Vue", VoterToken="voter_47291038", CreatedAt=now)
  Analytics: (PollCode="374819", OptionId=0, VoteTime=now)
```

---

### 8.3 Đóng Poll (Stop)

1. Creator bấm "Stop" trong AnalyticsView → modal xác nhận
2. Bấm "Stop Now":
   - `PUT /api/polls/code/482931` với body `{ ...poll, status: "Closed" }`
   - Backend cập nhật DB: `Polls.Status = "Closed"`
   - Backend phát hiện `statusIsChanging = true` → gọi VoteService
   - VoteService nhận → broadcast SignalR `"PollClosed"` đến group `poll_482931`
3. Kết quả tức thì:
   - **AnalyticsView (creator):** `poll.value.status = 'Closed'` → badge đổi sang "Closed" đỏ, nút "Stop" ẩn
   - **VoteView (voter đang mở):** nhận event `PollClosed` → `isPollExpired()` → hiện banner đỏ "This poll has ended", ẩn form vote
4. Sau khi đóng: Voters vào link cũ → VoteView load poll → `status !== "Active"` → hiện "Closed"

---

### 8.4 Xóa Poll (Delete)

1. Creator bấm "Delete" → modal xác nhận
2. Bấm "Delete":
   - `DELETE /api/polls/code/482931`
   - Backend xóa Polls + Options (cascade trong PollDB)
   - Backend gọi VoteService: `DELETE /api/votes/by-poll-code/482931`
   - VoteService xóa tất cả Votes có `PollCode = "482931"` trong VoteDB
   - AnalyticsDB **không xóa** (audit log giữ lại)
3. Frontend sau khi API thành công:
   - Xóa `"482931"` khỏi `localStorage['createdPolls']`
   - Toast "Poll deleted."
   - `router.push('/')` → về trang chủ
4. Nếu ai đó vào link vote cũ → PollService trả 404 → VoteView hiện "Poll Not Found"
5. Nếu ai đó vào `/analytics?code=482931` → code không còn trong `localStorage` → "Access Denied"

---

## 9. Realtime với SignalR

### Tại sao cần SignalR?

HTTP thông thường là **client kéo** (client hỏi, server trả lời). Để xem kết quả live, phải refresh liên tục — tốn băng thông, chậm. SignalR là **server đẩy** — khi có vote mới, server chủ động gửi dữ liệu đến tất cả trình duyệt đang mở mà không cần ai hỏi.

### Cơ chế kết nối

SignalR tự chọn transport tốt nhất:
1. **WebSocket** (ưu tiên) — kết nối 2 chiều liên tục, latency thấp nhất
2. **Server-Sent Events** — server đẩy 1 chiều, fallback nếu WebSocket bị block
3. **Long Polling** — HTTP polling "giả lập" realtime, fallback cuối cùng

### VoteHub (`VoteService/Hubs/VoteHub.cs`)

Hub là class trung tâm của SignalR server. Mỗi client kết nối có 1 `ConnectionId` duy nhất. Client có thể join **Group** để nhận broadcast theo nhóm.

```csharp
public class VoteHub : Hub
{
    // Client gọi: connection.invoke("JoinPollRoom", "482931")
    // → Server thêm ConnectionId này vào group "poll_482931"
    // → Server gửi confirm "JoinedRoom" về client đó
    public async Task JoinPollRoom(string pollCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        await Clients.Caller.SendAsync("JoinedRoom", pollCode);
    }

    // Client gọi khi unmount component → giải phóng connection khỏi group
    public async Task LeavePollRoom(string pollCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
    }

    // Broadcast từ client đến group (thường dùng từ server controller thay vì client)
    public async Task BroadcastVoteUpdate(string pollCode, object voteData)
    {
        await Clients.Group($"poll_{pollCode}").SendAsync("VoteUpdated", voteData);
    }
}
```

**Sơ đồ Groups:**
```
poll_482931 (group)
  ├── ConnectionId: "a3f8..." (AnalyticsView - creator)
  ├── ConnectionId: "b7c2..." (VoteView - voter đang mở)
  └── ConnectionId: "d9e1..." (VoteView - voter khác)

poll_193847 (group)
  └── ConnectionId: "f2a9..." (AnalyticsView - creator khác)
```

### Broadcast từ VoteService Controller

Server-side code không kế thừa Hub, không thể gọi method của Hub trực tiếp. Dùng `IHubContext<VoteHub>` — được inject vào Controller:

```csharp
// Sau khi lưu vote xong:
await _signalRHubContext.Clients
    .Group($"poll_{voteData.PollCode}")
    .SendAsync("VoteUpdated", new {
        pollCode = voteData.PollCode,
        totalVotes = totalVotesForThisPoll,
        voteResults = allVotesForThisPoll  // [{optionId, voteCount}]
    });
```

### `usePollHub.js` — Composable phía Frontend

File `src/usePollHub.js` đóng gói toàn bộ logic SignalR thành 1 composable (hàm có thể tái sử dụng trong nhiều component):

```js
export function usePollHub(pollCode, onVoteUpdated) {
  const connected = ref(false)
  let connection = null

  const start = async () => {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:5002/hubs/vote')
      // Tự reconnect sau: 0ms, 1s, 3s, 5s rồi dừng
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Đăng ký nhận các events từ server
    connection.on('VoteUpdated', notify)   // có vote mới
    connection.on('PollClosed', notify)    // poll bị đóng

    // Cập nhật badge "Live"/"Connecting..." khi state thay đổi
    connection.onreconnecting(() => { connected.value = false })
    connection.onreconnected(() => {
      connected.value = true
      connection.invoke('JoinPollRoom', pollCode)  // rejoin group sau reconnect
    })
    connection.onclose(() => { connected.value = false })

    await connection.start()
    await connection.invoke('JoinPollRoom', pollCode)  // join group
    connected.value = true
  }

  const stop = async () => {
    connection.invoke('LeavePollRoom', pollCode)  // rời group
    await connection.stop()                        // đóng kết nối
    connected.value = false
  }

  onUnmounted(stop)  // tự dọn dẹp khi component bị gỡ khỏi DOM

  return { connected, start, stop }
}
```

**Dùng trong AnalyticsView:**
```js
const { connected: isHubConnected, start: startHub } = usePollHub(pollCode, async (data) => {
  // Callback này chạy khi nhận VoteUpdated hoặc PollClosed
  totalVotes.value = data.total
  await loadResults()  // gọi lại REST API để lấy chi tiết kết quả
})

onMounted(async () => {
  // Load dữ liệu lần đầu, sau đó mới start SignalR
  await loadResults()
  startHub()
})
```

### Fallback khi SignalR offline

```js
// Cứ 6 giây: nếu WebSocket mất kết nối → gọi REST API thủ công
setInterval(() => {
  if (!isHubConnected.value) loadResults()
}, 6000)
```

Đảm bảo kết quả vẫn được cập nhật dù môi trường không hỗ trợ WebSocket.

---

## 10. Frontend — Giải thích từng file

### `main.js` — Entry point

File đầu tiên được chạy khi browser load app. Lắp ráp tất cả "mảnh ghép":

```js
import { createApp } from 'vue'
import App from './App.vue'         // Root component
import router from './router'       // Routing
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import './assets/main.css'          // Global CSS

const app = createApp(App)
app.use(router)  // Đăng ký router → <router-link>, useRouter(), useRoute() dùng được ở mọi component
app.use(Toast, { position: 'bottom-right', timeout: 2500, ... })  // Toast notification
app.mount('#app')  // Gắn Vue app vào <div id="app"> trong public/index.html
```

---

### `App.vue` — Root component (Khung xương)

Không chứa logic nghiệp vụ. Chỉ là "vỏ bọc" bao quanh tất cả trang:

```html
<router-view v-slot="{ Component }">
  <transition name="fade" mode="out-in">
    <component :is="Component" />  <!-- Trang hiện tại render ở đây -->
  </transition>
</router-view>
```

- `<router-view>` là "ổ cắm" — Vue Router tự thay component bên trong khi URL đổi
- `<transition name="fade">` thêm animation fade (định nghĩa trong `main.css`) khi chuyển trang
- `mode="out-in"` — trang cũ fade out xong, trang mới mới fade in (không chồng nhau)

---

### `router/index.js` — Điều hướng

```js
const routes = [
  { path: '/',           component: HomeView,       meta: { title: 'PollBuilder' } },
  { path: '/create',     component: CreatePollView, meta: { title: 'Create Poll' } },
  { path: '/vote/:code?',component: VoteView,       meta: { title: 'Vote' } },
  // :code? = tham số tùy chọn. /vote/123456 → code='123456'. /vote → code=undefined
  { path: '/analytics',  component: AnalyticsView,  meta: { title: 'Analytics & Results' } },
  // analytics dùng query string: /analytics?code=123456
  { path: '/:pathMatch(.*)*', redirect: '/' },  // URL không khớp → về trang chủ
]

// beforeEach chạy trước mỗi lần chuyển trang → update title tab trình duyệt
router.beforeEach(to => {
  document.title = to.meta.title || 'Poll Survey'
})
```

**Scroll behavior:** `scrollBehavior() { return { top: 0 } }` — tự cuộn lên đầu trang khi chuyển route.

---

### `api.js` — HTTP client

Tạo 1 Axios instance dùng chung cho toàn app:

```js
const apiClient = axios.create({
  baseURL: 'https://localhost:5000',      // OcelotGateway
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000,                         // 10s timeout
})

// Interceptor: chuẩn hóa error → component chỉ cần catch(e) → e.message
apiClient.interceptors.response.use(
  res => res,
  err => {
    const msg = err.response?.data?.message || err.message || 'Server connection error.'
    return Promise.reject(new Error(msg))
  }
)

export const pollApi = {
  getPollByCode: code => apiClient.get(`/api/polls/code/${code}`),
  checkPoll:     code => apiClient.get(`/api/polls/check/${code}`),
  createPoll:    data => apiClient.post('/api/polls', data),
  updatePoll:    (code, data) => apiClient.put(`/api/polls/code/${code}`, data),
  deletePoll:    code => apiClient.delete(`/api/polls/code/${code}`),
  submitVote:    data => apiClient.post('/api/votes', data),
  getVoteResults:code => apiClient.get(`/api/votes/result/${code}`),
  getVoteTotal:  code => apiClient.get(`/api/votes/total/${code}`),
  getVoteList:   code => apiClient.get(`/api/votes/list/${code}`),
}
```

---

### `voterToken.js` — Token định danh người dùng

Tạo và lưu token ngẫu nhiên vào localStorage — dùng để server chặn vote 2 lần:

```js
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token')
  if (token === null) {
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      randomPart += Math.floor(Math.random() * 10)  // 8 chữ số ngẫu nhiên
    }
    token = 'voter_' + randomPart  // "voter_47291038"
    localStorage.setItem('poll_voter_token', token)
  }
  return token
}
```

---

### `usePollHub.js` — SignalR composable

Xem phần [Realtime với SignalR](#9-realtime-với-signalr) ở trên.

---

### `views/HomeView.vue` — Trang chủ

**Giao diện:** Hero title + 2 card (Join Poll | Create Poll) + How It Works

**Các hàm quan trọng:**

```js
// joinPoll — xử lý khi bấm "Join Room"
const joinPoll = async () => {
  if (code.value.length < 6) {
    codeError.value = 'Please enter all 6 digits'
    return
  }
  joinLoading.value = true
  try {
    await pollApi.checkPoll(code.value)   // GET /api/polls/check/{code}
    // Thành công → chuyển đến trang vote
    router.push(`/vote/${code.value}`)
  } catch {
    codeError.value = 'Poll not found'    // API lỗi → hiện lỗi dưới input
  } finally {
    joinLoading.value = false
  }
}
```

**Test cases:**
- Nhập code < 6 chữ số → "Please enter all 6 digits" (validate trước khi gọi API)
- Nhập code 6 chữ số hợp lệ → gọi API → thành công → chuyển sang `/vote/{code}`
- Nhập code 6 chữ số nhưng poll không tồn tại / đóng / hết hạn → API lỗi → "Poll not found"

---

### `views/CreatePollView.vue` — Tạo Poll

**State quan trọng:**
```js
const form = ref({
  question: '',
  questionType: 'Multiple Choice',
  expireAt: getDefaultExpireDate(),   // 5 phút sau (default cho datetime-local input)
  options: [{ text: '' }, { text: '' }]  // tối thiểu 2
})
const expireMode = ref('none')  // 'none' | 'custom'
```

**Hàm `addOption` / `removeOption`:**
```js
const addOption = () => {
  if (form.value.options.length < 6) form.value.options.push({ text: '' })
}
const removeOption = (index) => {
  if (form.value.options.length > 2) form.value.options.splice(index, 1)
}
```

**Hàm `submit` — các bước:**
1. Validate question + đủ options
2. `roomCode = Math.floor(100000 + Math.random() * 900000).toString()`
3. Convert thời gian: `expireMode === 'custom'` → `localDateTimeToUtcIso(form.expireAt)` hoặc `100 năm sau`
4. `POST /api/polls` → nhận lại poll với Id
5. `saveCreatedPollCode(code)` → `localStorage['createdPolls'].push(code)`
6. `router.push('/analytics?code=' + code)`

**Test cases:**
- Gửi form với question rỗng → toast "Please enter a question."
- Multiple Choice với < 2 options có nội dung → toast "Need at least 2 valid options."
- Chọn "Set Deadline" nhưng không đổi giờ → expireAt = giờ mặc định (5 phút sau)
- Tạo thành công → toast "Poll created!" → redirect sang analytics

---

### `views/VoteView.vue` — Bỏ phiếu

**5 trạng thái UI (v-if/v-else-if theo thứ tự):**

```
pollNotFound   = true  → "Poll Not Found" card
alreadyVoted   = true  → "Already Voted" card
voteSubmitted  = true  → "Vote Recorded!" card
poll           ≠ null  → Form vote (theo questionType)
else                   → Form nhập code thủ công
```

**Các biến form:**
```js
const selectedOptionId = ref(null)  // MC/YN: ID option được chọn
const voteValue = ref('')           // Rating: "1"~"5", OpenText: nội dung
const hasSubmitError = ref(false)   // true = hiện thông báo lỗi đỏ
const isSubmitting = ref(false)     // true = disable nút, hiện spinner
```

**Hàm `submitVote` — validate theo loại:**
```js
if (type === 'Multiple Choice' || type === 'Yes / No') {
  if (selectedOptionId.value === null) { hasSubmitError = true; return }
} else if (type === 'Rating') {
  if (voteValue.value === '') { hasSubmitError = true; return }
} else if (type === 'Open Text') {
  if (voteValue.value.trim() === '') { hasSubmitError = true; return }
}
```

**Test cases:**
- Vào `/vote` không có code → hiện form nhập code
- Vào `/vote/XXXXXX` code sai → "Poll Not Found"
- Poll đã đóng → banner đỏ "This poll has ended", nút Submit ẩn
- `localStorage['voted_XXXXXX'] = 'true'` → "Already Voted" (không gọi API)
- Bấm Submit chưa chọn gì → error đỏ "Please select an option"
- Submit thành công → "Vote Recorded!" + lưu localStorage
- Đang mở VoteView khi creator đóng poll → nhận SignalR `PollClosed` → banner đỏ hiện ngay

---

### `views/AnalyticsView.vue` — Dashboard kết quả

**Kiểm tra quyền (onMounted):**
```js
const savedCodes = localStorage.getItem('createdPolls')
const createdPollCodes = JSON.parse(savedCodes || '[]')
if (!createdPollCodes.includes(pollCode)) {
  accessDenied.value = true  // hiện màn hình khóa
  return
}
```

**Hàm `loadResults` — render theo loại:**
```js
// Multiple Choice / Yes-No:
const voteCountList = (await pollApi.getVoteResults(pollCode)).data
// → map thêm tên option từ poll.options
const resultsWithName = voteCountList.map(item => ({
  optionId: item.optionId,
  optionText: poll.value.options.find(o => o.id === item.optionId)?.text ?? '(unknown)',
  count: item.count
}))
resultsWithName.sort((a, b) => b.count - a.count)  // sort giảm dần
choiceResults.value = resultsWithName

// Rating: getVoteList → mảng { voteValue: "4" } → render hàng sao
// Open Text: getVoteList → filter voteValue không rỗng → render text cards
```

**QR Code (2 canvas — thumbnail và modal):**
```js
// Thumbnail (100×100px): render sau khi poll load xong
setTimeout(() => renderQRCode(qrThumbnailCanvas.value, 100), 100)

// Modal (320×320px): render sau khi Vue tạo canvas trong modal DOM
const openQRModal = () => {
  showQRModal.value = true
  setTimeout(() => renderQRCode(qrLargeCanvas.value, 320), 50)
}
```
Cần `setTimeout` vì Vue cần 1 "tick" để tạo `<canvas>` trong DOM sau khi `v-if` bật lên.

**Test cases:**
- Vào `/analytics?code=XXX` không phải creator → "Access Denied"
- Bấm "Stop" → confirm modal → bấm "Stop Now" → badge đổi "Closed", nút Stop ẩn
- Bấm "Delete" → confirm modal → bấm "Delete" → redirect về `/`
- Có người vote → bar chart cập nhật ngay không cần refresh
- Bấm "Copy Link" → toast "Link copied!" (clipboard API)
- Bấm "Vote Page" → mở tab mới `/vote/{code}`
- Bấm QR thumbnail → modal QR phóng to 320×320px

---

### `assets/main.css` — Design system

Định nghĩa CSS variables (màu sắc, radius, shadow) và component classes tái sử dụng:

```css
:root {
  --blue: #2563eb;           /* màu chính */
  --text: #0f172a;           /* màu chữ đậm */
  --bg: #f1f5f9;             /* màu nền trang */
  --border: #e2e8f0;         /* màu viền */
  --green: #16a34a;          /* badge Active, vote recorded */
  --red: #dc2626;            /* badge Closed, lỗi, xóa */
  --radius: 8px;             /* bo góc cơ bản */
}

/* Component classes (dùng như Tailwind nhưng cho UI pattern lặp lại): */
.btn, .btn-primary, .btn-red, .btn-ghost, .btn-outline  /* buttons */
.card                                                     /* hộp trắng có viền */
.badge, .badge-blue, .badge-green, .badge-red             /* tag nhỏ */
.vote-option, .vote-option.selected                       /* option radio tùy chỉnh */
.bar-track, .bar-fill                                     /* thanh kết quả */
.modal-bg, .modal-box                                     /* popup */
.spinner, .live-dot, .live-badge                          /* loading, realtime indicator */
.code-input                                               /* ô nhập 6 số (font lớn, monospace) */
.fade-enter-active, .fade-leave-to                        /* animation chuyển trang */
```

---

## 11. VoterToken — Chống vote 2 lần

Dự án không có đăng nhập nhưng vẫn ngăn 1 người vote 2 lần bằng cách kết hợp **localStorage** (client) và **DB check** (server).

### Dữ liệu lưu trong localStorage

| Key | Ví dụ giá trị | Mục đích |
|---|---|---|
| `poll_voter_token` | `"voter_47291038"` | Token định danh thiết bị, tạo 1 lần, dùng mãi |
| `voted_482931` | `"true"` | Đánh dấu đã vote poll `482931` |
| `createdPolls` | `'["482931","193847"]'` | Danh sách poll mình đã tạo (kiểm tra quyền creator) |

### Luồng chống trùng

```
Lần 1 (chưa vote):
  localStorage['poll_voter_token'] không có
  → Tạo "voter_47291038" → lưu vào localStorage
  → Gửi vote kèm token
  → Server kiểm tra DB: không tìm thấy (PollCode="482931", VoterToken="voter_47291038")
  → Lưu vote → thành công
  → localStorage['voted_482931'] = 'true'

Lần 2 (cùng trình duyệt):
  VoteView load → check localStorage['voted_482931'] === 'true'
  → Hiện "Already Voted" ngay, không gọi API

Lần 2 (localStorage bị xóa nhưng token vẫn còn):
  → Gửi vote lên server với token cũ
  → Server tìm thấy (PollCode="482931", VoterToken="voter_47291038")
  → 400 "You have already voted."
  → VoteView hiện "Already Voted"

Lần 2 (xóa hoàn toàn cache, token mới):
  → Token mới "voter_93847271" → gửi vote
  → Server không tìm thấy → cho vote
  (Hạn chế chấp nhận được của giải pháp không có account)
```

---

## 12. Migration files — EF Core

### EF Core Migrations là gì?

EF Core Migrations là hệ thống quản lý schema database bằng code (code-first). Thay vì viết SQL `CREATE TABLE` thủ công, bạn:
1. Định nghĩa Model class (C#)
2. Chạy `dotnet ef migrations add TenMigration` → EF Core **tự generate file migration** mô tả cần làm gì với DB
3. Chạy `dotnet ef database update` → EF Core **chạy migration** để áp dụng thay đổi vào DB

### Giải thích từng file trong `Migrations/`

Lấy ví dụ PollService (`Migrations/20260724153429_InitialCreate.cs`):

```csharp
public partial class InitialCreate : Migration
{
    // Up() chạy khi "dotnet ef database update" (áp dụng migration)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Tạo bảng Polls
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),  // AUTO INCREMENT
                Code = table.Column<string>(nullable: false),
                ...
            },
            constraints: table => {
                table.PrimaryKey("PK_Polls", x => x.Id);
            });

        // Tạo bảng Options
        migrationBuilder.CreateTable(name: "Options", ...);

        // Tạo Foreign Key: Options.PollId → Polls.Id (CASCADE DELETE)
        table.ForeignKey(
            name: "FK_Options_Polls_PollId",
            column: x => x.PollId,
            principalTable: "Polls",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        // Tạo Index trên Options.PollId (tăng tốc JOIN)
        migrationBuilder.CreateIndex(
            name: "IX_Options_PollId",
            table: "Options",
            column: "PollId");
    }

    // Down() chạy khi rollback migration (undo)
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Options");  // phải xóa Options trước (FK)
        migrationBuilder.DropTable(name: "Polls");
    }
}
```

### File `*ModelSnapshot.cs`

`PollDbContextModelSnapshot.cs` — EF Core dùng để biết trạng thái **hiện tại** của schema. Khi chạy `migrations add` lần sau, EF so sánh Model class với Snapshot để biết cần thêm/sửa/xóa gì. File này được update tự động, không sửa tay.

### File `*.Designer.cs`

`20260724153429_InitialCreate.Designer.cs` — metadata của migration, dùng để biết migration này được apply ở version schema nào. Không sửa tay.

### Lệnh Migration quan trọng

```bash
# Tạo migration mới (khi thêm field vào Model)
dotnet ef migrations add TenMigration --project PollService

# Apply migration vào DB (tạo/cập nhật bảng)
dotnet ef database update --project PollService

# Xem trạng thái migrations nào đã apply
dotnet ef migrations list --project PollService

# Rollback về migration trước
dotnet ef database update TenMigrationTruoc --project PollService

# Xóa migration chưa apply
dotnet ef migrations remove --project PollService

# Cài dotnet-ef tool (nếu chưa có)
dotnet tool install --global dotnet-ef
```

---

## 13. Dockerfile & Containerization

Mỗi service có 1 `Dockerfile` riêng. Không có `docker-compose.yml` (chạy độc lập từng container).

### Dockerfile mẫu (format chuẩn .NET 8)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base   # runtime image
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build      # build image
WORKDIR /src
COPY ["PollService/PollService.csproj", "PollService/"]
RUN dotnet restore "PollService/PollService.csproj"
COPY . .
WORKDIR "/src/PollService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final                                   # final image nhỏ (chỉ có runtime)
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PollService.dll"]
```

### Build Docker image

```bash
# Build từ root của solution (cần context rộng vì Dockerfile dùng COPY . .)
docker build -t pollservice -f PollService/Dockerfile .
docker build -t voteservice -f VoteService/Dockerfile .
docker build -t analyticsservice -f AnalyticsService/Dockerfile .
docker build -t ocelotgateway -f OcelotGateway/Dockerfile .
```

### Chạy container

```bash
# Chạy PollService (container kết nối SQL Server ở host)
docker run -d -p 5001:80 \
  -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal;Database=PollDB;..." \
  pollservice

# Tương tự cho VoteService (:5002), AnalyticsService (:5003), OcelotGateway (:5000)
```

> Hiện tại dự án chưa có `docker-compose.yml`. Để chạy toàn bộ với Docker, cần tạo compose file hoặc chạy thủ công từng container.

### Export / Import Docker image (chuyển máy)

```bash
# Export image ra file .tar
docker save -o pollservice.tar pollservice
docker save -o voteservice.tar voteservice
docker save -o analyticsservice.tar analyticsservice
docker save -o ocelotgateway.tar ocelotgateway

# Import trên máy khác
docker load -i pollservice.tar
docker load -i voteservice.tar
docker load -i analyticsservice.tar
docker load -i ocelotgateway.tar

# Chạy bình thường sau khi import
docker run -d -p 5001:80 pollservice
```

### Cài Docker

- **Windows:** Tải [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop/) → cài đặt → restart
- **Kiểm tra sau cài:** `docker --version` → `Docker version 26.x.x`

---

## 14. Hướng dẫn chạy dự án

### Yêu cầu môi trường

| Tool | Version | Cài ở đâu |
|---|---|---|
| .NET 8 SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8 |
| Node.js | 18+ | https://nodejs.org/ |
| npm | đi kèm Node.js | — |
| SQL Server LocalDB | Đi kèm Visual Studio | Hoặc cài riêng: SQL Server Express |
| dotnet-ef tool | latest | `dotnet tool install --global dotnet-ef` |

Kiểm tra:
```bash
dotnet --version    # phải >= 8.0
node --version      # phải >= 18
npm --version
dotnet ef           # nếu lỗi: dotnet tool install --global dotnet-ef
```

---

### Bước 1: Clone và restore

```bash
git clone https://github.com/your-repo/poll-survey.git
cd poll-survey

# Restore .NET packages cho tất cả projects
dotnet restore PollSurvey.sln

# Restore frontend packages
cd client
npm install
cd ..
```

---

### Bước 2: Tạo Database (chỉ lần đầu)

```bash
# PollService → tạo PollDB với bảng Polls và Options
cd PollService
dotnet ef database update

# VoteService → tạo VoteDB với bảng Votes
cd ..\VoteService
dotnet ef database update

# AnalyticsService → tạo AnalyticsDB với bảng Analytics
cd ..\AnalyticsService
dotnet ef database update

cd ..
```

Nếu LocalDB chưa có, lệnh này tự tạo file database tại `(localdb)\mssqllocaldb`.  
Kiểm tra DB đã tạo: mở **SQL Server Object Explorer** trong Visual Studio → `(localdb)\mssqllocaldb` → `Databases`.

---

### Bước 3: Chạy Backend (4 terminal riêng)

```bash
# Terminal 1
cd PollService && dotnet run
# Chạy tại: https://localhost:5001

# Terminal 2
cd VoteService && dotnet run
# Chạy tại: https://localhost:5002

# Terminal 3
cd AnalyticsService && dotnet run
# Chạy tại: https://localhost:5003

# Terminal 4 (sau khi 3 service trên đã khởi động)
cd OcelotGateway && dotnet run
# Chạy tại: https://localhost:5000
```

---

### Bước 4: Chạy Frontend

```bash
cd client
npm run serve
# Dev server tại: http://localhost:8080
```

---

### Bước 5: Mở app

Truy cập `http://localhost:8080` trên trình duyệt.

**Chạy nhanh bằng Visual Studio:**
1. Mở `PollSurvey.sln`
2. Chuột phải Solution → **Properties** → **Multiple Startup Projects**
3. Set tất cả 4 project backend → **Start**
4. Bấm F5
5. Chạy riêng frontend: `cd client && npm run serve`

**Swagger (test API):**
- `https://localhost:5001/swagger` — PollService
- `https://localhost:5002/swagger` — VoteService  
- `https://localhost:5003/swagger` — AnalyticsService

---

## 15. Chuyển máy / Deploy sang máy khác

### Cách 1: Copy source code (khuyến nghị để dev)

**Máy nguồn:**
```bash
# Đảm bảo .gitignore loại trừ: bin/, obj/, node_modules/, .vs/
git add .
git commit -m "latest changes"
git push
```

**Máy đích:**
```bash
# 1. Cài đặt tools (xem phần Yêu cầu môi trường)

# 2. Clone code
git clone https://github.com/your-repo/poll-survey.git
cd poll-survey

# 3. Restore dependencies
dotnet restore PollSurvey.sln
cd client && npm install && cd ..

# 4. Tạo database (SQL Server LocalDB phải được cài sẵn)
cd PollService && dotnet ef database update && cd ..
cd VoteService && dotnet ef database update && cd ..
cd AnalyticsService && dotnet ef database update && cd ..

# 5. Chạy (4 terminal như Bước 3 ở trên)
```

**Lưu ý Connection String:** File `appsettings.json` mỗi service có:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PollDB;..."
```
Nếu máy đích dùng SQL Server khác (không phải LocalDB), sửa connection string cho phù hợp.

---

### Cách 2: Build production và copy file

**Máy nguồn — Build:**
```bash
# Build frontend
cd client
npm run build
# Output: client/dist/

# Build và publish backend (tạo thư mục chạy được, không cần cài SDK trên máy đích)
dotnet publish PollService -c Release -o ./publish/PollService
dotnet publish VoteService -c Release -o ./publish/VoteService
dotnet publish AnalyticsService -c Release -o ./publish/AnalyticsService
dotnet publish OcelotGateway -c Release -o ./publish/OcelotGateway
```

**Máy đích:**
```bash
# Chỉ cần cài .NET 8 Runtime (không cần SDK hay Node.js)
# Tải: https://dotnet.microsoft.com/download/dotnet/8

# Copy thư mục publish/ sang máy đích

# Cập nhật connection string trong appsettings.json của từng service

# Tạo database (phải có dotnet-ef hoặc chạy script SQL thủ công)

# Chạy:
./publish/PollService/PollService.exe        # Windows
./publish/VoteService/VoteService.exe
./publish/AnalyticsService/AnalyticsService.exe
./publish/OcelotGateway/OcelotGateway.exe
```

---

### Cách 3: Docker (nếu cần chạy trong container)

```bash
# Máy nguồn — Build và export images
docker build -t pollservice -f PollService/Dockerfile .
docker build -t voteservice -f VoteService/Dockerfile .
docker build -t analyticsservice -f AnalyticsService/Dockerfile .
docker build -t ocelotgateway -f OcelotGateway/Dockerfile .

docker save -o images.tar pollservice voteservice analyticsservice ocelotgateway

# Máy đích
# Cài Docker Desktop
docker load -i images.tar

# Chạy (cần SQL Server riêng, cập nhật connection string qua -e)
docker run -d -p 5001:80 -e "ConnectionStrings__DefaultConnection=Server=...;Database=PollDB;..." pollservice
docker run -d -p 5002:80 -e "ConnectionStrings__DefaultConnection=Server=...;Database=VoteDB;..." \
           -e "Services__PollServiceUrl=http://host.docker.internal:5001" \
           -e "Services__AnalyticsServiceUrl=http://host.docker.internal:5003" voteservice
docker run -d -p 5003:80 -e "ConnectionStrings__DefaultConnection=Server=...;Database=AnalyticsDB;..." analyticsservice
docker run -d -p 5000:80 ocelotgateway
```

---

### Troubleshooting thường gặp

**`dotnet ef` không tìm thấy:**
```bash
dotnet tool install --global dotnet-ef
# Restart terminal sau khi cài
```

**Lỗi SSL certificate (HTTPS localhost):**
```bash
dotnet dev-certs https --trust
# Bấm Yes để trust certificate
```

**Frontend không kết nối được backend:**
- Kiểm tra cả 4 service đã chạy chưa
- Kiểm tra `api.js` `baseURL: 'https://localhost:5000'` khớp với OcelotGateway port
- Kiểm tra CORS: PollService chỉ cho phép `http://localhost:8080`

**SignalR không kết nối được:**
- VoteService CORS phải cho phép cả `http://localhost:8080` và `AllowCredentials()`
- `usePollHub.js` hard-code `https://localhost:5002` — nếu VoteService chạy port khác phải sửa

**LocalDB không chạy:**
```bash
# Khởi động LocalDB instance
sqllocaldb start mssqllocaldb

# Kiểm tra trạng thái
sqllocaldb info mssqllocaldb
```

---

## Tóm tắt nhanh các lệnh

```bash
# Cài tools lần đầu
dotnet tool install --global dotnet-ef
cd client && npm install && cd ..

# Tạo DB lần đầu
dotnet ef database update --project PollService
dotnet ef database update --project VoteService
dotnet ef database update --project AnalyticsService

# Chạy dev (4 terminal)
cd PollService && dotnet run          # :5001
cd VoteService && dotnet run          # :5002
cd AnalyticsService && dotnet run     # :5003
cd OcelotGateway && dotnet run        # :5000
cd client && npm run serve            # :8080

# Build production frontend
cd client && npm run build

# Build production backend
dotnet publish PollSurvey.sln -c Release -o ./publish
```
