# 📊 Hệ Thống Poll & Survey Realtime

Ứng dụng khảo sát và bỏ phiếu trực tuyến với cập nhật kết quả theo thời gian thực, xây dựng bằng **ASP.NET Core Microservices**, **Vue 3**, **SignalR WebSocket** và **MySQL**.

---

## 📖 Mục Lục

- [Giới Thiệu Dự Án](#-giới-thiệu-dự-án)
- [Tính Năng Nổi Bật](#-tính-năng-nổi-bật)
- [Kiến Trúc Hệ Thống](#-kiến-trúc-hệ-thống-giải-thích-chi-tiết)
- [Cơ Sở Dữ Liệu](#-cơ-sở-dữ-liệu-giải-thích-chi-tiết)
- [Luồng Hoạt Động](#-luồng-hoạt-động-của-ứng-dụng)
- [API Documentation](#-api-endpoints-chi-tiết)
- [Hướng Dẫn Cài Đặt](#-hướng-dẫn-cài-đặt-local)
- [Deploy với Docker](#-deploy-với-docker)
- [Công Nghệ Sử Dụng](#-công-nghệ-sử-dụng)

---

## 🎯 Giới Thiệu Dự Án

**Poll & Survey Builder** là một ứng dụng web cho phép bạn tạo các cuộc khảo sát, bỏ phiếu trực tuyến một cách nhanh chóng và xem kết quả cập nhật theo thời gian thực. 

### Ứng dụng phù hợp cho:
- 👨‍🏫 **Giáo viên** muốn tạo bài kiểm tra nhanh trong lớp học
- 💼 **Doanh nghiệp** cần thu thập ý kiến nhân viên hoặc khách hàng
- 🎤 **MC sự kiện** muốn tương tác với khán giả
- 👥 **Nhóm bạn** muốn quyết định nhanh một vấn đề

### Điểm đặc biệt:
- ✅ **KHÔNG CẦN ĐĂNG NHẬP** - Người vote chỉ cần mã phòng 6 chữ số
- ⚡ **Cập nhật REALTIME** - Kết quả hiển thị ngay lập tức khi có người vote
- 📱 **Chia sẻ dễ dàng** - Tạo QR code để người khác quét và tham gia
- 🔒 **Chống vote trùng** - Mỗi trình duyệt chỉ được vote 1 lần

---

## ✨ Tính Năng Nổi Bật

### 1. **Bốn Loại Câu Hỏi**
- **📊 Multiple Choice (Chọn 1 trong nhiều)**: Ví dụ - "Ngôn ngữ lập trình yêu thích của bạn?" → Python, JavaScript, Java, C#...
- **✅ Yes/No (Có/Không)**: Ví dụ - "Bạn có đồng ý với đề xuất này?"
- **⭐ Rating (Đánh giá sao)**: Ví dụ - "Đánh giá dịch vụ của chúng tôi từ 1-5 sao"
- **💬 Open Text (Văn bản tự do)**: Ví dụ - "Góp ý của bạn cho sản phẩm?"

### 2. **Xác Thực Người Vote Không Cần Đăng Nhập**
- Hệ thống tạo một **"voter token"** duy nhất cho mỗi trình duyệt (dựa trên browser fingerprint)
- Token này được lưu trong `localStorage` để ngăn chặn vote nhiều lần
- Người dùng chỉ cần mã phòng 6 chữ số để tham gia

### 3. **Cập Nhật Realtime với SignalR**
- Khi có người vote, **TẤT CẢ** người đang xem kết quả sẽ thấy cập nhật ngay lập tức
- Sử dụng **WebSocket** thay vì phải refresh trang
- Auto reconnect khi mất kết nối

### 4. **Quản Lý Thời Hạn Poll**
- Có thể đặt deadline cho poll (ví dụ: kết thúc sau 1 giờ)
- Hoặc để poll mở mãi mãi (100 năm)
- Poll tự động đóng khi hết hạn

### 5. **Dashboard Analytics cho Creator**
- Xem tổng số vote
- Xem phân bố kết quả (biểu đồ)
- Xem danh sách tất cả các vote (với timestamp)
- Đóng poll thủ công hoặc xóa poll

### 6. **QR Code Sharing**
- Tạo QR code ngay trong trang analytics
- Người khác quét QR code → truy cập trực tiếp vào trang vote

---

## 🏗️ Kiến Trúc Hệ Thống (Giải Thích Chi Tiết)

Dự án này sử dụng **kiến trúc Microservices** - tức là chia nhỏ ứng dụng thành nhiều service độc lập, mỗi service đảm nhiệm một nhiệm vụ riêng.

```
┌─────────────────────────────────────────────────────────────┐
│                    VUE 3 CLIENT (Frontend)                  │
│                   http://localhost:8081                     │
│                                                             │
│  - Giao diện người dùng                                     │
│  - Tạo poll, vote, xem kết quả                              │
│  - Kết nối WebSocket để nhận realtime update                │
└────────────────────────┬────────────────────────────────────┘
                         │
                         │ HTTP REST API + WebSocket
                         ↓
┌─────────────────────────────────────────────────────────────┐
│         OCELOT GATEWAY (API Gateway - Cổng vào duy nhất)    │
│                 http://localhost:5000                       │
│                                                             │
│  - Nhận request từ frontend                                 │
│  - Định tuyến request đến đúng service:                     │
│    • /api/polls/*  → PollService                            │
│    • /api/votes/*  → VoteService                            │
│    • /hubs/vote    → VoteService (WebSocket)                │
└───────────┬──────────────────────────┬──────────────────────┘
            │                          │
            ↓                          ↓
┌──────────────────────┐    ┌──────────────────────┐
│   POLLSERVICE        │    │  VOTESERVICE         │
│   localhost:5001     │    │  localhost:5002      │
│                      │    │                      │
│  NHIỆM VỤ:           │    │  NHIỆM VỤ:           │
│  • Tạo poll mới      │    │  • Nhận vote         │
│  • Lấy thông tin poll│    │  • Lưu vote vào DB   │
│  • Cập nhật poll     │    │  • Tính toán kết quả │
│  • Xóa poll          │◄───┤  • Gửi realtime      │
│  • Validate dữ liệu  │    │    update (SignalR)  │
│                      │    │  • Kiểm tra trùng    │
│  DATABASE: PollDB    │    │  DATABASE: VoteDB    │
└──────────────────────┘    └──────────────────────┘
            │                          │
            └──────────┬───────────────┘
                       ↓
               ┌────────────────┐
               │  MYSQL SERVER  │
               │                │
               │  PollDB:       │
               │   - Polls      │
               │   - Options    │
               │                │
               │  VoteDB:       │
               │   - Votes      │
               └────────────────┘
```

### Giải Thích Từng Thành Phần:

#### 1. **Vue 3 Client (Frontend)**
- **Vai trò**: Giao diện mà người dùng tương tác
- **Công nghệ**: Vue 3 (framework JavaScript), Tailwind CSS (styling)
- **Chức năng**:
  - Trang tạo poll: `/create`
  - Trang vote: `/vote/:code`
  - Trang xem analytics: `/analytics?code=123456`
  - Trang home: `/`

#### 2. **Ocelot Gateway**
- **Vai trò**: Cổng vào duy nhất cho tất cả request từ frontend
- **Lý do cần Gateway**:
  - Frontend chỉ cần biết 1 địa chỉ duy nhất thay vì 2 địa chỉ của PollService và VoteService
  - Dễ thêm authentication, rate limiting sau này
  - Load balancing khi scale
- **Hoạt động**: 
  - Nhận request → Xem URL → Chuyển tiếp đến service tương ứng

#### 3. **PollService (Microservice 1)**
- **Vai trò**: Quản lý thông tin poll
- **Database**: PollDB (chứa bảng `Polls` và `Options`)
- **API Endpoints**:
  - `POST /api/polls` - Tạo poll mới
  - `GET /api/polls/code/{code}` - Lấy thông tin poll
  - `GET /api/polls/check/{code}` - Kiểm tra poll còn valid không
  - `PUT /api/polls/code/{code}` - Cập nhật poll (đóng poll)
  - `DELETE /api/polls/code/{code}` - Xóa poll

#### 4. **VoteService (Microservice 2)**
- **Vai trò**: Quản lý vote và realtime updates
- **Database**: VoteDB (chứa bảng `Votes`)
- **API Endpoints**:
  - `POST /api/votes` - Submit vote
  - `GET /api/votes/{pollCode}` - Lấy kết quả vote
  - `DELETE /api/votes?pollCode={code}` - Xóa tất cả vote (internal)
- **SignalR Hub**: `/hubs/vote` - WebSocket cho realtime updates

#### 5. **MySQL Database**
- **Vai trò**: Lưu trữ dữ liệu vĩnh viễn
- **Tách biệt 2 database**: Mỗi service có database riêng (theo nguyên tắc microservices)

---

## 🗄️ Cơ Sở Dữ Liệu (Giải Thích Chi Tiết)

### **Tại Sao Tách Làm 2 Database?**

Trong microservices, mỗi service nên có database riêng để:
- Mỗi service có thể thay đổi schema của mình mà không ảnh hưởng service khác
- Dễ scale riêng lẻ (ví dụ: vote nhiều hơn poll, ta chỉ cần scale VoteDB)
- Tránh "single point of failure" - nếu PollDB chết thì VoteService vẫn hoạt động

---

### **Database 1: PollDB (thuộc về PollService)**

#### **Bảng: Polls** (Lưu thông tin các poll)

| Cột | Kiểu Dữ Liệu | Mô Tả Chi Tiết |
|-----|--------------|----------------|
| `Id` | INT (Primary Key, Auto Increment) | ID duy nhất cho mỗi poll, tự động tăng |
| `Code` | VARCHAR(255) (UNIQUE) | Mã phòng 6 chữ số (ví dụ: "143829"), người vote dùng mã này để join |
| `Question` | VARCHAR(500) | Câu hỏi của poll (ví dụ: "Ngôn ngữ lập trình yêu thích?") |
| `QuestionType` | VARCHAR(50) | Loại câu hỏi: "Multiple Choice", "Yes / No", "Rating", "Open Text" |
| `Status` | VARCHAR(50) (Default: "Active") | Trạng thái: "Active" (đang mở) hoặc "Closed" (đã đóng) |
| `ExpireAt` | DATETIME | Thời điểm poll hết hạn (UTC timezone) |
| `CreatedAt` | DATETIME | Thời điểm tạo poll (UTC timezone) |

**Indexes:**
- Index trên `Code` để tìm kiếm poll nhanh
- Index trên `Status` để lọc poll đang active

#### **Bảng: Options** (Lưu các lựa chọn cho Multiple Choice)

| Cột | Kiểu Dữ Liệu | Mô Tả Chi Tiết |
|-----|--------------|----------------|
| `Id` | INT (Primary Key, Auto Increment) | ID duy nhất cho mỗi option |
| `PollId` | INT (Foreign Key → Polls.Id) | Poll nào sở hữu option này |
| `Text` | VARCHAR(255) | Nội dung của option (ví dụ: "Python", "JavaScript") |

**Quan Hệ:**
- Khi xóa một Poll → Tất cả Options của poll đó cũng bị xóa (ON DELETE CASCADE)

**Lưu Ý Quan Trọng:**
- **Multiple Choice**: Có 2-6 options lưu trong bảng này
- **Yes/No, Rating, Open Text**: KHÔNG có options trong database (xử lý ở frontend)

#### **Ví Dụ Dữ Liệu:**

**Bảng Polls:**
```
Id | Code   | Question                  | QuestionType     | Status | ExpireAt            | CreatedAt
---|--------|---------------------------|------------------|--------|---------------------|--------------------
1  | 143829 | Ngôn ngữ yêu thích?       | Multiple Choice  | Active | 2026-08-10 00:00:00 | 2026-08-04 10:00:00
2  | 567890 | Bạn có đồng ý?            | Yes / No         | Active | 2026-08-15 00:00:00 | 2026-08-04 11:00:00
```

**Bảng Options (cho poll 143829):**
```
Id | PollId | Text
---|--------|------------
1  | 1      | Python
2  | 1      | JavaScript
3  | 1      | C#
```

---

### **Database 2: VoteDB (thuộc về VoteService)**

#### **Bảng: Votes** (Lưu tất cả các vote)

| Cột | Kiểu Dữ Liệu | Mô Tả Chi Tiết |
|-----|--------------|----------------|
| `Id` | INT (Primary Key, Auto Increment) | ID duy nhất cho mỗi vote |
| `PollCode` | VARCHAR(255) | Mã poll (soft reference - không dùng Foreign Key để tách biệt services) |
| `OptionId` | INT | ID của option được chọn (Multiple Choice) hoặc 0 (các loại khác) |
| `VoteValue` | VARCHAR(500) | Giá trị vote: "1"/"0" (Yes/No), "1"-"5" (Rating), hoặc text (Open Text) |
| `VoterToken` | VARCHAR(255) | Token duy nhất của người vote (tạo từ browser fingerprint) |
| `CreatedAt` | DATETIME | Thời điểm vote (UTC timezone) |

**Indexes:**
- Compound index trên `(PollCode, VoterToken)` để kiểm tra vote trùng nhanh
- Index trên `PollCode` để tính toán kết quả nhanh

#### **Cách Lưu Vote Theo Loại Câu Hỏi:**

| Loại Câu Hỏi | OptionId | VoteValue | Ví Dụ Cụ Thể |
|--------------|----------|-----------|--------------|
| **Multiple Choice** | ID từ bảng Options | `""` (empty) | `OptionId=2, VoteValue=""` (chọn JavaScript) |
| **Yes/No** | `0` | `"1"` (Yes) hoặc `"0"` (No) | `OptionId=0, VoteValue="1"` (Yes) |
| **Rating** | `0` | `"1"` đến `"5"` | `OptionId=0, VoteValue="4"` (4 sao) |
| **Open Text** | `0` | Text của user | `OptionId=0, VoteValue="Tuyệt vời!"` |

#### **Chống Vote Trùng:**
- Có unique constraint trên `(PollCode, VoterToken)`
- Nếu cùng một token cố vote lại → Database reject

#### **Ví Dụ Dữ Liệu:**

**Poll 143829 (Multiple Choice: Ngôn ngữ yêu thích?)**
```
Id | PollCode | OptionId | VoteValue | VoterToken        | CreatedAt
---|----------|----------|-----------|-------------------|--------------------
1  | 143829   | 2        |           | voter_abc123      | 2026-08-04 11:00:00
2  | 143829   | 1        |           | voter_def456      | 2026-08-04 11:05:00
3  | 143829   | 2        |           | voter_ghi789      | 2026-08-04 11:10:00
```
→ Kết quả: JavaScript (2 vote), Python (1 vote)

**Poll 567890 (Yes/No: Bạn có đồng ý?)**
```
Id | PollCode | OptionId | VoteValue | VoterToken        | CreatedAt
---|----------|----------|-----------|-------------------|--------------------
4  | 567890   | 0        | 1         | voter_abc123      | 2026-08-04 12:00:00
5  | 567890   | 0        | 0         | voter_def456      | 2026-08-04 12:05:00
6  | 567890   | 0        | 1         | voter_ghi789      | 2026-08-04 12:10:00
```
→ Kết quả: Yes (2 vote), No (1 vote)

---

## 🔄 Luồng Hoạt Động Của Ứng Dụng

### **Luồng 1: Tạo Poll**

```
┌──────────┐     POST /api/polls        ┌─────────┐      INSERT      ┌─────────┐
│  Vue 3   │ ─────────────────────────> │  Poll   │ ──────────────> │  PollDB │
│  Client  │                            │ Service │                 │         │
└──────────┘ <───────────────────────── └─────────┘ <────────────── └─────────┘
             Poll data + Code 143829              Success            (Polls table)
```

**Chi Tiết:**
1. User nhập câu hỏi + chọn loại poll + thêm options (nếu Multiple Choice)
2. Frontend tạo random 6-digit code (ví dụ: 143829)
3. Gửi `POST /api/polls` với payload:
   ```json
   {
     "code": "143829",
     "question": "Ngôn ngữ yêu thích?",
     "questionType": "Multiple Choice",
     "expireAt": "2026-08-10T00:00:00Z",
     "options": [
       {"text": "Python"},
       {"text": "JavaScript"},
       {"text": "C#"}
     ]
   }
   ```
4. PollService lưu vào database → Trả về poll đã tạo
5. Frontend chuyển đến trang Analytics với mã 143829

---

### **Luồng 2: Vote**

```
┌──────────┐  GET /api/polls/check/143829  ┌─────────┐     SELECT     ┌─────────┐
│  Vue 3   │ ──────────────────────────────> │  Poll   │ ────────────> │  PollDB │
│  Client  │                                │ Service │                │         │
└──────────┘ <────────────────────────────── └─────────┘ <──────────── └─────────┘
             Poll data (if valid)                       Poll 143829
```

**Sau đó submit vote:**

```
┌──────────┐   POST /api/votes    ┌─────────┐  1. Check với  ┌─────────┐
│  Vue 3   │ ──────────────────> │  Vote   │   PollService  │  Poll   │
│  Client  │                     │ Service │ <─────────────> │ Service │
└────┬─────┘ <──────────────────── └────┬────┘                └─────────┘
     │       Success                   │
     │                                 │ 2. INSERT
     │                                 ↓
     │                            ┌─────────┐
     │                            │  VoteDB │
     │                            └────┬────┘
     │                                 │
     │       3. SignalR Broadcast      │
     │       "VoteUpdated" event       │
     └─────<──────────────────────────┘
     ↓
 ┌──────────────────────────────────┐
 │  All clients watching this poll  │
 │  update charts automatically     │
 └──────────────────────────────────┘
```

**Chi Tiết:**
1. User truy cập `/vote/143829` → Frontend gọi `GET /api/polls/check/143829` để xác minh poll còn valid
2. PollService kiểm tra:
   - Poll có tồn tại không?
   - Status còn "Active" không?
   - ExpireAt chưa quá hạn chưa?
3. Nếu OK → Hiển thị form vote
4. User chọn option và bấm Submit → Frontend gọi `POST /api/votes`:
   ```json
   {
     "pollCode": "143829",
     "optionId": 2,
     "voteValue": "",
     "voterToken": "voter_abc123"
   }
   ```
5. VoteService:
   - Gọi lại PollService để double-check poll vẫn còn valid
   - Kiểm tra `voterToken` đã vote chưa (query VoteDB)
   - Nếu chưa → INSERT vote vào database
   - **QUAN TRỌNG**: Broadcast event "VoteUpdated" qua SignalR Hub
6. **TẤT CẢ** client đang kết nối WebSocket tới poll này nhận được event → Update UI realtime

---

### **Luồng 3: Xem Analytics Realtime**

```
┌──────────┐   1. HTTP GET results   ┌─────────┐    SELECT    ┌─────────┐
│  Vue 3   │ ──────────────────────> │  Vote   │ ──────────> │  VoteDB │
│  Client  │ <────────────────────── │ Service │ <────────── │         │
└────┬─────┘   Initial data          └─────────┘   Aggregate  └─────────┘
     │
     │ 2. WebSocket connect to /hubs/vote
     ↓
┌─────────────────────────────┐
│   SignalR Hub (VoteService) │
│                             │
│   connection.invoke(        │
│     'JoinPollRoom',         │
│     '143829'                │
│   )                         │
└────────┬────────────────────┘
         │
         │ Listen for "VoteUpdated" event
         ↓
┌──────────────────────────────────┐
│  When new vote comes:            │
│  Hub broadcasts to all clients   │
│  in room "143829"                │
│                                  │
│  Frontend updates chart without  │
│  page refresh                    │
└──────────────────────────────────┘
```

**Chi Tiết:**
1. User truy cập `/analytics?code=143829`
2. Frontend:
   - Gọi `GET /api/votes/143829` để lấy dữ liệu ban đầu
   - Khởi tạo SignalR connection: `new HubConnectionBuilder().withUrl('/hubs/vote')`
   - Gọi `connection.invoke('JoinPollRoom', '143829')` để join room
   - Lắng nghe event: `connection.on('VoteUpdated', (data) => { updateChart(data) })`
3. Khi có vote mới → Hub gửi data mới xuống → Chart tự động update

---

## 🔌 API Endpoints Chi Tiết

Dưới đây là tất cả các API endpoints mà ứng dụng sử dụng, giải thích chi tiết về request/response.

---

## 🛠️ Hướng Dẫn Cài Đặt Local

### **Yêu Cầu Hệ Thống**

- **.NET 8 SDK** - [Download tại đây](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download tại đây](https://nodejs.org/)
- **MySQL Server** - [Download tại đây](https://dev.mysql.com/downloads/mysql/)

---

### **Bước 1: Clone Repository**

```bash
git clone https://github.com/yourusername/poll-survey.git
cd poll-survey
```

---

### **Bước 2: Cấu Hình Database**

#### **Tạo Database Trên MySQL**

Chạy file `database.sql`:

```bash
mysql -u root -p < database.sql
```

Hoặc copy nội dung `database.sql` và chạy trong MySQL Workbench.

---

#### **Cấu Hình Connection String**

Sửa file `appsettings.json` trong cả `PollService` và `VoteService`:

**PollService/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=PollDB;User=root;Password=your_password;AllowPublicKeyRetrieval=true;"
  },
  "AllowedOrigins": [
    "http://localhost:8081"
  ]
}
```

**VoteService/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=VoteDB;User=root;Password=your_password;AllowPublicKeyRetrieval=true;"
  },
  "Services": {
    "PollServiceUrl": "http://localhost:5001"
  },
  "AllowedOrigins": [
    "http://localhost:8081"
  ]
}
```

**Lưu Ý:** Thay `your_password` bằng password MySQL của bạn

---

### **Bước 3: Chạy Backend Services**

Mở **4 terminal** riêng biệt:

**Terminal 1 - OcelotGateway:**
```bash
cd OcelotGateway
dotnet run
```
→ Chạy tại: `http://localhost:5000`

**Terminal 2 - PollService:**
```bash
cd PollService
dotnet run
```
→ Chạy tại: `http://localhost:5001`

**Terminal 3 - VoteService:**
```bash
cd VoteService
dotnet run
```
→ Chạy tại: `http://localhost:5002`

**Kiểm tra Backend:**
- PollService Swagger: `http://localhost:5001/swagger`
- VoteService Swagger: `http://localhost:5002/swagger`

---

### **Bước 4: Chạy Frontend**

**Terminal 4:**
```bash
cd client
npm install
npm run serve
```
→ Chạy tại: `http://localhost:8081`

---

### **Bước 5: Truy Cập Ứng Dụng**

Mở trình duyệt:
- **Homepage**: http://localhost:8081
- **Tạo Poll**: http://localhost:8081/create
- **Vote**: http://localhost:8081/vote/123456 (thay 123456 bằng mã poll)

---

## 🐳 Deploy Với Docker

Docker giúp chạy toàn bộ ứng dụng với 1 lệnh duy nhất.

### **Bước 1: Tạo File `.env`**

Tạo file `.env` ở thư mục gốc:

```env
# MySQL Connection Strings
POLL_DB_CONNECTION=Server=YOUR_HOST;Port=3306;Database=PollDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;
VOTE_DB_CONNECTION=Server=YOUR_HOST;Port=3306;Database=VoteDB;User=YOUR_USER;Password=YOUR_PASSWORD;AllowPublicKeyRetrieval=true;

# Public URLs (người dùng truy cập)
GATEWAY_PUBLIC_URL=http://localhost:5000
VOTE_SERVICE_PUBLIC_URL=http://localhost:5002
FRONTEND_URL=http://localhost:8081
```

**Lưu Ý:** 
- `YOUR_HOST`: Địa chỉ MySQL server (có thể là `host.docker.internal` nếu MySQL chạy trên máy host)
- `YOUR_USER`, `YOUR_PASSWORD`: Thông tin đăng nhập MySQL

---

### **Bước 2: Build và Chạy**

```bash
docker-compose up -d --build
```

Lệnh này sẽ:
- Build tất cả services (Gateway, PollService, VoteService, Client)
- Chạy các container
- Expose các port: 5000 (Gateway), 5001 (PollService), 5002 (VoteService), 8080 (Client)

---

### **Bước 3: Kiểm Tra**

```bash
docker-compose ps
```

---

### **Dừng Containers**

```bash
docker-compose down
```

---

## 💻 Công Nghệ Sử Dụng

### **Backend**
- **ASP.NET Core 8.0** - Framework chính
- **Entity Framework Core 8.0** - ORM để tương tác với MySQL
- **MySQL 8.0** - Database
- **SignalR** - WebSocket cho realtime updates
- **Ocelot** - API Gateway
- **Swagger/OpenAPI** - API documentation

### **Frontend**
- **Vue 3** - Progressive JavaScript Framework
- **Vue Router** - Client-side routing
- **Axios** - HTTP client
- **@microsoft/signalr** - SignalR client library
- **Tailwind CSS** - Utility-first CSS framework
- **QRCode.js** - Tạo QR code
- **Lucide Icons** - Icon library
- **Vue Toastification** - Toast notifications

### **DevOps**
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration

---

## 📂 Cấu Trúc Thư Mục Dự Án

```
poll-survey/
│
├── PollService/                    # Microservice quản lý polls
│   ├── Controllers/
│   │   └── PollsController.cs      # API endpoints: Create, Read, Update, Delete polls
│   ├── Data/
│   │   └── PollDbContext.cs        # EF Core context cho PollDB
│   ├── Models/
│   │   ├── Poll.cs                 # Entity model cho Poll
│   │   └── Option.cs               # Entity model cho Option
│   ├── appsettings.json            # Cấu hình (connection string, CORS)
│   └── Program.cs                  # Entry point, khởi tạo services
│
├── VoteService/                    # Microservice quản lý votes & realtime
│   ├── Controllers/
│   │   └── VotesController.cs      # API endpoints: Submit vote, Get results
│   ├── Data/
│   │   └── VoteDbContext.cs        # EF Core context cho VoteDB
│   ├── Hubs/
│   │   └── VoteHub.cs              # SignalR Hub cho realtime updates
│   ├── Models/
│   │   └── Vote.cs                 # Entity model cho Vote
│   ├── appsettings.json            # Cấu hình
│   └── Program.cs                  # Entry point, khởi tạo SignalR
│
├── OcelotGateway/                  # API Gateway
│   ├── ocelot.json                 # Cấu hình routing (polls → 5001, votes → 5002)
│   ├── appsettings.json            # Cấu hình chung
│   └── Program.cs                  # Entry point, khởi tạo Ocelot
│
├── client/                         # Vue 3 Frontend
│   ├── src/
│   │   ├── views/
│   │   │   ├── HomeView.vue        # Trang chủ
│   │   │   ├── CreatePollView.vue  # Trang tạo poll
│   │   │   ├── VoteView.vue        # Trang vote
│   │   │   └── AnalyticsView.vue   # Trang xem kết quả realtime
│   │   ├── router/
│   │   │   └── index.js            # Vue Router config
│   │   ├── api.js                  # Axios HTTP client wrapper
│   │   ├── usePollHub.js           # SignalR connection composable
│   │   ├── voterToken.js           # Generate/retrieve voter token
│   │   └── App.vue                 # Root component
│   ├── public/
│   │   └── index.html              # HTML entry point
│   ├── package.json                # npm dependencies
│   └── vue.config.js               # Vue CLI config
│
├── database.sql                    # SQL script tạo PollDB và VoteDB
├── docker-compose.yml              # Docker orchestration config
├── .env                            # Environment variables (không commit)
├── .env.example                    # Template cho .env
└── README.md                       # File bạn đang đọc
```

---

## 🎓 Kiến Thức Học Được Từ Dự Án

Dự án này là một ví dụ tốt để học:

### **1. Kiến Trúc Microservices**
- Cách chia nhỏ ứng dụng thành các service độc lập
- Service communication (inter-service HTTP calls)
- Database per service pattern

### **2. API Gateway Pattern**
- Sử dụng Ocelot để routing requests
- Centralized entry point cho frontend
- CORS configuration

### **3. Realtime với WebSocket**
- SignalR Hub setup
- Broadcasting events đến nhiều clients
- Auto reconnect và error handling

### **4. RESTful API Design**
- CRUD operations
- HTTP status codes đúng chuẩn (200, 201, 204, 400, 404)
- Request/Response patterns

### **5. Vue 3 Composition API**
- Reactive state management với `ref()`
- Lifecycle hooks (`onMounted`, `onUnmounted`)
- Composables (reusable logic)

### **6. Database Design**
- Normalized schema
- Foreign keys và cascading deletes
- Indexes để tối ưu query

### **7. Docker & Containerization**
- Multi-stage builds
- Docker Compose orchestration
- Environment variables

---

## 🔒 Bảo Mật & Lưu Ý

### **Hiện Tại:**
- ✅ CORS được cấu hình để chỉ cho phép frontend truy cập
- ✅ Voter token chống vote trùng (mỗi browser chỉ 1 lần)
- ✅ Validation poll expiry và status

### **Cần Cải Thiện (Nếu Deploy Production):**
- 🔐 Thêm Authentication/Authorization cho creator (JWT tokens)
- 🔐 Rate limiting để chống spam
- 🔐 HTTPS cho tất cả connections
- 🔐 Input sanitization chống XSS
- 🔐 Parameterized queries đã có (EF Core tự động handle)
- 🔒 Secret management (Azure Key Vault, AWS Secrets Manager)

---