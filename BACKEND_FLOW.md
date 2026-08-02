# Backend Services Flow Documentation

## Overview
The poll-survey backend consists of 3 microservices that communicate synchronously and asynchronously:
1. **PollService** - Manages poll CRUD operations
2. **VoteService** - Handles vote submission and real-time updates via SignalR
3. **AnalyticsService** - Records and summarizes voting analytics

---

## 1. Create Poll Flow

### Request Path
```
Client → POST /api/polls → PollService
```

### PollsController.CreatePoll()
1. **Validate** question is not empty
2. **Normalize DateTime** to UTC (frontend sends ISO strings)
3. **Validate** expiration date is in future
4. **Check** poll code is unique in database
5. **Auto-generate Options** based on QuestionType:
   - "Multiple Choice": Use provided options (must be ≥2)
   - "Yes / No": Generate Yes and No options automatically
   - Others: Empty options list
6. **Set Timestamps**: CreatedAt = UtcNow, Status = "Active"
7. **Save to PollDB** → Return 201 with poll data

---

## 2. Vote Submission Flow

### Request Path
```
Client → POST /api/votes → VoteService
```

### VotesController.SubmitVote()
1. **Validate** PollCode and VoterToken are provided
2. **Check Duplicate**: Query VoteDB for existing vote with same PollCode + VoterToken
   - If found → Return 400 (already voted)
3. **Call PollService**: GET /api/polls/check/{pollCode}
   - Validates poll exists, is Active, and not expired
   - If failed → Return 400 (poll invalid/closed/expired)
4. **Save Vote**: Add vote record to VoteDB with timestamp
5. **Calculate Results**: Group all votes by OptionId, count per option
6. **Broadcast via SignalR**: Send "VoteUpdated" event to all clients in poll group
   - Group name: `poll_{pollCode}`
   - Data: totalVotes, voteResults array
7. **Send to Analytics** (fire-and-forget):
   - POST to AnalyticsService with: pollCode, optionId, voteTime
8. **Return 200 OK**

---

## 3. Real-Time Vote Results Flow

### SignalR Connection
```
Client WebSocket → /hubs/vote → VoteHub
```

### VoteHub Operations
- **JoinPollRoom(pollCode)**
  - Adds client to group: `poll_{pollCode}`
  - Receives VoteUpdated broadcasts
  
- **LeavePollRoom(pollCode)**
  - Removes client from group when navigating away

### Broadcasting
- When vote submitted: VoteService broadcasts to group
- When poll closed: PollService calls VoteService.BroadcastPollClosed()
  - VoteService broadcasts "PollClosed" event

---

## 4. Poll Management Flows

### Update Poll
```
PUT /api/polls/code/{code}
```
1. Find poll by code
2. Update Status, Question, ExpireAt
3. Save to PollDB
4. If Status changed to "Closed":
   - Call VoteService: POST /api/votes/broadcast-poll-closed
   - VoteService broadcasts to all connected clients

### Delete Poll
```
DELETE /api/polls/code/{code}
```
1. Find and delete poll from PollDB (Options cascade-delete)
2. Call VoteService: DELETE /api/votes/by-poll-code/{code}
   - VoteService deletes all votes for this poll

---

## 5. Analytics Flow

### Automatic Analytics Recording
```
VoteService → POST /api/analytics → AnalyticsService
```

### AnalyticsController.RecordVote()
1. Receive vote data from VoteService (fire-and-forget)
2. If VoteTime empty → Set to DateTime.Now
3. Save record to AnalyticsDB
4. Return 200 OK

### Get Poll Summary
```
GET /api/analytics/summary/{pollCode}
```
1. Query all analytics records for poll
2. Count total votes
3. Find most-voted option (highest count)
4. Return: { totalVotes, mostVotedOptionId }

---

## 6. Validation Flows

### Pre-Vote Validation
```
VoteService → GET /api/polls/check/{code} → PollService
```
1. **PollsController.ValidatePoll()**
   - Verify poll exists
   - Verify Status = "Active"
   - Verify ExpireAt > DateTime.UtcNow
   - Return 200 if valid, else 400/404

### Option Validation
```
VoteService → GET /api/polls/check-option/{optionId} → PollService
```
1. **PollsController.ValidateOption()**
   - Lookup option by ID
   - Return 200 if found, else 404

---

## Database Schema

### PollDB (PollService)
- **Polls**: Id, Code, Question, QuestionType, Status, ExpireAt, CreatedAt
- **Options**: Id, PollId, Text

### VoteDB (VoteService)
- **Votes**: Id, PollCode, OptionId, VoteValue, VoterToken, CreatedAt

### AnalyticsDB (AnalyticsService)
- **Analytics**: Id, PollCode, OptionId, VoteTime

---

## Error Handling

All services follow consistent error patterns:
- **400 Bad Request**: Business logic violations (poll closed, already voted, etc.)
- **404 Not Found**: Resource doesn't exist
- **500 Server Error**: Unhandled exceptions

Inter-service calls use fire-and-forget for non-critical operations:
- Analytics recording won't fail vote submission
- Poll closed broadcast won't fail vote submission
- Vote deletion won't fail poll deletion

---

## DateTime Handling

**Important**: All DateTime values are UTC-based
- Frontend sends ISO strings with Z suffix (e.g., "2026-08-02T12:00:00Z")
- C# deserializes these as Unspecified kind
- Services explicitly mark as UTC using `DateTime.SpecifyKind(..., Utc)`
- JSON serializer configured with `DateTimeZoneHandling.Utc` to always include "Z"
- Comparisons always use `DateTime.UtcNow` (never `DateTime.Now`)

---

## Inter-Service Communication

All services communicate via HTTP/HTTPS:
- **PollService**: http://localhost:5248
- **VoteService**: https://localhost:5002
- **AnalyticsService**: http://localhost:5125

Configuration can override defaults via appsettings.json:
```json
"Services": {
  "PollServiceUrl": "http://localhost:5248",
  "AnalyticsServiceUrl": "http://localhost:5125"
}
```

---

## Dependency Injection

All controllers inject dependencies:
- **DbContext**: Database access
- **IHttpClientFactory**: Inter-service HTTP calls
- **IConfiguration**: Config values
- **IHubContext<VoteHub>**: SignalR broadcasting
