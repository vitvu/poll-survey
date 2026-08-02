# 📊 Poll Survey — Hướng dẫn chi tiết cho người mới

> **Mục tiêu tài liệu:** Giải thích mọi thứ từ đầu, cho người chưa biết gì về Microservices, SignalR, Docker.  
> **Tóm tắt dự án:** App tạo poll trực tuyến, không cần đăng ký, kết quả cập nhật realtime qua WebSocket.

---

## 📑 Nội dung

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Database Schema — Chi tiết từng bảng](#2-database-schema)
3. [Backend API — Request/Response từng endpoint](#3-backend-api)
4. [Ocelot Gateway — Routing chi tiết](#4-ocelot-gateway)
5. [SignalR Realtime — Cách hoạt động](#5-signalr-realtime)
6. [Frontend — Giải thích từng file](#6-frontend)
7. [Luồng nghiệp vụ chi tiết](#7-luồng-nghiệp-vụ)
8. [Migration Files — Tại sao có, tác dụng gì](#8-migration-files)
9. [Cài đặt Docker — Import/Export](#9-cài-đặt-docker)
10. [Chuyển code sang máy khác](#10-chuyển-code-sang-máy-khác)
11. [Các Port và URL](#11-các-port-và-url)

---

## 1. Tổng quan kiến trúc

### 1.1 Kiến trúc Microservices là gì?

Thay vì 1 app lớn làm hết mọi việc, ta chia thành nhiều service nhỏ độc lập:

```
┌─────────────────────────────────────────┐
│  Frontend (Vue.js)                      │
│  User nhìn thấy + tương tác            │
└──────────┬──────────────────────────────┘
           │ HTTP + WebSocket
           ▼
┌─────────────────────────────────────────┐
│  OcelotGateway (API Gateway)            │
│  Tiếp nhận mọi request, điều hướng     │
└──────────┬──────────────────────────────┘
           │ Forward theo URL pattern
      ┌────┼────┬───────────────────┐
      ▼         ▼                   ▼
┌──────────┐ ┌──────────┐  ┌──────────────┐
│PollSvc   │ │VoteSvc   │  │AnalyticsSvc  │
│Quản lý   │ │Quản lý   │  │Lưu log       │
│poll      │ │vote +    │  │vote (audit)  │
│          │ │realtime  │  │              │
└────┬─────┘ └────┬─────┘  └────┬─────────┘
     │            │              │
     ▼            ▼              ▼
[PollDB]      [VoteDB]      [AnalyticsDB]
 SQL Server    SQL Server    SQL Server
```

**Lợi ích:**
- Mỗi service có database riêng, crash 1 service không sập hết
- Deploy độc lập (sửa PollService không cần restart VoteService)
- Scale độc lập (nếu vote nhiều, chỉ cần thêm VoteService container)


### 1.2 Request Flow — Request đi từ đâu đến đâu?

**Ví dụ cụ thể:** User muốn xem kết quả poll code `123456`

```
Bước 1: User mở trang Analytics
  → Browser chạy: GET /analytics?code=123456

Bước 2: Vue Router nhận URL
  → Render component AnalyticsView.vue

Bước 3: AnalyticsView gọi API
  → pollApi.getPollByCode('123456')
  → Axios gửi: GET https://localhost:5000/api/polls/code/123456

Bước 4: OcelotGateway nhận request
  → Đọc file ocelot.json, tìm rule: "/api/polls/{everything}"
  → Forward đến: https://localhost:5001/api/Polls/code/123456

Bước 5: PollService nhận request
  → PollsController.GetPollByCode('123456')
  → Query database PollDB: SELECT * FROM Polls WHERE Code='123456'
  → Kèm JOIN: SELECT * FROM Options WHERE PollId=...
  → Trả về JSON: { id, code, question, options: [...] }

Bước 6: OcelotGateway trả về cho Vue
  → Vue nhận JSON → hiển thị tên poll, danh sách options
```

**3 loại communication:**
1. **User → Backend:** HTTP qua Axios (GET, POST, PUT, DELETE)
2. **Service → Service:** HTTP qua HttpClient (C#)
3. **Backend → User realtime:** WebSocket qua SignalR

---

## 2. Database Schema

Có 3 database hoàn toàn độc lập (mỗi service quản lý 1 database).  
**Nguyên tắc Microservices:** Service không được truy cập database của service khác.

### 2.1 PollDB (PollService quản lý)

#### Bảng `Polls`

Lưu thông tin câu hỏi khảo sát.

| Cột | Kiểu | Bắt buộc | Mô tả | Ví dụ |
|-----|------|----------|-------|-------|
| `Id` | int | PK, IDENTITY | ID tự động tăng | 1, 2, 3... |
| `Code` | nvarchar(max) | NOT NULL | Mã phòng 6 chữ số | "123456" |
| `Question` | nvarchar(max) | NOT NULL | Nội dung câu hỏi | "Best framework?" |
| `QuestionType` | nvarchar(max) | NOT NULL | Loại câu hỏi | "Multiple Choice" |
| `Status` | nvarchar(max) | NOT NULL | Trạng thái | "Active" hoặc "Closed" |
| `ExpireAt` | datetime2 | NOT NULL | Thời điểm hết hạn | "2026-08-10T12:00:00Z" |
| `CreatedAt` | datetime2 | NOT NULL | Thời điểm tạo | "2026-08-02T06:00:00Z" |

**Các giá trị `QuestionType`:**
- `"Multiple Choice"` — Chọn 1 trong nhiều option
- `"Yes / No"` — Chọn Yes hoặc No
- `"Rating"` — Chọn 1-5 sao
- `"Open Text"` — Trả lời tự do

**Các giá trị `Status`:**
- `"Active"` — Đang nhận vote
- `"Closed"` — Đã đóng, không nhận vote nữa


#### Bảng `Options`

Lưu các lựa chọn của câu hỏi Multiple Choice hoặc Yes/No.

| Cột | Kiểu | Mô tả | Ví dụ |
|-----|------|-------|-------|
| `Id` | int | PK, IDENTITY | 1, 2, 3... |
| `PollId` | int | FK → Polls.Id, ON DELETE CASCADE | 1 |
| `Text` | nvarchar(max) | Nội dung option | "Vue.js" |

**Quan hệ 1-nhiều:**
```
Poll (Id=1, Code="123456", Question="Best framework?")
  ├── Option (Id=1, PollId=1, Text="Vue.js")
  ├── Option (Id=2, PollId=1, Text="React")
  └── Option (Id=3, PollId=1, Text="Angular")
```

**CASCADE DELETE nghĩa là gì?**
Khi xóa Poll có Id=1 → SQL Server tự động xóa tất cả Option có PollId=1.  
Không cần code xóa thủ công.

**Lưu ý:**
- Poll loại `"Rating"` và `"Open Text"` không có Option nào trong bảng này
- Poll loại `"Yes / No"` → Backend tự tạo 2 Option: "Yes" và "No"

#### Index

EF Core tự tạo index `IX_Options_PollId` trên cột `PollId` để tăng tốc truy vấn:
```sql
SELECT * FROM Options WHERE PollId = 1
```

### 2.2 VoteDB (VoteService quản lý)

#### Bảng `Votes`

Lưu mỗi phiếu bầu.

| Cột | Kiểu | Mô tả | Ví dụ |
|-----|------|-------|-------|
| `Id` | int | PK, IDENTITY | 1, 2, 3... |
| `PollCode` | nvarchar(max) | Mã poll (không dùng FK) | "123456" |
| `OptionId` | int | ID option được chọn | 2 (hoặc 0 nếu Rating/Text) |
| `VoteValue` | nvarchar(max) | Giá trị vote (Rating/Text) | "4" hoặc "Tôi thích Vue" |
| `VoterToken` | nvarchar(max) | Token định danh voter | "voter_47291038" |
| `CreatedAt` | datetime2 | Thời điểm vote | "2026-08-02T07:00:00" |

**Tại sao không dùng Foreign Key đến PollDB?**

Trong Microservices, mỗi service có database riêng. Nếu dùng FK:
```sql
-- Cái này KHÔNG ĐƯỢC vì Polls table ở database khác
ALTER TABLE Votes ADD CONSTRAINT FK_Votes_Polls 
  FOREIGN KEY (PollId) REFERENCES Polls(Id)
```
SQL Server không cho phép FK cross-database. Thay vào đó, VoteService validate bằng cách gọi HTTP sang PollService.

**Mapping loại câu hỏi ↔ dữ liệu lưu:**

| Question Type | OptionId | VoteValue | Giải thích |
|---------------|----------|-----------|------------|
| Multiple Choice | 2 | `""` | Đã chọn option Id=2 |
| Yes / No | 1 | `""` | Đã chọn Yes (hoặc No) |
| Rating | 0 | `"4"` | Đã chọn 4 sao |
| Open Text | 0 | `"Vue tốt hơn"` | Câu trả lời tự do |

**Chống vote 2 lần:**

Trước khi lưu vote, VoteService query:
```sql
SELECT * FROM Votes 
WHERE PollCode='123456' AND VoterToken='voter_47291038'
```
Nếu tìm thấy → trả lỗi `400 "You have already voted."`


### 2.3 AnalyticsDB (AnalyticsService quản lý)

#### Bảng `Analytics`

Audit log — lưu vết mọi lần vote (write-only, không đọc realtime).

| Cột | Kiểu | Mô tả | Ví dụ |
|-----|------|-------|-------|
| `Id` | int | PK, IDENTITY | 1, 2, 3... |
| `PollCode` | nvarchar(max) | Mã poll | "123456" |
| `OptionId` | int | Option được chọn | 2 |
| `VoteTime` | datetime2 | Thời điểm ghi log | "2026-08-02T07:05:00" |

**Tác dụng:**
1. **Audit log:** Lưu vết mọi hành động vote (ai vote, khi nào, option gì)
2. **Phân tích sau:** Có thể export để xây dashboard thống kê nâng cao
3. **Backup:** Nếu VoteDB bị lỗi, vẫn có log trong AnalyticsDB

**Có thể bỏ không?**

✅ **CÓ** — VoteService gửi log qua HTTP nhưng dùng `fire-and-forget` (không chờ response):
```csharp
_ = SendVoteAnalyticsAsync(...);  // Dấu _ = không await
```
Nếu AnalyticsService down, vote vẫn được lưu trong VoteDB bình thường.

**Khi nào cần giữ lại:**
- Yêu cầu audit log (pháp lý, kiểm toán)
- Cần phân tích xu hướng vote theo thời gian
- Backup dữ liệu

---

## 3. Backend API

### 3.1 PollService — `/api/Polls`

#### `POST /api/polls` — Tạo poll mới

**Request Body:**
```json
{
  "code": "123456",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "expireAt": "2026-08-10T12:00:00Z",
  "options": [
    { "text": "Vue.js" },
    { "text": "React" }
  ]
}
```

**Xử lý backend (step-by-step):**

```
1. Validate question không rỗng
   → Nếu rỗng: 400 "Question cannot be empty."

2. Đánh dấu expireAt là UTC
   DateTime.SpecifyKind(pollData.ExpireAt, DateTimeKind.Utc)
   Vì sao? C# deserialize JSON thành DateTime kind "Unspecified"
   → Phải chỉ rõ là UTC để lưu đúng vào SQL Server

3. Validate expireAt > DateTime.UtcNow
   → Nếu không: 400 "Expiration date must be in the future."

4. Kiểm tra code chưa tồn tại
   SELECT * FROM Polls WHERE Code='123456'
   → Nếu có: 400 "Code already exists."

5. Sinh Options theo questionType:
   - "Multiple Choice" → dùng options từ request (phải >= 2)
   - "Yes / No" → bỏ qua options request, tạo [{Text:"Yes"}, {Text:"No"}]
   - "Rating" / "Open Text" → options = []

6. Set createdAt = DateTime.UtcNow, status = "Active"

7. INSERT vào database
   _db.Polls.Add(pollData)
   await _db.SaveChangesAsync()
   → EF Core tự sinh Id cho Poll và Options

8. Trả về 201 Created + Location header
```

**Response thành công `201 Created`:**
```json
{
  "id": 1,
  "code": "123456",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "status": "Active",
  "expireAt": "2026-08-10T12:00:00Z",
  "createdAt": "2026-08-02T06:30:00Z",
  "options": [
    { "id": 1, "pollId": 1, "text": "Vue.js" },
    { "id": 2, "pollId": 1, "text": "React" }
  ]
}
```
**Header:** `Location: /api/Polls/code/123456`

**Response thất bại:**
- `400 Bad Request` — `{ "message": "Question cannot be empty." }`
- `400 Bad Request` — `{ "message": "Expiration date must be in the future." }`
- `400 Bad Request` — `{ "message": "Code already exists." }`


#### `GET /api/polls/code/{code}` — Lấy thông tin poll đầy đủ

**Dùng ở đâu:** AnalyticsView cần lấy danh sách options để map với kết quả vote.

**Request:** `GET /api/polls/code/123456`

**Xử lý backend:**
```csharp
var poll = await _db.Polls
    .Include(p => p.Options)  // JOIN với bảng Options
    .FirstOrDefaultAsync(p => p.Code == code);

if (poll == null)
    return NotFound();

return Ok(poll);
```

**Response `200 OK`:**
```json
{
  "id": 1,
  "code": "123456",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "status": "Active",
  "expireAt": "2026-08-10T12:00:00Z",
  "createdAt": "2026-08-02T06:30:00Z",
  "options": [
    { "id": 1, "pollId": 1, "text": "Vue.js" },
    { "id": 2, "pollId": 1, "text": "React" }
  ]
}
```

**Response `404 Not Found`:** Poll không tồn tại.

---

#### `GET /api/polls/check/{code}` — Validate poll còn hoạt động

**Dùng ở đâu:**
- HomeView: User nhập code → kiểm tra poll tồn tại trước khi chuyển sang VoteView
- VoteView: Khi load trang → validate poll còn Active
- VoteService: Trước khi lưu vote → gọi HTTP sang endpoint này

**Request:** `GET /api/polls/check/123456`

**Xử lý backend:**
```
1. Query poll kèm options (giống endpoint trên)
2. Nếu không tìm thấy → 404 "Poll does not exist."
3. Nếu status != "Active" → 400 "Poll is closed."
4. Nếu expireAt <= DateTime.UtcNow → 400 "Poll has expired."
5. Nếu hợp lệ → 200 OK kèm data poll
```

**Response thành công `200 OK`:** (cùng format với endpoint trên)

**Response thất bại:**
- `404 Not Found` — `{ "message": "Poll does not exist." }`
- `400 Bad Request` — `{ "message": "Poll is closed." }` (status="Closed")
- `400 Bad Request` — `{ "message": "Poll has expired." }` (quá expireAt)

---

#### `PUT /api/polls/code/{code}` — Cập nhật poll (chủ yếu để đóng)

**Dùng ở đâu:** AnalyticsView khi creator bấm nút "Close Poll".

**Request:**
```http
PUT /api/polls/code/123456
Content-Type: application/json

{
  "id": 1,
  "code": "123456",
  "question": "Best JavaScript framework?",
  "questionType": "Multiple Choice",
  "status": "Closed",
  "expireAt": "2026-08-10T12:00:00Z",
  "options": []
}
```

**Xử lý backend:**
```
1. Tìm poll theo code → 404 nếu không có

2. Kiểm tra status thay đổi không
   bool statusChanged = (existingPoll.Status != newPoll.Status)

3. Cập nhật Status, Question, ExpireAt
   existingPoll.Status = newPoll.Status
   await _db.SaveChangesAsync()

4. Nếu statusChanged && newStatus == "Closed"
   → Gọi HTTP đến VoteService:
     POST /api/votes/broadcast-poll-closed
     Body: { "pollCode": "123456" }

   VoteService nhận → phát SignalR "PollClosed" đến tất cả client trong group

5. Trả về 204 No Content
```

**Điều gì xảy ra khi đóng poll?**
1. PollDB: `Status` đổi từ "Active" → "Closed"
2. VoteService phát SignalR → mọi client đang mở VoteView/AnalyticsView nhận event
3. VoteView: Hiện banner đỏ "This poll has ended", disable form vote
4. AnalyticsView: Badge status đổi thành "Closed"
5. Vote mới gửi lên → PollService trả 400 "Poll is closed"

**Response thành công:** `204 No Content`  
**Response thất bại:** `404 Not Found` — poll không tồn tại


#### `DELETE /api/polls/code/{code}` — Xóa poll vĩnh viễn

**Dùng ở đâu:** AnalyticsView khi creator bấm nút "Delete Poll".

**Request:** `DELETE /api/polls/code/123456`

**Xử lý backend:**
```
1. Tìm poll kèm options
   var poll = await _db.Polls
       .Include(p => p.Options)
       .FirstOrDefaultAsync(p => p.Code == code);

2. Nếu không tìm thấy → 404

3. Xóa poll (cascade sẽ xóa luôn options)
   _db.Polls.Remove(poll)
   await _db.SaveChangesAsync()

4. Gọi HTTP đến VoteService xóa votes:
   DELETE /api/votes/by-poll-code/123456
   → VoteService xóa tất cả Vote có PollCode="123456" trong VoteDB

5. Trả về 204 No Content
```

**Những gì bị xóa:**
| Database | Bảng | Hành động |
|----------|------|-----------|
| PollDB | `Polls` | Xóa row poll |
| PollDB | `Options` | Xóa tất cả options (CASCADE) |
| VoteDB | `Votes` | Xóa tất cả votes (qua HTTP call) |
| AnalyticsDB | `Analytics` | **KHÔNG xóa** (audit log giữ lại) |
| Frontend | `localStorage['createdPolls']` | Frontend xóa code khỏi mảng |

**Tại sao AnalyticsDB không xóa?**

Audit log giữ vĩnh viễn để kiểm toán. Nếu cần xóa, phải vào SQL Server xóa thủ công.

**Response thành công:** `204 No Content`  
**Response thất bại:** `404 Not Found`

---

### 3.2 VoteService — `/api/Votes`

#### `POST /api/votes` — Submit phiếu bầu (endpoint phức tạp nhất)

**Dùng ở đâu:** VoteView khi user bấm "Submit Vote".

**Request Body:**
```json
{
  "pollCode": "123456",
  "voterToken": "voter_47291038",
  "optionId": 2,
  "voteValue": ""
}
```

**Xử lý backend (chi tiết từng bước):**

```
──────────────────────────────────────────────
BƯỚC 1: Validate đầu vào
──────────────────────────────────────────────
if (string.IsNullOrWhiteSpace(voteData.PollCode))
    return BadRequest(new { message = "Missing required data." });

if (string.IsNullOrWhiteSpace(voteData.VoterToken))
    return BadRequest(new { message = "Missing required data." });

──────────────────────────────────────────────
BƯỚC 2: Chống vote 2 lần
──────────────────────────────────────────────
bool alreadyVoted = await _db.Votes.AnyAsync(v =>
    v.PollCode == voteData.PollCode &&
    v.VoterToken == voteData.VoterToken
);

if (alreadyVoted)
    return BadRequest(new { message = "You have already voted." });

──────────────────────────────────────────────
BƯỚC 3: Validate poll còn nhận vote không
──────────────────────────────────────────────
// Gọi HTTP sang PollService
var pollServiceUrl = _config["Services:PollServiceUrl"]; // "https://localhost:5001"
var response = await httpClient.GetAsync($"{pollServiceUrl}/api/Polls/check/{voteData.PollCode}");

if (!response.IsSuccessStatusCode)
    return BadRequest(new { message = "Poll is invalid or has been closed." });

// PollService trả về 200 OK → poll hợp lệ
// PollService trả về 400/404 → poll không hợp lệ (đóng/hết hạn/không tồn tại)

──────────────────────────────────────────────
BƯỚC 4: Lưu vote vào VoteDB
──────────────────────────────────────────────
voteData.CreatedAt = DateTime.Now;
_db.Votes.Add(voteData);
await _db.SaveChangesAsync();

──────────────────────────────────────────────
BƯỚC 5: Tính kết quả mới (để broadcast)
──────────────────────────────────────────────
var voteResults = await _db.Votes
    .Where(v => v.PollCode == voteData.PollCode)
    .GroupBy(v => v.OptionId)
    .Select(g => new {
        optionId = g.Key,
        voteCount = g.Count()
    })
    .ToListAsync();

int totalVotes = voteResults.Sum(r => r.voteCount);

──────────────────────────────────────────────
BƯỚC 6: Broadcast SignalR realtime
──────────────────────────────────────────────
await _hubContext.Clients
    .Group($"poll_{voteData.PollCode}")
    .SendAsync("VoteUpdated", new {
        pollCode = voteData.PollCode,
        totalVotes = totalVotes,
        voteResults = voteResults
    });

// Tất cả AnalyticsView đang mở poll này nhận event ngay lập tức

──────────────────────────────────────────────
BƯỚC 7: Fire-and-forget Analytics (không chờ)
──────────────────────────────────────────────
_ = SendVoteAnalyticsAsync(httpClient, voteData);

// Hàm SendVoteAnalyticsAsync():
//   POST https://localhost:5003/api/Analytics
//   Body: { pollCode, optionId, voteTime }
//   Bọc trong try-catch, nếu lỗi chỉ log warning, không làm fail request

──────────────────────────────────────────────
BƯỚC 8: Trả về thành công
──────────────────────────────────────────────
return Ok(new { message = "Vote submitted successfully!" });
```

**Response thành công `200 OK`:**
```json
{ "message": "Vote submitted successfully!" }
```

**Response thất bại:**
- `400 Bad Request` — `{ "message": "Missing required data." }`
- `400 Bad Request` — `{ "message": "You have already voted." }`
- `400 Bad Request` — `{ "message": "Poll is invalid or has been closed." }`


#### `GET /api/votes/result/{pollCode}` — Lấy kết quả nhóm theo option

**Dùng ở đâu:** AnalyticsView để vẽ bar chart (Multiple Choice, Yes/No).

**Request:** `GET /api/votes/result/123456`

**Xử lý backend:**
```csharp
var results = await _db.Votes
    .Where(v => v.PollCode == pollCode)
    .GroupBy(v => v.OptionId)
    .Select(g => new {
        optionId = g.Key,
        voteCount = g.Count()
    })
    .ToListAsync();

return Ok(results);
```

**SQL tương đương:**
```sql
SELECT OptionId, COUNT(*) as voteCount
FROM Votes
WHERE PollCode = '123456'
GROUP BY OptionId
```

**Response `200 OK`:**
```json
[
  { "optionId": 1, "voteCount": 3 },
  { "optionId": 2, "voteCount": 5 },
  { "optionId": 3, "voteCount": 2 }
]
```

**Frontend dùng như nào?**
```js
// AnalyticsView map optionId với tên option từ poll.options
const chartData = voteResults.map(r => ({
  name: poll.options.find(o => o.id === r.optionId)?.text,
  count: r.voteCount
}))
```

---

#### `GET /api/votes/total/{pollCode}` — Lấy tổng số phiếu

**Dùng ở đâu:** AnalyticsView hiển thị stat card "Total Votes".

**Request:** `GET /api/votes/total/123456`

**Xử lý backend:**
```csharp
int total = await _db.Votes.CountAsync(v => v.PollCode == pollCode);
return Ok(new { pollCode, totalVotes = total });
```

**Response `200 OK`:**
```json
{ "pollCode": "123456", "totalVotes": 10 }
```

---

#### `GET /api/votes/list/{pollCode}` — Lấy danh sách từng phiếu

**Dùng ở đâu:** AnalyticsView để hiển thị:
- **Rating:** Từng sao (để tính trung bình)
- **Open Text:** Từng câu trả lời

**Request:** `GET /api/votes/list/123456`

**Xử lý backend:**
```csharp
var votes = await _db.Votes
    .Where(v => v.PollCode == pollCode)
    .OrderByDescending(v => v.CreatedAt)
    .Select(v => new {
        optionId = v.OptionId,
        voteValue = v.VoteValue,
        createdAt = v.CreatedAt
    })
    .ToListAsync();

return Ok(votes);
```

**Response `200 OK` (Rating):**
```json
[
  { "optionId": 0, "voteValue": "5", "createdAt": "2026-08-02T07:05:00" },
  { "optionId": 0, "voteValue": "4", "createdAt": "2026-08-02T07:03:00" },
  { "optionId": 0, "voteValue": "3", "createdAt": "2026-08-02T07:01:00" }
]
```

**Response `200 OK` (Open Text):**
```json
[
  { "optionId": 0, "voteValue": "Vue is amazing", "createdAt": "2026-08-02T07:05:00" },
  { "optionId": 0, "voteValue": "I prefer React", "createdAt": "2026-08-02T07:03:00" }
]
```

**Frontend dùng như nào?**
```js
// Rating: tính trung bình
const avg = votes.reduce((sum, v) => sum + parseInt(v.voteValue), 0) / votes.length

// Open Text: hiển thị list
votes.forEach(v => {
  console.log(v.voteValue) // "Vue is amazing"
})
```

---

#### `DELETE /api/votes/by-poll-code/{pollCode}` — Xóa tất cả votes

**Dùng ở đâu:** PollService gọi qua HTTP khi xóa poll (inter-service call).

**Request:** `DELETE /api/votes/by-poll-code/123456`

**Xử lý backend:**
```csharp
var votes = await _db.Votes
    .Where(v => v.PollCode == pollCode)
    .ToListAsync();

_db.Votes.RemoveRange(votes);
await _db.SaveChangesAsync();

return NoContent();
```

**Response:** `204 No Content`

---

#### `POST /api/votes/broadcast-poll-closed` — Phát SignalR "Poll Closed"

**Dùng ở đâu:** PollService gọi qua HTTP khi đóng poll.

**Request Body:**
```json
{ "pollCode": "123456" }
```

**Xử lý backend:**
```csharp
if (string.IsNullOrWhiteSpace(request.PollCode))
    return BadRequest(new { message = "PollCode is required." });

await _hubContext.Clients
    .Group($"poll_{request.PollCode}")
    .SendAsync("PollClosed", new {
        pollCode = request.PollCode,
        status = "Closed"
    });

return Ok(new { message = "Broadcast sent." });
```

**Response:** `200 OK`

**Frontend nhận event:**
```js
// VoteView.vue
hubConnection.on('PollClosed', data => {
  showClosedBanner.value = true
  disableForm()
})

// AnalyticsView.vue
hubConnection.on('PollClosed', data => {
  poll.value.status = 'Closed'
})
```

---

### 3.3 AnalyticsService — `/api/Analytics`

#### `POST /api/analytics` — Ghi log vote (write-only)

**Dùng ở đâu:** VoteService gọi sau mỗi vote (fire-and-forget).

**Request Body:**
```json
{
  "pollCode": "123456",
  "optionId": 2,
  "voteTime": "2026-08-02T07:05:00Z"
}
```

**Xử lý backend:**
```csharp
if (record.VoteTime == default(DateTime))
    record.VoteTime = DateTime.Now;

_db.Analytics.Add(record);
await _db.SaveChangesAsync();

return Ok();
```

**Response:** `200 OK`

**Lưu ý:** Endpoint này **không được frontend gọi trực tiếp**. Chỉ VoteService gọi.

---

#### `GET /api/analytics/summary/{pollCode}` — Thống kê tổng hợp

**Dùng ở đâu:** Hiện tại frontend không dùng (dành cho mở rộng sau).

**Request:** `GET /api/analytics/summary/123456`

**Xử lý backend:**
```csharp
var records = await _db.Analytics
    .Where(a => a.PollCode == pollCode)
    .ToListAsync();

var mostVoted = records
    .GroupBy(a => a.OptionId)
    .OrderByDescending(g => g.Count())
    .Select(g => g.Key)
    .FirstOrDefault();

return Ok(new {
    totalVotes = records.Count,
    mostVotedOptionId = mostVoted
});
```

**Response `200 OK`:**
```json
{
  "totalVotes": 10,
  "mostVotedOptionId": 2
}
```

**Khác gì với VoteService?**

VoteService đọc từ `Votes` table (dữ liệu chính).  
AnalyticsService đọc từ `Analytics` table (audit log).  
Nếu cần so sánh 2 nguồn để phát hiện lỗi → dùng endpoint này.


---

## 4. Ocelot Gateway

### 4.1 Ocelot là gì?

**API Gateway** — điểm vào duy nhất cho frontend. Frontend chỉ biết địa chỉ Gateway, không cần biết có bao nhiêu service backend.

**Vai trò:**
1. **Reverse Proxy:** Nhận request từ client, forward đến đúng service
2. **Routing:** Đọc URL pattern để biết forward đến đâu
3. **Static File Server:** Serve file Vue build (production)
4. **CORS:** Cấu hình CORS tập trung

### 4.2 File cấu hình — `ocelot.json`

**Development (local):**
```json
{
  "GlobalConfiguration": {
    "BaseUrl": "https://localhost:5000"
  },
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/polls/{everything}",
      "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"],
      "DownstreamPathTemplate": "/api/Polls/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ]
    },
    {
      "UpstreamPathTemplate": "/api/votes/{everything}",
      "UpstreamHttpMethod": ["GET", "POST", "DELETE"],
      "DownstreamPathTemplate": "/api/Votes/{everything}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5002 }
      ]
    }
  ]
}
```

**Giải thích từng trường:**

| Trường | Ý nghĩa | Ví dụ |
|--------|---------|-------|
| `UpstreamPathTemplate` | URL pattern mà Gateway nhận | `/api/polls/{everything}` |
| `{everything}` | Placeholder bắt phần còn lại của URL | `/api/polls/check/123` → `{everything}` = `check/123` |
| `UpstreamHttpMethod` | HTTP method cho phép | `["GET", "POST"]` |
| `DownstreamPathTemplate` | URL forward đến service | `/api/Polls/{everything}` |
| `DownstreamScheme` | Giao thức | `https` hoặc `http` |
| `DownstreamHostAndPorts` | Địa chỉ service backend | `localhost:5001` |

### 4.3 Ví dụ routing chi tiết

#### Request 1: Tạo poll

```
┌───────────────────────────────────────────────────────────────┐
│ Frontend (Vue)                                                │
├───────────────────────────────────────────────────────────────┤
│ pollApi.createPoll({...})                                     │
│ → axios.post('https://localhost:5000/api/polls', body)        │
└───────────┬───────────────────────────────────────────────────┘
            │ HTTP POST
            ▼
┌───────────────────────────────────────────────────────────────┐
│ OcelotGateway :5000                                           │
├───────────────────────────────────────────────────────────────┤
│ 1. Nhận request: POST /api/polls                              │
│ 2. Tìm route khớp: "/api/polls/{everything}"                 │
│    → {everything} = "" (không có gì sau /polls)              │
│ 3. Downstream: POST https://localhost:5001/api/Polls          │
└───────────┬───────────────────────────────────────────────────┘
            │ Forward
            ▼
┌───────────────────────────────────────────────────────────────┐
│ PollService :5001                                             │
├───────────────────────────────────────────────────────────────┤
│ PollsController.CreatePoll() nhận body                        │
│ → Xử lý → INSERT vào PollDB                                   │
│ → Trả về: 201 Created + JSON poll                            │
└───────────┬───────────────────────────────────────────────────┘
            │ Response
            ▼
┌───────────────────────────────────────────────────────────────┐
│ OcelotGateway                                                 │
├───────────────────────────────────────────────────────────────┤
│ Nhận response từ PollService → trả nguyên về Frontend        │
└───────────┬───────────────────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────────────────────────────┐
│ Frontend                                                      │
├───────────────────────────────────────────────────────────────┤
│ Nhận 201 Created → hiển thị toast → chuyển sang Analytics    │
└───────────────────────────────────────────────────────────────┘
```

#### Request 2: Submit vote

```
Frontend: POST /api/votes
   → OcelotGateway: Khớp route "/api/votes/{everything}"
   → Forward: POST https://localhost:5002/api/Votes
   → VoteService: VotesController.SubmitVote()
   → Trả về: 200 OK
```

#### Request 3: Lấy kết quả

```
Frontend: GET /api/votes/result/123456
   → OcelotGateway: Khớp "/api/votes/{everything}"
      {everything} = "result/123456"
   → Forward: GET https://localhost:5002/api/Votes/result/123456
   → VoteService: VotesController.GetVoteResults("123456")
   → Trả về: 200 OK + JSON array
```

### 4.4 Docker Compose — `ocelot.Production.json`

Khi chạy trong Docker, hostname thay đổi (không còn `localhost`):

```json
{
  "GlobalConfiguration": {
    "BaseUrl": "http://gateway:8080"
  },
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/polls/{everything}",
      "DownstreamPathTemplate": "/api/Polls/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "poll-service", "Port": 8080 }
      ]
    }
  ]
}
```

**Sự khác biệt:**

| Môi trường | Hostname | Port | Scheme |
|------------|----------|------|--------|
| Development | `localhost` | 5001, 5002, 5003 | `https` |
| Docker | `poll-service`, `vote-service` | 8080 | `http` |

**Vì sao HTTP trong Docker?**

Các container nói chuyện qua internal network, không cần HTTPS (đã an toàn).  
HTTPS chỉ cần ở cổng ra ngoài (user → gateway).

### 4.5 Program.cs — Load config theo environment

```csharp
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", 
    optional: true, reloadOnChange: true);
```

**Cách hoạt động:**
1. Load `ocelot.json` (base config)
2. Nếu `ASPNETCORE_ENVIRONMENT=Production` → load `ocelot.Production.json` đè lên
3. Production config ghi đè hostname + port


---

## 5. SignalR Realtime

### 5.1 SignalR là gì?

**SignalR** là thư viện Microsoft để làm **realtime communication** (server push dữ liệu cho client mà client không cần hỏi).

**So sánh với HTTP thường:**

| HTTP thông thường | SignalR |
|-------------------|---------|
| Client hỏi → Server trả lời | Client kết nối 1 lần, Server chủ động push |
| Polling: Client hỏi mỗi 3s "Có gì mới không?" | Server tự push khi có dữ liệu mới |
| Tốn bandwidth, chậm | Tiết kiệm, realtime ngay lập tức |

**Công nghệ đằng sau:**
1. **WebSocket** (ưu tiên nhất) — kết nối 2 chiều, full-duplex
2. **Server-Sent Events** (fallback) — chỉ server → client
3. **Long Polling** (fallback cuối) — HTTP kéo dài

SignalR tự động chọn công nghệ phù hợp với trình duyệt.

### 5.2 VoteHub.cs — SignalR Hub (Backend)

**Hub** là class trung tâm xử lý kết nối và events.

```csharp
public class VoteHub : Hub
{
    // Client gọi: connection.invoke('JoinPollRoom', '123456')
    public async Task JoinPollRoom(string pollCode)
    {
        // Thêm connection này vào group "poll_123456"
        await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        
        // Gửi xác nhận về client
        await Clients.Caller.SendAsync("JoinedRoom", pollCode);
    }

    // Client gọi: connection.invoke('LeavePollRoom', '123456')
    public async Task LeavePollRoom(string pollCode)
    {
        // Xóa connection khỏi group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
    }
}
```

**Context.ConnectionId là gì?**

Mỗi khi 1 client kết nối SignalR, SignalR tự sinh 1 ID duy nhất (ví dụ: `"abc123xyz"`).  
Backend dùng ID này để biết gửi message cho client nào.

**Groups là gì?**

Nhóm nhiều connections lại. Ví dụ:
- Poll `123456` có 5 người đang xem → 5 connections trong group `"poll_123456"`
- Khi có vote mới → Server gửi 1 lần đến group → 5 người nhận cùng lúc

### 5.3 Broadcast từ VotesController

**VotesController** không phải Hub nhưng vẫn gửi được SignalR qua `IHubContext`:

```csharp
public class VotesController : ControllerBase
{
    private readonly IHubContext<VoteHub> _hubContext;

    public VotesController(IHubContext<VoteHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitVote([FromBody] Vote vote)
    {
        // ... lưu vote vào DB ...

        // Tính kết quả mới
        var results = await CalculateResults(vote.PollCode);

        // Broadcast đến tất cả client trong group
        await _hubContext.Clients
            .Group($"poll_{vote.PollCode}")
            .SendAsync("VoteUpdated", new {
                pollCode = vote.PollCode,
                totalVotes = results.Total,
                voteResults = results.ByOption
            });

        return Ok();
    }
}
```

**Luồng realtime khi có vote:**

```
1. User A submit vote
   → POST /api/votes

2. VoteService lưu vote vào DB

3. VoteService tính kết quả mới:
   - Option 1: 3 votes
   - Option 2: 5 votes
   Total: 8 votes

4. VoteService gọi SignalR:
   _hubContext.Clients.Group("poll_123456")
     .SendAsync("VoteUpdated", {...})

5. SignalR Hub phát event đến tất cả clients trong group

6. AnalyticsView (đang mở ở 3 trình duyệt) nhận event:
   hubConnection.on('VoteUpdated', data => {
     totalVotes.value = data.totalVotes
     chartData.value = data.voteResults
   })

7. UI tự động cập nhật không cần reload
```

### 5.4 usePollHub.js — Frontend Composable

**Composable** là hàm Vue Composition API để tái sử dụng logic.

```js
import * as signalR from '@microsoft/signalr'

export function usePollHub(pollCode, onVoteUpdated) {
  const connected = ref(false)
  let connection = null

  const start = async () => {
    // 1. Tạo connection
    connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:5002/hubs/vote')  // URL của VoteHub
      .withAutomaticReconnect([0, 1000, 3000, 5000]) // Retry delay
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // 2. Đăng ký event handlers
    connection.on('VoteUpdated', data => {
      if (data.pollCode === pollCode) {
        onVoteUpdated(data)  // Gọi callback từ component
      }
    })

    connection.on('PollClosed', data => {
      if (data.pollCode === pollCode) {
        onVoteUpdated(data)
      }
    })

    // 3. Kết nối
    await connection.start()
    connected.value = true

    // 4. Join group của poll này
    await connection.invoke('JoinPollRoom', pollCode)
  }

  const stop = async () => {
    if (connection) {
      await connection.invoke('LeavePollRoom', pollCode)
      await connection.stop()
    }
    connected.value = false
  }

  // Tự động disconnect khi component unmount
  onUnmounted(stop)

  return { connected, start, stop }
}
```

### 5.5 Dùng trong component

**AnalyticsView.vue:**
```vue
<script setup>
import { usePollHub } from '@/usePollHub'

const pollCode = route.query.code

const handleVoteUpdate = (data) => {
  if (data.totalVotes !== undefined) {
    // Nhận VoteUpdated event
    totalVotes.value = data.totalVotes
    chartData.value = data.voteResults
  }
  if (data.status === 'Closed') {
    // Nhận PollClosed event
    poll.value.status = 'Closed'
  }
}

const { connected, start } = usePollHub(pollCode, handleVoteUpdate)

onMounted(() => {
  start()
})
</script>

<template>
  <div class="realtime-badge" :class="{ connected }">
    {{ connected ? 'Live' : 'Connecting...' }}
  </div>
</template>
```

**VoteView.vue:**
```vue
<script setup>
const handleVoteUpdate = (data) => {
  if (data.status === 'Closed') {
    showClosedBanner.value = true
  }
}

const { start } = usePollHub(pollCode, handleVoteUpdate)
onMounted(start)
</script>
```

### 5.6 Automatic Reconnect

SignalR tự động reconnect khi mất kết nối (Wi-Fi đứt, server restart):

```js
.withAutomaticReconnect([0, 1000, 3000, 5000])
```

**Delay sequence:**
1. Lần 1: retry ngay (0ms)
2. Lần 2: retry sau 1 giây
3. Lần 3: retry sau 3 giây
4. Lần 4: retry sau 5 giây
5. Lần 5+: retry mỗi 5 giây

**Sau khi reconnect thành công:**
```js
connection.onreconnected(() => {
  connected.value = true
  connection.invoke('JoinPollRoom', pollCode)  // Join lại group
})
```

### 5.7 Tại sao không routing SignalR qua Ocelot?

Ocelot có thể routing WebSocket nhưng:
1. Cần cấu hình phức tạp (`UpgradeHttpVersion`, `AllowAutoRedirect`)
2. Long-polling fallback dễ bị lỗi qua proxy
3. Latency cao hơn (thêm 1 hop)

→ Frontend kết nối **trực tiếp** đến VoteService `:5002/hubs/vote`.


---

## 6. Frontend

### 6.1 Cấu trúc thư mục

```
client/
├── public/               ← Favicon, index.html (template)
├── src/
│   ├── main.js          ← Entry point: khởi tạo app
│   ├── App.vue          ← Root component
│   ├── api.js           ← Axios + tất cả hàm gọi API
│   ├── voterToken.js    ← Tạo/lấy voter token
│   ├── usePollHub.js    ← SignalR composable
│   ├── router/
│   │   └── index.js     ← Route config
│   ├── views/           ← 4 trang chính
│   │   ├── HomeView.vue
│   │   ├── CreatePollView.vue
│   │   ├── VoteView.vue
│   │   └── AnalyticsView.vue
│   └── assets/
│       └── main.css     ← Design system + TailwindCSS
├── package.json         ← Dependencies
├── tailwind.config.js   ← TailwindCSS config
└── vue.config.js        ← Dev server config
```

### 6.2 Thư viện Frontend chi tiết

#### Vue 3 — Framework UI

**Cài đặt:**
```bash
npm install vue@^3.2.13
```

**Tác dụng:**
- Framework reactive: thay đổi data → UI tự động cập nhật
- Component-based: chia UI thành các component nhỏ tái sử dụng
- Composition API: `<script setup>`, `ref()`, `computed()`, `onMounted()`

**Ví dụ reactive:**
```vue
<script setup>
import { ref } from 'vue'

const count = ref(0)  // Tạo biến reactive

function increment() {
  count.value++  // Thay đổi giá trị
}
</script>

<template>
  <!-- UI tự động cập nhật khi count thay đổi -->
  <button @click="increment">{{ count }}</button>
</template>
```

---

#### Vue Router — Điều hướng

**Cài đặt:**
```bash
npm install vue-router@^4.6.4
```

**Tác dụng:** SPA routing — chuyển trang không reload.

**File `router/index.js`:**
```js
const routes = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../views/HomeView.vue'),
    meta: { title: 'PollBuilder' }
  },
  {
    path: '/vote/:code?',  // :code? = optional param
    name: 'Vote',
    component: () => import('../views/VoteView.vue'),
    meta: { title: 'Vote' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 }  // Scroll về đầu trang khi chuyển route
  }
})

// Cập nhật title tab trình duyệt
router.beforeEach(to => {
  document.title = to.meta.title || 'Poll Survey'
})
```

**Dùng trong component:**
```vue
<script setup>
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const route = useRoute()

// Chuyển trang
router.push('/vote/123456')
router.push({ name: 'Analytics', query: { code: '123456' } })

// Đọc URL
const code = route.params.code     // Lấy từ /vote/:code
const code2 = route.query.code     // Lấy từ /analytics?code=123
</script>
```

---

#### Axios — HTTP Client

**Cài đặt:**
```bash
npm install axios@^1.19.0
```

**Tác dụng:** Gửi HTTP request, tiện hơn `fetch` native.

**File `api.js`:**
```js
import axios from 'axios'

const apiClient = axios.create({
  baseURL: 'https://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000
})

// Interceptor: bắt lỗi tập trung
apiClient.interceptors.response.use(
  res => res,
  err => {
    const msg = err.response?.data?.message || err.message
    return Promise.reject(new Error(msg))
  }
)

export const pollApi = {
  createPoll: data => apiClient.post('/api/polls', data),
  checkPoll: code => apiClient.get(`/api/polls/check/${code}`),
  submitVote: data => apiClient.post('/api/votes', data)
}
```

**Dùng trong component:**
```vue
<script setup>
import { pollApi } from '@/api'

const submit = async () => {
  try {
    const res = await pollApi.createPoll({ question: '...', ... })
    console.log(res.data)  // Object poll từ server
  } catch (error) {
    console.error(error.message)  // Message từ interceptor
  }
}
</script>
```

---

#### @microsoft/signalr — Realtime Client

**Cài đặt:**
```bash
npm install @microsoft/signalr@^10.0.0
```

**Tác dụng:** Kết nối SignalR Hub, nhận events realtime.

**File `usePollHub.js`:**
```js
import * as signalR from '@microsoft/signalr'

export function usePollHub(pollCode, onVoteUpdated) {
  let connection = null

  const start = async () => {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:5002/hubs/vote')
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .build()

    connection.on('VoteUpdated', onVoteUpdated)
    
    await connection.start()
    await connection.invoke('JoinPollRoom', pollCode)
  }

  return { start }
}
```

---

#### vue-toastification — Thông báo

**Cài đặt:**
```bash
npm install vue-toastification@^2.0.0-rc.5
```

**Import CSS bắt buộc trong `main.js`:**
```js
import 'vue-toastification/dist/index.css'
```

**Cấu hình trong `main.js`:**
```js
import Toast from 'vue-toastification'

app.use(Toast, {
  position: 'bottom-right',
  timeout: 2500,
  closeOnClick: true,
  pauseOnHover: true
})
```

**Dùng trong component:**
```vue
<script setup>
import { useToast } from 'vue-toastification'

const toast = useToast()

function success() {
  toast.success('Poll created!')
}

function error() {
  toast.error('Failed to create poll.')
}
</script>
```

---

#### qrcode — Tạo mã QR

**Cài đặt:**
```bash
npm install qrcode@^1.5.4
```

**Tác dụng:** Vẽ QR code lên `<canvas>`.

**Dùng trong component:**
```vue
<script setup>
import QRCode from 'qrcode'
import { ref, onMounted } from 'vue'

const canvasRef = ref(null)

onMounted(async () => {
  const url = 'https://example.com/vote/123456'
  await QRCode.toCanvas(canvasRef.value, url, {
    width: 320,
    margin: 2,
    color: {
      dark: '#000000',
      light: '#ffffff'
    }
  })
})
</script>

<template>
  <canvas ref="canvasRef"></canvas>
</template>
```

---

#### @lucide/vue — Icon SVG

**Cài đặt:**
```bash
npm install @lucide/vue@^1.28.0
```

**Tác dụng:** Icon SVG dạng Vue component, tree-shakeable.

**Dùng trong component:**
```vue
<script setup>
import { Check, Trash2, Star } from '@lucide/vue'
</script>

<template>
  <button>
    <Check :size="16" />
    Success
  </button>
  
  <button>
    <Trash2 :size="16" color="#ff0000" />
    Delete
  </button>
</template>
```

---

#### TailwindCSS — Utility CSS

**Cài đặt:**
```bash
npm install -D tailwindcss@^3.4.19 autoprefixer@^10.5.4 postcss@^8.5.25
npx tailwindcss init
```

**File `tailwind.config.js`:**
```js
module.exports = {
  content: ['./src/**/*.{vue,js,ts}'],
  theme: {
    extend: {}
  }
}
```

**File `main.css`:**
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

:root {
  --blue: #2563eb;
  --text: #1e293b;
  --border: #e2e8f0;
}
```

**Dùng trong template:**
```vue
<template>
  <div class="flex items-center gap-2 p-4 bg-white border rounded-lg">
    <button class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-700">
      Submit
    </button>
  </div>
</template>
```


### 6.3 Giải thích từng View

#### HomeView.vue — Trang chủ

**Chức năng:**
1. Join poll bằng code 6 số
2. Hoặc chuyển sang CreatePollView để tạo mới

**Logic quan trọng:**

```vue
<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { pollApi } from '../api'

const router = useRouter()
const code = ref('')
const codeError = ref('')
const joinLoading = ref(false)

const joinPoll = async () => {
  // Validate code đủ 6 chữ số
  if (code.value.length < 6) {
    codeError.value = 'Please enter all 6 digits'
    return
  }

  joinLoading.value = true
  try {
    // Gọi API validate poll
    await pollApi.checkPoll(code.value)
    
    // Thành công → chuyển sang VoteView
    router.push(`/vote/${code.value}`)
  } catch {
    // Thất bại → hiện lỗi
    codeError.value = 'Poll not found'
  } finally {
    joinLoading.value = false
  }
}
</script>

<template>
  <form @submit.prevent="joinPoll">
    <input 
      v-model="code" 
      type="text" 
      inputmode="numeric"
      maxlength="6" 
      placeholder="000000"
    />
    <p v-if="codeError">{{ codeError }}</p>
    <button type="submit" :disabled="joinLoading">
      {{ joinLoading ? 'Joining...' : 'Join Room' }}
    </button>
  </form>
</template>
```

**Test case:**

| Hành động | Kết quả |
|-----------|---------|
| Nhập `123` → Submit | "Please enter all 6 digits" |
| Nhập `999999` → Submit | API call → 404 → "Poll not found" |
| Nhập `123456` (tồn tại) → Submit | API call → 200 OK → Chuyển sang `/vote/123456` |

---

#### CreatePollView.vue — Tạo poll

**Chức năng:** Form tạo poll với 4 loại câu hỏi.

**Logic quan trọng:**

```vue
<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import { pollApi } from '../api'

const router = useRouter()
const toast = useToast()

const form = ref({
  question: '',
  questionType: 'Multiple Choice',
  expireAt: getDefaultExpireDate(),  // 5 phút sau
  options: [{ text: '' }, { text: '' }]
})

const expireMode = ref('none')  // 'none' hoặc 'custom'

// Tạo code ngẫu nhiên 6 chữ số
const generateCode = () => {
  return Math.floor(100000 + Math.random() * 900000).toString()
}

// Chuyển datetime-local sang UTC ISO
const localDateTimeToUtcIso = (localStr) => {
  const date = new Date(localStr)
  return date.toISOString()
}

const submit = async () => {
  // Validate
  if (!form.value.question.trim()) {
    toast.error('Please enter a question.')
    return
  }

  const payload = {
    code: generateCode(),
    question: form.value.question.trim(),
    questionType: form.value.questionType,
    expireAt: expireMode.value === 'custom'
      ? localDateTimeToUtcIso(form.value.expireAt)
      : new Date(Date.now() + 100 * 365 * 24 * 60 * 60 * 1000).toISOString(), // 100 năm
    options: form.value.questionType === 'Multiple Choice'
      ? form.value.options.filter(o => o.text.trim()).map(o => ({ text: o.text.trim() }))
      : []
  }

  try {
    const res = await pollApi.createPoll(payload)
    const poll = res.data
    
    // Lưu code vào localStorage (để xác nhận quyền creator)
    const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]')
    saved.push(poll.code)
    localStorage.setItem('createdPolls', JSON.stringify(saved))
    
    toast.success('Poll created!')
    router.push({ name: 'Analytics', query: { code: poll.code } })
  } catch (error) {
    toast.error(error.message)
  }
}
</script>
```

**Test case tạo Multiple Choice:**

| Input | Backend nhận | Backend lưu |
|-------|--------------|-------------|
| Question: "Best framework?" | `question: "Best framework?"` | Lưu vào `Polls.Question` |
| Type: "Multiple Choice" | `questionType: "Multiple Choice"` | Lưu vào `Polls.QuestionType` |
| Options: ["Vue", "React"] | `options: [{text:"Vue"}, {text:"React"}]` | INSERT 2 rows vào `Options` |
| ExpireAt: Custom "2026-08-10 12:00" | `expireAt: "2026-08-10T05:00:00Z"` (UTC+7 → UTC) | Lưu vào `Polls.ExpireAt` |

**Test case tạo Yes/No:**

| Input | Backend xử lý |
|-------|---------------|
| Type: "Yes / No" | Backend **bỏ qua** options frontend gửi |
| | Backend tự tạo: `options: [{text:"Yes"}, {text:"No"}]` |

**Test case tạo Rating:**

| Input | Backend xử lý |
|-------|---------------|
| Type: "Rating" | Backend **không tạo** Options nào |
| | Bảng `Options` không có row nào cho poll này |

---

#### VoteView.vue — Trang bỏ phiếu

**5 trạng thái UI:**

1. **Loading** — Đang gọi API lấy thông tin poll
2. **Poll Closed** — Poll đã đóng (banner đỏ)
3. **Already Voted** — User đã vote rồi (hiện kết quả)
4. **Vote Form** — Form bỏ phiếu
5. **Success** — Đã vote thành công (hiện kết quả + confetti)

**Logic quan trọng:**

```vue
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { pollApi } from '@/api'
import { getVoterToken } from '@/voterToken'
import { usePollHub } from '@/usePollHub'

const route = useRoute()
const pollCode = ref(route.params.code || '')

const poll = ref(null)
const loading = ref(true)
const alreadyVoted = ref(false)
const showClosedBanner = ref(false)

// Kiểm tra đã vote chưa từ localStorage
const checkAlreadyVoted = () => {
  const voted = localStorage.getItem(`voted_${pollCode.value}`)
  return voted === 'true'
}

onMounted(async () => {
  try {
    // Lấy thông tin poll
    const res = await pollApi.checkPoll(pollCode.value)
    poll.value = res.data
    
    // Kiểm tra đã vote chưa
    alreadyVoted.value = checkAlreadyVoted()
    
    // Nếu poll đã Closed → hiện banner
    if (poll.value.status === 'Closed') {
      showClosedBanner.value = true
    }
    
    // Kết nối SignalR để nhận event PollClosed realtime
    const { start } = usePollHub(pollCode.value, (data) => {
      if (data.status === 'Closed') {
        showClosedBanner.value = true
      }
    })
    start()
    
  } catch (error) {
    // Poll không tồn tại → hiện lỗi
  } finally {
    loading.value = false
  }
})

const submitVote = async (optionId, voteValue = '') => {
  const payload = {
    pollCode: pollCode.value,
    voterToken: getVoterToken(),
    optionId,
    voteValue
  }
  
  try {
    await pollApi.submitVote(payload)
    
    // Lưu flag đã vote vào localStorage
    localStorage.setItem(`voted_${pollCode.value}`, 'true')
    
    alreadyVoted.value = true
    // Hiện confetti...
  } catch (error) {
    toast.error(error.message)
  }
}
</script>

<template>
  <div v-if="loading">Loading...</div>
  
  <div v-else-if="showClosedBanner" class="banner-closed">
    This poll has ended
  </div>
  
  <div v-else-if="alreadyVoted">
    <p>You already voted. Here are the results:</p>
    <!-- Hiển thị kết quả -->
  </div>
  
  <div v-else>
    <!-- Form vote theo questionType -->
    <div v-if="poll.questionType === 'Multiple Choice'">
      <button 
        v-for="opt in poll.options" 
        :key="opt.id"
        @click="submitVote(opt.id)"
      >
        {{ opt.text }}
      </button>
    </div>
    
    <div v-else-if="poll.questionType === 'Rating'">
      <button 
        v-for="star in [1,2,3,4,5]" 
        :key="star"
        @click="submitVote(0, star.toString())"
      >
        ⭐ {{ star }}
      </button>
    </div>
    
    <div v-else-if="poll.questionType === 'Open Text'">
      <textarea v-model="openText"></textarea>
      <button @click="submitVote(0, openText)">Submit</button>
    </div>
  </div>
</template>
```

**Test case:**

| Tình huống | Kết quả |
|------------|---------|
| Poll code không tồn tại | API 404 → hiện "Poll not found" |
| Poll status="Closed" | Hiện banner đỏ, disable form |
| `localStorage['voted_123456']` = "true" | Hiện "You already voted" + kết quả |
| Vote Multiple Choice option 2 | POST {optionId:2, voteValue:""} → 200 OK → set localStorage |
| Vote Rating 4 sao | POST {optionId:0, voteValue:"4"} |
| Vote Open Text "Vue tốt" | POST {optionId:0, voteValue:"Vue tốt"} |
| Đang vote, creator đóng poll | SignalR event "PollClosed" → hiện banner |


#### AnalyticsView.vue — Dashboard kết quả realtime

**Chức năng:**
1. Hiển thị kết quả realtime (cập nhật khi có vote mới)
2. Đóng poll
3. Xóa poll
4. Share link/QR code

**Kiểm tra quyền creator:**

```vue
<script setup>
const isCreator = computed(() => {
  const saved = localStorage.getItem('createdPolls')
  const codes = JSON.parse(saved || '[]')
  return codes.includes(pollCode.value)
})
</script>

<template>
  <!-- Chỉ creator mới thấy nút Close/Delete -->
  <div v-if="isCreator">
    <button @click="closePoll">Close Poll</button>
    <button @click="deletePoll">Delete Poll</button>
  </div>
</template>
```

**Logic realtime:**

```vue
<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { pollApi } from '@/api'
import { usePollHub } from '@/usePollHub'

const route = useRoute()
const pollCode = ref(route.query.code)

const poll = ref(null)
const voteResults = ref([])
const totalVotes = ref(0)

// Kết nối SignalR
const handleVoteUpdate = (data) => {
  if (data.totalVotes !== undefined) {
    // Event "VoteUpdated"
    totalVotes.value = data.totalVotes
    voteResults.value = data.voteResults
  }
  if (data.status === 'Closed') {
    // Event "PollClosed"
    poll.value.status = 'Closed'
  }
}

const { connected, start } = usePollHub(pollCode.value, handleVoteUpdate)

onMounted(async () => {
  // 1. Lấy thông tin poll
  const pollRes = await pollApi.getPollByCode(pollCode.value)
  poll.value = pollRes.data
  
  // 2. Lấy kết quả ban đầu (tùy theo questionType)
  if (poll.value.questionType === 'Multiple Choice' || poll.value.questionType === 'Yes / No') {
    const res = await pollApi.getVoteResults(pollCode.value)
    voteResults.value = res.data
  } else if (poll.value.questionType === 'Rating') {
    const res = await pollApi.getVoteList(pollCode.value)
    const votes = res.data
    // Tính trung bình
    const sum = votes.reduce((acc, v) => acc + parseInt(v.voteValue), 0)
    averageRating.value = votes.length ? sum / votes.length : 0
  } else if (poll.value.questionType === 'Open Text') {
    const res = await pollApi.getVoteList(pollCode.value)
    textResponses.value = res.data
  }
  
  // 3. Lấy tổng votes
  const totalRes = await pollApi.getVoteTotal(pollCode.value)
  totalVotes.value = totalRes.data.totalVotes
  
  // 4. Kết nối SignalR để nhận updates
  start()
})

// Chart data cho Multiple Choice
const chartData = computed(() => {
  return voteResults.value.map(r => {
    const option = poll.value.options.find(o => o.id === r.optionId)
    return {
      name: option?.text || 'Unknown',
      count: r.voteCount,
      percentage: totalVotes.value ? (r.voteCount / totalVotes.value * 100).toFixed(1) : 0
    }
  })
})

const closePoll = async () => {
  if (!confirm('Close this poll?')) return
  
  try {
    await pollApi.updatePoll(pollCode.value, {
      ...poll.value,
      status: 'Closed'
    })
    poll.value.status = 'Closed'
    toast.success('Poll closed')
  } catch (error) {
    toast.error(error.message)
  }
}

const deletePoll = async () => {
  if (!confirm('Delete this poll permanently?')) return
  
  try {
    await pollApi.deletePoll(pollCode.value)
    
    // Xóa code khỏi localStorage
    const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]')
    const updated = saved.filter(c => c !== pollCode.value)
    localStorage.setItem('createdPolls', JSON.stringify(updated))
    
    toast.success('Poll deleted')
    router.push('/')
  } catch (error) {
    toast.error(error.message)
  }
}
</script>

<template>
  <div class="stats">
    <div class="stat-card">
      <div class="stat-value">{{ totalVotes }}</div>
      <div class="stat-label">Total Votes</div>
    </div>
    <div class="stat-card">
      <div class="badge" :class="poll.status === 'Active' ? 'badge-green' : 'badge-red'">
        {{ poll.status }}
      </div>
    </div>
    <div class="stat-card">
      <div class="realtime-badge" :class="{ connected }">
        {{ connected ? '🟢 Live' : '🔴 Connecting...' }}
      </div>
    </div>
  </div>

  <!-- Multiple Choice: Bar chart -->
  <div v-if="poll.questionType === 'Multiple Choice'" class="chart">
    <div v-for="item in chartData" :key="item.name" class="bar">
      <div class="bar-label">{{ item.name }}</div>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: item.percentage + '%' }"></div>
      </div>
      <div class="bar-stat">{{ item.count }} ({{ item.percentage }}%)</div>
    </div>
  </div>

  <!-- Rating: Average stars -->
  <div v-else-if="poll.questionType === 'Rating'">
    <div class="rating-display">
      <div class="rating-value">{{ averageRating.toFixed(1) }}</div>
      <div class="stars">⭐⭐⭐⭐⭐</div>
    </div>
  </div>

  <!-- Open Text: List responses -->
  <div v-else-if="poll.questionType === 'Open Text'">
    <div v-for="(response, i) in textResponses" :key="i" class="response-card">
      <p>{{ response.voteValue }}</p>
      <small>{{ formatDate(response.createdAt) }}</small>
    </div>
  </div>
</template>
```

**Test case realtime:**

| Hành động | Kết quả |
|-----------|---------|
| Mở trang Analytics poll 123456 | SignalR kết nối → join group "poll_123456" |
| User A vote option 2 (ở tab khác) | VoteService broadcast → AnalyticsView nhận event → chart tự động update |
| Creator bấm "Close Poll" | PUT /api/polls → backend broadcast "PollClosed" → badge đổi sang "Closed" |
| User B cố vote sau khi đóng | VoteView nhận event "PollClosed" → hiện banner đỏ |

**Test case xóa poll:**

| Hành động | DB/Storage thay đổi |
|-----------|---------------------|
| Bấm Delete Poll code 123456 | 1. PollDB: xóa Polls (Id=1) + Options (cascade) |
| | 2. VoteDB: xóa tất cả Votes có PollCode="123456" |
| | 3. AnalyticsDB: KHÔNG xóa (audit log giữ lại) |
| | 4. `localStorage['createdPolls']`: xóa "123456" khỏi array |
| | 5. Router push('/') → về trang chủ |

---

### 6.4 voterToken.js — Chống vote 2 lần

**Vấn đề:** User không cần đăng nhập, làm sao biết user A đã vote chưa?

**Giải pháp:** Lưu token duy nhất vào localStorage trình duyệt.

```js
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token')
  
  if (token === null) {
    // Tạo chuỗi ngẫu nhiên 8 chữ số
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      randomPart += Math.floor(Math.random() * 10)
    }
    token = 'voter_' + randomPart  // "voter_47291038"
    localStorage.setItem('poll_voter_token', token)
  }
  
  return token
}
```

**localStorage lưu:**
```
poll_voter_token = "voter_47291038"
voted_123456 = "true"
voted_789012 = "true"
```

**Luồng chống vote 2 lần:**

```
1. User A mở VoteView lần đầu
   → getVoterToken() tạo "voter_47291038"
   → Lưu vào localStorage

2. User A vote poll 123456
   → Frontend gửi: POST /api/votes {voterToken: "voter_47291038", ...}
   → Backend: INSERT vào Votes
   → Frontend: localStorage.setItem('voted_123456', 'true')

3. User A reload trang
   → VoteView: checkAlreadyVoted() → localStorage['voted_123456'] === 'true'
   → Hiện "You already voted" + kết quả

4. User A mở tab mới, vào VoteView lại
   → Cùng localStorage → cùng token
   → Backend: SELECT * FROM Votes WHERE VoterToken='voter_47291038' AND PollCode='123456'
   → Tìm thấy → trả 400 "You have already voted."

5. User A xóa cache trình duyệt
   → localStorage mất → tạo token mới
   → Có thể vote lại (hạn chế chấp nhận được)

6. User A đổi trình duyệt
   → localStorage khác → token khác
   → Có thể vote lại
```

**Tại sao không dùng session cookie?**

Session cookie cần backend set qua HTTP header:
```
Set-Cookie: voter_session=abc123; HttpOnly; SameSite=Strict
```
Phức tạp hơn, cần cấu hình CORS đúng. localStorage đơn giản hơn cho bài tập.

**Tại sao không dùng browser fingerprint?**

Canvas fingerprint, WebGL, fonts... không ổn định, dễ trùng.


---

## 7. Luồng nghiệp vụ chi tiết

### 7.1 Tạo Poll (4 loại)

#### Loại 1: Multiple Choice

**Frontend:**
```
1. User điền form:
   - Question: "Best JavaScript framework?"
   - Type: "Multiple Choice"
   - Options: ["Vue.js", "React", "Angular"]
   - ExpireAt: "2026-08-10 12:00" (local time)

2. Click "Create Poll"

3. JavaScript xử lý:
   - Sinh code: "123456" (random 6 số)
   - Chuyển expireAt sang UTC: "2026-08-10T05:00:00Z"
   - POST /api/polls {
       code: "123456",
       question: "Best JavaScript framework?",
       questionType: "Multiple Choice",
       expireAt: "2026-08-10T05:00:00Z",
       options: [
         { text: "Vue.js" },
         { text: "React" },
         { text: "Angular" }
       ]
     }
```

**Backend PollService:**
```
1. Validate question không rỗng ✓
2. Validate expireAt > now ✓
3. Validate code chưa tồn tại ✓
4. Validate options.length >= 2 ✓

5. Set createdAt = DateTime.UtcNow
6. Set status = "Active"

7. INSERT vào PollDB:
   Polls table:
     Id=1, Code="123456", Question="Best...", 
     QuestionType="Multiple Choice", Status="Active",
     ExpireAt="2026-08-10T05:00:00Z", 
     CreatedAt="2026-08-02T06:30:00Z"
   
   Options table:
     Id=1, PollId=1, Text="Vue.js"
     Id=2, PollId=1, Text="React"
     Id=3, PollId=1, Text="Angular"

8. Trả về 201 Created + poll object
```

**Frontend nhận response:**
```
1. Lưu code vào localStorage['createdPolls']
2. toast.success('Poll created!')
3. router.push('/analytics?code=123456')
```

---

#### Loại 2: Yes / No

**Frontend:**
```
POST /api/polls {
  code: "789012",
  question: "Do you like Vue.js?",
  questionType: "Yes / No",
  expireAt: "2026-08-10T05:00:00Z",
  options: []  ← Frontend KHÔNG gửi options
}
```

**Backend:**
```
1. Validate như trên ✓

2. Phát hiện questionType = "Yes / No"
   → BỎ QUA options từ request
   → TỰ TẠO: options = [{ text: "Yes" }, { text: "No" }]

3. INSERT vào DB:
   Polls:
     Id=2, Code="789012", Question="Do you like Vue.js?",
     QuestionType="Yes / No", ...
   
   Options:
     Id=4, PollId=2, Text="Yes"
     Id=5, PollId=2, Text="No"
```

---

#### Loại 3: Rating

**Frontend:**
```
POST /api/polls {
  code: "345678",
  question: "Rate our service",
  questionType: "Rating",
  expireAt: "2026-08-10T05:00:00Z",
  options: []
}
```

**Backend:**
```
1. Validate ✓

2. Phát hiện questionType = "Rating"
   → options = [] (KHÔNG tạo Options nào)

3. INSERT vào DB:
   Polls:
     Id=3, Code="345678", Question="Rate our service",
     QuestionType="Rating", ...
   
   Options:
     (Không có row nào)
```

---

#### Loại 4: Open Text

**Frontend:**
```
POST /api/polls {
  code: "901234",
  question: "What do you think?",
  questionType: "Open Text",
  expireAt: "2026-08-10T05:00:00Z",
  options: []
}
```

**Backend:**
```
1. Validate ✓

2. Phát hiện questionType = "Open Text"
   → options = [] (KHÔNG tạo Options)

3. INSERT vào DB:
   Polls:
     Id=4, Code="901234", Question="What do you think?",
     QuestionType="Open Text", ...
   
   Options:
     (Không có row nào)
```

---

### 7.2 User Vote

#### Vote Multiple Choice

**Frontend VoteView:**
```
1. Load poll: GET /api/polls/check/123456
   → Nhận: { id:1, code:"123456", questionType:"Multiple Choice", 
            options:[{id:1,text:"Vue.js"}, ...] }

2. User click nút "Vue.js" (optionId = 1)

3. Submit:
   POST /api/votes {
     pollCode: "123456",
     voterToken: "voter_47291038",
     optionId: 1,
     voteValue: ""
   }
```

**Backend VoteService:**
```
1. Chống vote 2 lần:
   SELECT * FROM Votes 
   WHERE PollCode='123456' AND VoterToken='voter_47291038'
   → Không tìm thấy ✓

2. Validate poll:
   HTTP GET https://localhost:5001/api/Polls/check/123456
   → PollService trả 200 OK ✓

3. Lưu vote:
   INSERT INTO Votes VALUES (
     PollCode='123456',
     OptionId=1,
     VoteValue='',
     VoterToken='voter_47291038',
     CreatedAt='2026-08-02T07:05:00'
   )

4. Tính kết quả:
   SELECT OptionId, COUNT(*) FROM Votes WHERE PollCode='123456' GROUP BY OptionId
   → [{optionId:1, voteCount:1}]

5. Broadcast SignalR:
   hubContext.Clients.Group("poll_123456")
     .SendAsync("VoteUpdated", {
       pollCode: "123456",
       totalVotes: 1,
       voteResults: [{optionId:1, voteCount:1}]
     })

6. Fire-and-forget Analytics:
   POST https://localhost:5003/api/Analytics {
     pollCode: "123456",
     optionId: 1,
     voteTime: "2026-08-02T07:05:00Z"
   }

7. Trả về: 200 OK { message: "Vote submitted successfully!" }
```

**Frontend nhận response:**
```
1. localStorage.setItem('voted_123456', 'true')
2. alreadyVoted.value = true
3. Hiển thị confetti + kết quả
```

**AnalyticsView (đang mở) nhận SignalR event:**
```
hubConnection.on('VoteUpdated', data => {
  totalVotes.value = data.totalVotes  // 1
  voteResults.value = data.voteResults  // [{optionId:1, voteCount:1}]
  // Chart tự động update vì Vue reactive
})
```

---

#### Vote Rating (4 sao)

**Frontend:**
```
POST /api/votes {
  pollCode: "345678",
  voterToken: "voter_47291038",
  optionId: 0,  ← Không có option, để 0
  voteValue: "4"  ← Số sao dạng string
}
```

**Backend lưu:**
```
INSERT INTO Votes VALUES (
  PollCode='345678',
  OptionId=0,
  VoteValue='4',
  VoterToken='voter_47291038',
  CreatedAt='2026-08-02T07:10:00'
)
```

**AnalyticsView tính trung bình:**
```
GET /api/votes/list/345678
→ [
    { optionId:0, voteValue:"5" },
    { optionId:0, voteValue:"4" },
    { optionId:0, voteValue:"3" }
  ]

avg = (5 + 4 + 3) / 3 = 4.0
```

---

#### Vote Open Text

**Frontend:**
```
POST /api/votes {
  pollCode: "901234",
  voterToken: "voter_47291038",
  optionId: 0,
  voteValue: "Vue is amazing!"
}
```

**Backend lưu:**
```
INSERT INTO Votes VALUES (
  PollCode='901234',
  OptionId=0,
  VoteValue='Vue is amazing!',
  VoterToken='voter_47291038',
  CreatedAt='2026-08-02T07:15:00'
)
```

**AnalyticsView hiển thị:**
```
GET /api/votes/list/901234
→ [
    { voteValue: "Vue is amazing!", createdAt: "..." },
    { voteValue: "I prefer React", createdAt: "..." }
  ]

Render:
  📝 Vue is amazing!
  📝 I prefer React
```

---

### 7.3 Đóng Poll

**Frontend (AnalyticsView):**
```
1. Creator click "Close Poll"
2. Confirm dialog → OK

3. PUT /api/polls/code/123456 {
     id: 1,
     code: "123456",
     question: "...",
     questionType: "Multiple Choice",
     status: "Closed",  ← Thay đổi từ "Active"
     expireAt: "...",
     options: []
   }
```

**Backend PollService:**
```
1. Tìm poll: SELECT * FROM Polls WHERE Code='123456'
   → poll.Status hiện tại = "Active"

2. Phát hiện status thay đổi:
   bool statusChanged = (existingPoll.Status != newPoll.Status)
   → statusChanged = true

3. Cập nhật DB:
   UPDATE Polls SET Status='Closed' WHERE Id=1

4. Vì statusChanged && newStatus='Closed':
   → Gọi VoteService:
     POST https://localhost:5002/api/votes/broadcast-poll-closed {
       pollCode: "123456"
     }

5. VoteService phát SignalR:
   hubContext.Clients.Group("poll_123456")
     .SendAsync("PollClosed", {
       pollCode: "123456",
       status: "Closed"
     })

6. Trả về: 204 No Content
```

**Clients nhận SignalR event:**

**VoteView (đang mở):**
```
hubConnection.on('PollClosed', data => {
  showClosedBanner.value = true
  // Hiện banner đỏ: "This poll has ended"
  // Disable tất cả nút vote
})
```

**AnalyticsView (đang mở):**
```
hubConnection.on('PollClosed', data => {
  poll.value.status = 'Closed'
  // Badge đổi từ "Active" (xanh) → "Closed" (đỏ)
})
```

**User cố vote sau khi đóng:**
```
POST /api/votes {...}
→ VoteService: GET /api/polls/check/123456
→ PollService: status='Closed' → trả 400 "Poll is closed."
→ VoteService: nhận 400 → trả 400 "Poll is invalid or has been closed."
→ Frontend: toast.error("Poll is invalid or has been closed.")
```

---

### 7.4 Xóa Poll

**Frontend (AnalyticsView):**
```
1. Creator click "Delete Poll"
2. Confirm "Delete permanently?" → OK

3. DELETE /api/polls/code/123456
```

**Backend PollService:**
```
1. Tìm poll kèm options:
   SELECT * FROM Polls WHERE Code='123456'
   INCLUDE SELECT * FROM Options WHERE PollId=1

2. Xóa poll:
   DELETE FROM Polls WHERE Id=1
   → CASCADE tự động xóa:
      DELETE FROM Options WHERE PollId=1

3. Gọi VoteService xóa votes:
   DELETE https://localhost:5002/api/votes/by-poll-code/123456
   
   VoteService:
     SELECT * FROM Votes WHERE PollCode='123456'
     → Tìm thấy 10 votes
     DELETE FROM Votes WHERE PollCode='123456'
     → Xóa 10 rows

4. AnalyticsDB KHÔNG động đến (audit log giữ lại)

5. Trả về: 204 No Content
```

**Frontend nhận 204:**
```
1. Xóa code khỏi localStorage:
   const saved = JSON.parse(localStorage['createdPolls'])  // ["123456", "789012"]
   const updated = saved.filter(c => c !== "123456")       // ["789012"]
   localStorage['createdPolls'] = JSON.stringify(updated)

2. toast.success('Poll deleted')

3. router.push('/') → Về trang chủ
```

**Tổng kết những gì bị xóa:**

| Database | Bảng | Hành động | Số rows |
|----------|------|-----------|---------|
| PollDB | `Polls` | DELETE | 1 row |
| PollDB | `Options` | CASCADE DELETE | 3 rows (Vue, React, Angular) |
| VoteDB | `Votes` | DELETE qua HTTP | 10 rows |
| AnalyticsDB | `Analytics` | **KHÔNG xóa** | 10 rows giữ nguyên |
| localStorage | `createdPolls` | Array.filter() | Xóa "123456" |
| localStorage | `voted_123456` | **KHÔNG xóa** | Giữ nguyên (harmless) |


---

## 8. Migration Files

### 8.1 Migration là gì?

**Entity Framework Core Migration** = file code C# mô tả thay đổi schema database.

Thay vì viết SQL thủ công:
```sql
CREATE TABLE Polls (
  Id INT PRIMARY KEY IDENTITY(1,1),
  Code NVARCHAR(MAX) NOT NULL,
  ...
)
```

EF Core tự sinh code C# từ model:
```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(nullable: false),
                ...
            });
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Polls");
    }
}
```

### 8.2 Tại sao có Migration?

**Vấn đề không dùng Migration:**
1. Developer A tạo bảng `Polls` bằng SQL script
2. Developer B không biết phải chạy script nào
3. Production database khác Development
4. Không theo dõi được lịch sử thay đổi schema

**Giải pháp dùng Migration:**
1. Developer A thay đổi `Poll.cs` model
2. Chạy `dotnet ef migrations add AddExpireAt`
3. EF Core tự sinh file migration
4. Developer B pull code → chạy `dotnet ef database update`
5. Database tự động cập nhật

### 8.3 File Migration trong project

**PollService/Migrations/20260724153449_InitialCreate.cs:**

```csharp
public partial class InitialCreate : Migration
{
    // Up() chạy khi apply migration
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Tạo bảng Polls
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(nullable: false),
                Question = table.Column<string>(nullable: false),
                QuestionType = table.Column<string>(nullable: false),
                Status = table.Column<string>(nullable: false),
                ExpireAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Polls", x => x.Id);
            });

        // Tạo bảng Options
        migrationBuilder.CreateTable(
            name: "Options",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PollId = table.Column<int>(nullable: false),
                Text = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Options", x => x.Id);
                table.ForeignKey(
                    name: "FK_Options_Polls_PollId",
                    column: x => x.PollId,
                    principalTable: "Polls",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);  // CASCADE DELETE
            });

        // Tạo index
        migrationBuilder.CreateIndex(
            name: "IX_Options_PollId",
            table: "Options",
            column: "PollId");
    }

    // Down() chạy khi rollback migration
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Options");
        migrationBuilder.DropTable(name: "Polls");
    }
}
```

**Tên file:** `20260724153449_InitialCreate.cs`
- `20260724153449` = timestamp (July 24, 2026, 15:34:49)
- `InitialCreate` = tên migration (developer đặt)

### 8.4 File khác trong Migrations/

**AnalyticsDbContextModelSnapshot.cs:**

File snapshot mô tả schema hiện tại (không phải migration, dùng để detect changes).

```csharp
protected override void BuildModel(ModelBuilder modelBuilder)
{
    modelBuilder.Entity("AnalyticsService.Models.Analytics", b =>
    {
        b.Property<int>("Id").ValueGeneratedOnAdd();
        b.Property<string>("PollCode").IsRequired();
        b.Property<int>("OptionId");
        b.Property<DateTime>("VoteTime");
        b.HasKey("Id");
        b.ToTable("Analytics");
    });
}
```

Khi bạn sửa model, EF Core so sánh với snapshot để biết cần tạo migration nào.

### 8.5 Lệnh Migration

#### Tạo migration mới

```bash
dotnet ef migrations add AddNewColumn --project PollService
```

**Khi nào dùng:**
- Thêm/xóa/sửa property trong model
- Thêm/xóa bảng

**Output:** Tạo file mới trong `Migrations/`

#### Apply migration lên database

```bash
dotnet ef database update --project PollService
```

**Khi nào dùng:**
- Lần đầu chạy project (tạo database)
- Sau khi pull code có migration mới
- Deploy lên server mới

**EF Core sẽ:**
1. Kết nối database
2. Kiểm tra bảng `__EFMigrationsHistory` (lưu migration nào đã apply)
3. Chạy migration chưa apply (theo thứ tự timestamp)

#### Xem SQL migration sẽ chạy

```bash
dotnet ef migrations script --project PollService
```

**Output:** File SQL có thể chạy thủ công trong SQL Server Management Studio.

#### Rollback migration

```bash
dotnet ef database update PreviousMigrationName --project PollService
```

Chạy `Down()` của migration sau đó.

### 8.6 Bảng __EFMigrationsHistory

EF Core tự tạo bảng này để tracking:

| MigrationId | ProductVersion |
|-------------|----------------|
| 20260724153449_InitialCreate | 8.0.0 |
| 20260725120000_AddExpireAt | 8.0.0 |

Khi chạy `dotnet ef database update`:
1. Đọc bảng này → biết `InitialCreate` đã apply
2. Tìm migration mới hơn trong code
3. Apply migration mới → INSERT row mới vào bảng

### 8.7 Docker — Migration tự động

**Trong Docker Compose**, migration chạy tự động khi container start:

**Option 1:** Environment variable
```yaml
services:
  poll-service:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

**Option 2:** Command trong Dockerfile
```dockerfile
# Thêm vào Dockerfile
RUN dotnet ef database update
```

**Option 3:** Code trong Program.cs (recommend)
```csharp
var app = builder.Build();

// Tự động apply migrations khi app start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PollDbContext>();
    db.Database.Migrate();  // Apply pending migrations
}

app.Run();
```

**Ưu điểm:** Không cần chạy lệnh thủ công mỗi lần deploy.

---

## 9. Cài đặt Docker

### 9.1 Cài Docker Desktop

**Windows:**
1. Tải Docker Desktop: https://www.docker.com/products/docker-desktop/
2. Cài đặt (yêu cầu Windows 10/11 Pro hoặc WSL2)
3. Khởi động Docker Desktop
4. Kiểm tra: Mở CMD, chạy `docker --version`

**Mac:**
1. Tải Docker Desktop for Mac (Intel hoặc Apple Silicon)
2. Cài đặt
3. Khởi động Docker Desktop
4. Kiểm tra: `docker --version`

### 9.2 Chạy project bằng Docker Compose

**Bước 1: Clone code**
```bash
git clone <repo-url>
cd poll-survey
```

**Bước 2: Tạo file .env**
```bash
# Sao chép từ template
cp .env.example .env

# Hoặc tạo thủ công
echo SA_PASSWORD=YourStrong@Passw0rd > .env
```

**Bước 3: Build và chạy**
```bash
docker compose up --build
```

**Giải thích lệnh:**
- `docker compose up` = khởi động tất cả services
- `--build` = build lại image từ Dockerfile (lần đầu hoặc có thay đổi code)

**Chờ container start:**
```
[+] Running 6/6
 ✔ Container poll-sqlserver         Healthy
 ✔ Container poll-service           Started
 ✔ Container vote-service           Started
 ✔ Container analytics-service      Started
 ✔ Container ocelot-gateway         Started
 ✔ Container poll-client            Started
```

**Bước 4: Truy cập**
- Frontend: http://localhost:8081
- Gateway API: http://localhost:5000
- SQL Server: `localhost:1433` (user: sa, password: từ .env)

**Bước 5: Dừng containers**
```bash
# Dừng nhưng giữ containers
docker compose stop

# Dừng và xóa containers (giữ volumes)
docker compose down

# Xóa cả volumes (mất dữ liệu DB)
docker compose down -v
```

### 9.3 Export Docker image

**Export toàn bộ images:**
```bash
# 1. Xem images đang có
docker images

# 2. Save từng image
docker save poll-survey-poll-service:latest -o poll-service.tar
docker save poll-survey-vote-service:latest -o vote-service.tar
docker save poll-survey-analytics-service:latest -o analytics-service.tar
docker save poll-survey-gateway:latest -o gateway.tar
docker save poll-survey-client:latest -o client.tar

# 3. Nén lại (optional)
tar -czvf poll-survey-images.tar.gz *.tar
```

**Chuyển sang máy khác:**
```bash
# Copy file .tar sang máy mới (USB, SCP, ...)
scp poll-survey-images.tar.gz user@other-machine:/tmp/

# Ở máy mới, giải nén và load
tar -xzvf poll-survey-images.tar.gz
docker load -i poll-service.tar
docker load -i vote-service.tar
docker load -i analytics-service.tar
docker load -i gateway.tar
docker load -i client.tar

# Chạy
docker compose up
```

### 9.4 Docker Hub (recommend)

**Push lên Docker Hub:**
```bash
# 1. Login
docker login

# 2. Tag images
docker tag poll-survey-poll-service:latest yourusername/poll-service:latest
docker tag poll-survey-vote-service:latest yourusername/vote-service:latest

# 3. Push
docker push yourusername/poll-service:latest
docker push yourusername/vote-service:latest

# 4. Sửa docker-compose.yml
services:
  poll-service:
    image: yourusername/poll-service:latest
    # Xóa phần build
```

**Ở máy khác:**
```bash
# Chỉ cần pull và chạy
docker compose pull
docker compose up
```


---

## 10. Chuyển code sang máy khác

### 10.1 Dùng Git (recommend)

**Máy hiện tại:**
```bash
# 1. Commit code
git add .
git commit -m "Add Docker Compose setup"

# 2. Push lên GitHub/GitLab
git push origin main
```

**Máy mới:**
```bash
# 1. Clone code
git clone https://github.com/username/poll-survey.git
cd poll-survey

# 2. Tạo file .env (QUAN TRỌNG - file này không được commit)
cp .env.example .env
# Sửa mật khẩu nếu cần

# 3. Chạy bằng Docker
docker compose up --build

# Hoặc chạy development (cần .NET 8 + Node.js)
# Backend:
dotnet restore
dotnet ef database update --project PollService
dotnet ef database update --project VoteService
dotnet ef database update --project AnalyticsService
dotnet run --project OcelotGateway

# Frontend (terminal khác):
cd client
npm install
npm run serve
```

### 10.2 Copy thủ công (không dùng Git)

**Máy hiện tại:**
```
1. Nén folder project:
   - Windows: Click phải → Send to → Compressed folder
   - Mac: Click phải → Compress

2. Copy file .zip sang máy mới (USB, Google Drive, AirDrop...)
```

**Máy mới:**
```
1. Giải nén folder

2. Tạo file .env:
   - Copy .env.example thành .env
   - Sửa SA_PASSWORD nếu muốn

3. Mở Docker Desktop

4. Mở Terminal trong folder project

5. Chạy: docker compose up --build

6. Truy cập: http://localhost:8081
```

### 10.3 Development setup (không dùng Docker)

**Yêu cầu:**
- .NET SDK 8.0: https://dotnet.microsoft.com/download/dotnet/8.0
- Node.js 18+: https://nodejs.org/
- SQL Server LocalDB (có sẵn trong Visual Studio) hoặc SQL Server Express

**Bước 1: Clone/copy code**
```bash
git clone <repo> hoặc copy folder
cd poll-survey
```

**Bước 2: Backend**
```bash
# Restore NuGet packages
dotnet restore

# Tạo database và tables (chạy cho 3 service)
dotnet ef database update --project PollService
dotnet ef database update --project VoteService
dotnet ef database update --project AnalyticsService

# Kiểm tra database đã tạo:
# - Mở SQL Server Management Studio
# - Connect: (localdb)\mssqllocaldb
# - Xem databases: PollDB, VoteDB, AnalyticsDB
```

**Bước 3: Chạy backend services (4 terminal)**

Terminal 1 — PollService:
```bash
cd PollService
dotnet run
# Output: Now listening on: https://localhost:5001
```

Terminal 2 — VoteService:
```bash
cd VoteService
dotnet run
# Output: Now listening on: https://localhost:5002
```

Terminal 3 — AnalyticsService:
```bash
cd AnalyticsService
dotnet run
# Output: Now listening on: https://localhost:5003
```

Terminal 4 — OcelotGateway:
```bash
cd OcelotGateway
dotnet run
# Output: Now listening on: https://localhost:5000
```

**Bước 4: Frontend**

Terminal 5:
```bash
cd client
npm install
npm run serve
# Output: Local: http://localhost:8080
```

**Bước 5: Truy cập**
- Mở browser: http://localhost:8080

### 10.4 Troubleshooting

#### Lỗi: Port already in use

```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:5001 | xargs kill -9
```

#### Lỗi: Database connection failed

**Kiểm tra connection string trong `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PollDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Nếu dùng SQL Server Express, đổi thành:**
```json
"Server=localhost\\SQLEXPRESS;Database=PollDB;Trusted_Connection=True;TrustServerCertificate=True"
```

#### Lỗi: Cannot connect to SQL Server in Docker

```bash
# Xem logs
docker logs poll-sqlserver

# Kiểm tra container healthy
docker ps

# Nếu không healthy, đổi mật khẩu trong .env
# Mật khẩu phải có: chữ hoa, chữ thường, số, ký tự đặc biệt
SA_PASSWORD=YourStrong@Passw0rd123
```

#### Lỗi: CORS blocked

**Trong Docker:** Frontend phải gọi `http://localhost:5000` (gateway), không gọi trực tiếp service.

**Development:** Kiểm tra `Program.cs` của service có:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

---

## 11. Các Port và URL

### 11.1 Development (local, không dùng Docker)

| Service | Port | URL | Swagger |
|---------|------|-----|---------|
| OcelotGateway | 5000 | https://localhost:5000 | https://localhost:5000/swagger |
| PollService | 5001 | https://localhost:5001 | https://localhost:5001/swagger |
| VoteService | 5002 | https://localhost:5002 | https://localhost:5002/swagger |
| AnalyticsService | 5003 | https://localhost:5003 | https://localhost:5003/swagger |
| Vue Dev Server | 8080 | http://localhost:8080 | - |
| SQL Server | 1433 | (localdb)\mssqllocaldb | - |

**Frontend gọi API qua:**
- HTTP: Gateway `https://localhost:5000/api/*`
- SignalR: Trực tiếp `https://localhost:5002/hubs/vote`

---

### 11.2 Docker Compose (production)

| Service | Container Name | Internal Port | External Port | URL từ host |
|---------|---------------|---------------|---------------|-------------|
| Gateway | ocelot-gateway | 8080 | 5000 | http://localhost:5000 |
| PollService | poll-service | 8080 | 5001 | http://localhost:5001 |
| VoteService | vote-service | 8080 | 5002 | http://localhost:5002 |
| AnalyticsService | analytics-service | 8080 | 5003 | http://localhost:5003 |
| Vue Client | poll-client | 80 | 8081 | http://localhost:8081 |
| SQL Server | poll-sqlserver | 1433 | 1433 | localhost:1433 |

**Internal network (container ↔ container):**
- Gateway → PollService: `http://poll-service:8080`
- VoteService → PollService: `http://poll-service:8080`
- VoteService → AnalyticsService: `http://analytics-service:8080`

**Frontend trong Docker:**
- Client → Gateway: `http://localhost:5000` (từ browser host)
- Client → SignalR: `http://localhost:5002/hubs/vote`

---

### 11.3 Kiểm tra port đang chạy

**Windows:**
```bash
netstat -ano | findstr :5001
```

**Mac/Linux:**
```bash
lsof -i :5001
```

**Docker:**
```bash
docker ps
# Cột PORTS hiển thị mapping: 0.0.0.0:5001->8080/tcp
```

---

## 12. Tổng kết

### 12.1 Tech stack

**Backend:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core 8.0 (ORM)
- SQL Server 2022 (3 databases)
- SignalR (WebSocket realtime)
- Ocelot 23.3 (API Gateway)

**Frontend:**
- Vue 3 Composition API
- Vue Router 4
- Axios (HTTP client)
- @microsoft/signalr (realtime client)
- TailwindCSS + Custom CSS
- QRCode.js

**DevOps:**
- Docker + Docker Compose
- Multi-stage Dockerfile
- NGINX (serve Vue production)

### 12.2 Design patterns

- **Microservices Architecture** — 3 service độc lập
- **API Gateway Pattern** — Single entry point
- **Database per Service** — Mỗi service có DB riêng
- **Event-driven** — SignalR push events
- **Repository Pattern** — DbContext như repository
- **Dependency Injection** — ASP.NET Core built-in DI

### 12.3 Workflow tóm tắt

```
Tạo poll → Lưu PollDB → Chia sẻ code
                             ↓
User join → Validate poll → Vote → Lưu VoteDB
                                      ↓
                          Tính kết quả → Broadcast SignalR
                                      ↓
                          Fire-and-forget → Lưu AnalyticsDB
                                      ↓
                          Dashboard nhận event → UI update
```

### 12.4 Bảo mật

**Hiện tại (demo):**
- Không có authentication/authorization
- Voter token chỉ chống vote 2 lần cơ bản
- Không mã hóa database
- CORS cho phép mọi origin (development)

**Cần cải thiện cho production:**
- JWT authentication cho creator
- Rate limiting (chống spam vote)
- HTTPS bắt buộc
- Input sanitization (XSS)
- SQL injection đã được EF Core ngăn chặn
- Environment variables cho secrets
- CORS config strict

### 12.5 Mở rộng

**Tính năng có thể thêm:**
- User authentication (Google, GitHub OAuth)
- Poll templates
- Export results (PDF, Excel)
- Poll scheduling (tự đóng theo lịch)
- Vote analytics dashboard nâng cao
- Email notification
- Multi-language support
- Dark mode

**Scale:**
- Redis cache cho kết quả vote
- Message queue (RabbitMQ, Kafka)
- Load balancer (multiple instances)
- CDN cho static files
- Database replication

---
