# Realtime Poll Sync Guide

## Architecture Overview

### Services
1. **OcelotGateway** (Port 5000) - API Gateway
   - Routes `/api/polls/*` → PollService (5001)
   - Routes `/api/votes/*` → VoteService (5002)
   - Routes `/hubs/vote/*` → VoteService (5002)

2. **PollService** (Port 5001) - Poll Management
   - Create, update, delete, close polls
   - Auto-close background service (checks every 1 minute)
   - Validates poll status before voting

3. **VoteService** (Port 5002) - Vote & SignalR
   - Accept votes
   - Real-time updates via SignalR
   - Broadcast poll closed events

### Client (Port 8080)
- Vue.js frontend
- Connects to OcelotGateway for REST APIs
- Connects directly to VoteService for SignalR (`.env.local`)

---

## Realtime Flow

### Vote Submission Flow
```
User submits vote
  ↓
VoteView calls pollApi.submitVote()
  ↓
POST https://localhost:5000/api/votes
  ↓
OcelotGateway routes to VoteService:5002/api/Votes
  ↓
VotesController validates:
  - Vote data not null
  - pollCode exists
  - voterToken exists
  - voter hasn't voted before
  - poll is active (via PollService)
  ↓
Save to database
  ↓
BroadcastVoteResults() via SignalR
  ↓
All connected clients receive VoteUpdated event
  ↓
Show toast: "Your vote has been recorded!"
```

### Poll Close Flow (Real-time)
```
Admin clicks "Stop"
  ↓
AnalyticsView calls broadcastPollClosed()
  ↓
POST https://localhost:5002/api/Votes/broadcast-closed
  ↓
VotesController emits "PollClosed" event to poll room
  ↓
All connected voters receive PollClosed event
  ↓
VoteView disables form, shows "Poll has ended"
  ↓
Voter sees warning toast
```

---

## Common Issues & Solutions

### Issue: POST /api/votes returns 400 Bad Request

**Possible causes:**
1. **Missing fields** - pollCode or voterToken not sent
2. **Invalid data** - both optionId and voteValue are empty
3. **Network** - request not reaching VoteService

**Solution:**
- Check browser console for request payload
- Verify VoteService is running on port 5002
- Check OcelotGateway routes are configured

### Issue: Vote submitted but no feedback

**Solution implemented:**
- Added toast notifications on success/error
- Show "Your vote has been recorded!" on success
- Show error messages on failure
- Disable submit button during submission

### Issue: Admin closes poll, but user doesn't see it

**Solution implemented:**
1. Admin closes poll → immediately broadcasts via SignalR
2. VoteView listens to "PollClosed" event
3. User's form auto-disables
4. User sees warning toast
5. No F5 needed

### Issue: Poll shows as closed but can still vote

**Solution implemented:**
- Backend validates poll status before accepting vote
- Returns 400 "Poll is closed" if poll status isn't Active
- Client shows error toast

---

## Testing Checklist

- [ ] Create poll with 10 minute expiry
- [ ] Open vote page and submit vote
- [ ] Check "Vote recorded!" toast appears
- [ ] Check analytics shows vote count updated
- [ ] Admin closes poll
- [ ] Vote page disables form immediately (no F5)
- [ ] Voter sees warning: "Poll has been closed by admin"
- [ ] Voter can't submit another vote
- [ ] Wait for auto-close (expiry time)
- [ ] Check both analytics and vote page show closed status

---

## Configuration Files

### .env.local (Client)
```
VUE_APP_API_BASE_URL=https://localhost:5000      # REST APIs
VUE_APP_VOTE_SERVICE_URL=https://localhost:5002  # SignalR direct
```

### appsettings.Development.json (VoteService)
- AllowedOrigins: includes `https://localhost:8080`
- Enables CORS for SignalR

### ocelot.json (OcelotGateway)
- `/api/votes/*` routes to VoteService:5002
- `/hubs/vote/*` routes to VoteService:5002 (WebSocket support)

---

## Code Changes Summary

1. **VoteView.vue** - Added toast notifications, realtime poll closed listener
2. **AnalyticsView.vue** - Broadcast poll closed immediately via SignalR
3. **VotesController.cs** - Enhanced error handling, validate vote data
4. **VoteHub.cs** - Cleaned up, renamed events, added error handler
5. **usePollHub.js** - Optimized handlers, better error logging
6. **PollAutoCloseService.cs** - Background job auto-closes expired polls

---

## Performance Notes

- SignalR reconnect delays: [0ms, 1s, 3s, 5s]
- Auto-close check interval: every 1 minute
- Fallback polling (when SignalR disconnected): every 6 seconds
