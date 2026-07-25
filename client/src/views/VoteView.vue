<template>
  <div class="vote-page">
    <!-- Loading phòng -->
    <div v-if="loading" class="state-center">
      <div class="spinner spinner-blue" style="width:28px;height:28px;margin:0 auto 12px;"></div>
      <p class="fs-sm text-3">Đang tải phòng bình chọn...</p>
    </div>

    <!-- Không tìm thấy -->
    <div v-else-if="notFound" class="card state-card text-center">
      <div class="state-icon state-icon--red mb-3">
        <SearchX :size="28" color="var(--red)" />
      </div>
      <h2 class="state-title">Không tìm thấy phòng</h2>
      <p class="page-sub mb-3">Mã <strong>{{ routeCode }}</strong> không tồn tại hoặc đã bị xóa.</p>
      <router-link to="/" class="btn btn-outline">
        <ArrowLeft :size="14" /> Quay lại trang chủ
      </router-link>
    </div>

    <!-- Đã vote rồi -->
    <div v-else-if="alreadyVoted && poll" class="card state-card text-center">
      <div class="state-icon state-icon--green mb-3">
        <CheckCircle2 :size="28" color="var(--green)" />
      </div>
      <h2 class="state-title">Bạn đã bình chọn!</h2>
      <p class="page-sub">Cảm ơn bạn đã tham gia phòng <strong>{{ poll.code }}</strong>.</p>
    </div>

    <!-- Đã vote thành công (vừa gửi) -->
    <div v-else-if="voted" class="card state-card text-center">
      <div class="state-icon state-icon--green mb-3" style="animation: popIn .35s cubic-bezier(.34,1.56,.64,1);">
        <CheckCircle2 :size="28" color="var(--green)" />
      </div>
      <h2 class="state-title">Đã ghi nhận!</h2>
      <p class="page-sub">Cảm ơn bạn đã tham gia bình chọn.</p>
    </div>

    <!-- Form vote -->
    <div v-else-if="poll" class="vote-card">
      <!-- Header -->
      <div class="vote-header">
        <div class="d-flex align-center justify-between mb-2">
          <span class="badge badge-blue">{{ poll.code }}</span>
          <span class="badge" :class="isExpired ? 'badge-red' : 'badge-green'">
            <span v-if="!isExpired" class="live-dot"></span>
            {{ isExpired ? 'Đã đóng' : 'Đang mở' }}
          </span>
        </div>
        <h1 class="vote-question">{{ poll.question }}</h1>
        <p class="vote-type">
          <component :is="typeIcon" :size="13" />
          {{ poll.questionType }}
        </p>
      </div>

      <!-- Closed notice -->
      <div v-if="isExpired" class="vote-expired">
        <Lock :size="16" />
        Cuộc bình chọn này đã kết thúc. Bạn không thể vote nữa.
      </div>

      <!-- Vote form -->
      <form v-else class="vote-body" @submit.prevent="submit">

        <!-- Multiple Choice / Yes-No -->
        <div v-if="['Multiple Choice', 'Yes / No'].includes(poll.questionType)" class="option-list">
          <label
            v-for="opt in poll.options" :key="opt.id"
            class="vote-option"
            :class="{ selected: selectedId === opt.id, error: submitError && !selectedId }"
          >
            <input type="radio" :value="opt.id" v-model="selectedId" class="visually-hidden" />
            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: selectedId === opt.id }"></div>
            </div>
            <span class="vote-option-text">{{ opt.text }}</span>
            <Check v-if="selectedId === opt.id" :size="15" class="vote-option-check" />
          </label>
          <p v-if="submitError && !selectedId" class="inline-error">
            <AlertCircle :size="13" /> Vui lòng chọn một phương án
          </p>
        </div>

        <!-- Rating -->
        <div v-else-if="poll.questionType === 'Rating'" class="rating-wrap">
          <div class="star-row">
            <button
              v-for="s in 5" :key="s" type="button"
              class="star-btn" :class="{ on: s <= Number(voteValue), hover: s <= hoverStar }"
              @click="voteValue = String(s)"
              @mouseenter="hoverStar = s"
              @mouseleave="hoverStar = 0"
            >
              <Star :size="36" :fill="(s <= (hoverStar || Number(voteValue))) ? 'currentColor' : 'none'" />
            </button>
          </div>
          <p class="rating-label">
            {{ voteValue ? ratingLabels[Number(voteValue) - 1] : 'Chọn số sao đánh giá' }}
          </p>
          <p v-if="submitError && !voteValue" class="inline-error">
            <AlertCircle :size="13" /> Vui lòng chọn số sao
          </p>
        </div>

        <!-- Open Text -->
        <div v-else-if="poll.questionType === 'Open Text'" class="opentext-wrap">
          <textarea
            v-model="voteValue" rows="4" class="form-control"
            :class="{ 'is-error': submitError && !voteValue.trim() }"
            placeholder="Nhập phản hồi của bạn..."
          ></textarea>
          <p v-if="submitError && !voteValue.trim()" class="inline-error">
            <AlertCircle :size="13" /> Vui lòng nhập phản hồi
          </p>
        </div>

        <!-- Submit -->
        <button
          type="submit"
          class="btn btn-primary btn-lg btn-block submit-btn"
          :class="{ submitting: submitting, done: submitDone }"
          :disabled="submitting"
        >
          <span v-if="submitting" class="spinner"></span>
          <Check v-else-if="submitDone" :size="18" />
          <Send v-else :size="16" />
          {{ submitting ? 'Đang gửi...' : submitDone ? 'Đã ghi nhận!' : 'Gửi bình chọn' }}
        </button>
      </form>
    </div>

    <!-- Manual code (no route param) -->
    <div v-else class="card state-card" style="max-width:420px;">
      <p class="label-upper mb-1">Tham gia bình chọn</p>
      <h1 style="font-size:18px;font-weight:800;margin-bottom:4px;">Nhập mã phòng</h1>
      <p class="page-sub mb-3">Nhập mã 6 chữ số từ người tạo</p>

      <form @submit.prevent="loadPoll(manualCode)">
        <div class="code-boxes-small mb-3" ref="manualBoxRef">
          <input
            v-for="(_, i) in 6" :key="i"
            :ref="el => { if(el) manualInputs[i] = el }"
            type="text" inputmode="numeric" maxlength="1"
            class="code-box" :class="{ error: manualError }"
            :value="manualDigits[i]"
            @input="onManualInput(i, $event)"
            @keydown="onManualKeydown(i, $event)"
            @paste.prevent="onManualPaste($event)"
          />
        </div>
        <p v-if="manualError" class="inline-error justify-center">
          <AlertCircle :size="13" /> {{ manualError }}
        </p>
        <button type="submit" class="btn btn-primary btn-lg btn-block mt-3" :disabled="loadingManual">
          <span v-if="loadingManual" class="spinner"></span>
          <LogIn v-else :size="16" />
          Vào phòng
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { pollApi } from '../services/api';
import { getVoterToken } from '../helpers/voterToken';
import {
  CheckCircle2, Check, AlertCircle, Lock, Send,
  LogIn, Star, ArrowLeft, SearchX,
  BarChart2, ToggleLeft, MessageSquare,
} from 'lucide-vue-next';

const route  = useRoute();
const router = useRouter();

const routeCode  = computed(() => route.params.code || '');
const loading    = ref(false);
const loadingManual = ref(false);
const submitting = ref(false);
const submitDone = ref(false);
const submitError = ref(false);
const poll       = ref(null);
const voted      = ref(false);
const alreadyVoted = ref(false);
const notFound   = ref(false);
const selectedId = ref(null);
const voteValue  = ref('');
const hoverStar  = ref(0);

// Manual OTP input
const manualInputs = reactive([]);
const manualDigits = reactive(Array(6).fill(''));
const manualError  = ref('');
const manualBoxRef = ref(null);
const manualCode   = computed(() => manualDigits.join(''));

const ratingLabels = ['Rất tệ', 'Tệ', 'Trung bình', 'Tốt', 'Rất tốt'];

const typeIcon = computed(() => {
  if (!poll.value) return BarChart2;
  const map = { 'Multiple Choice': BarChart2, 'Yes / No': ToggleLeft, 'Rating': Star, 'Open Text': MessageSquare };
  return map[poll.value.questionType] || BarChart2;
});

const isExpired = computed(() => {
  if (!poll.value) return false;
  if (poll.value.status !== 'Active') return true;
  return new Date(poll.value.expireAt) <= new Date();
});

import { onMounted } from 'vue';

onMounted(() => {
  if (routeCode.value) loadPoll(routeCode.value);
});

const loadPoll = async (code) => {
  if (!code) return;
  loading.value = true;
  notFound.value = false;
  try {
    const r = await pollApi.checkPoll(code);
    poll.value = r.data;
    if (localStorage.getItem(`voted_${code}`) === 'true') {
      alreadyVoted.value = true;
    }
  } catch {
    notFound.value = true;
  } finally {
    loading.value = false;
  }
};

// Manual OTP helpers
const mFocusNext = (i) => { if (i < 5) manualInputs[i + 1]?.focus(); };
const mFocusPrev = (i) => { if (i > 0) manualInputs[i - 1]?.focus(); };

const onManualInput = (i, e) => {
  const val = e.target.value.replace(/\D/g, '').slice(-1);
  manualDigits[i] = val; e.target.value = val;
  manualError.value = '';
  if (val) mFocusNext(i);
};
const onManualKeydown = (i, e) => {
  if (e.key === 'Backspace') { if (manualDigits[i]) manualDigits[i] = ''; else mFocusPrev(i); }
  if (e.key === 'ArrowLeft')  mFocusPrev(i);
  if (e.key === 'ArrowRight') mFocusNext(i);
};
const onManualPaste = (e) => {
  const text = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
  text.split('').forEach((c, i) => { manualDigits[i] = c; });
  manualInputs[Math.min(text.length, 5)]?.focus();
};

const shakeManual = () => {
  const el = manualBoxRef.value;
  if (!el) return;
  el.classList.add('shake');
  setTimeout(() => el.classList.remove('shake'), 500);
};

const loadManual = async () => {
  const code = manualCode.value;
  if (code.length < 6) { manualError.value = 'Vui lòng nhập đủ 6 chữ số'; shakeManual(); return; }
  loadingManual.value = true;
  try {
    await loadPoll(code);
    if (notFound.value) { manualError.value = 'Không tìm thấy phòng bình chọn này'; shakeManual(); }
  } finally {
    loadingManual.value = false;
  }
};

// Submit vote
const submit = async () => {
  const type = poll.value.questionType;
  const invalid =
    (['Multiple Choice', 'Yes / No'].includes(type) && !selectedId.value) ||
    (['Rating', 'Open Text'].includes(type) && !voteValue.value);

  if (invalid) {
    submitError.value = true;
    // shake the form
    return;
  }

  submitError.value = false;
  submitting.value  = true;
  try {
    await pollApi.submitVote({
      pollCode:   poll.value.code,
      voterToken: getVoterToken(),
      optionId:   selectedId.value || 0,
      voteValue:  voteValue.value,
    });
    localStorage.setItem(`voted_${poll.value.code}`, 'true');
    submitDone.value = true;
    // brief green flash then show success state
    setTimeout(() => { voted.value = true; }, 700);
  } catch (e) {
    // Show inline error, no toast
    submitError.value = true;
    // If already voted from server
    if (e.message?.includes('đã thực hiện')) {
      alreadyVoted.value = true;
    }
  } finally {
    submitting.value = false;
  }
};
</script>

<style scoped>
.vote-page {
  display: flex; flex-direction: column; align-items: center;
  padding: 24px 16px 64px; min-height: calc(100vh - 0px);
  justify-content: flex-start; padding-top: 40px;
}

/* State cards (notFound, success, etc.) */
.state-center { text-align: center; padding: 80px 20px; }

.state-card {
  max-width: 440px; width: 100%; margin: 0 auto;
}

.state-icon {
  width: 64px; height: 64px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  margin: 0 auto;
}

.state-icon--green { background: var(--green-light); border: 1px solid #86efac; }
.state-icon--red   { background: var(--red-light);   border: 1px solid #fca5a5; }

.state-title { font-size: 18px; font-weight: 800; color: var(--text); margin-bottom: 6px; }

/* Vote card */
.vote-card {
  width: 100%; max-width: 560px;
  background: var(--surface); border: 1px solid var(--border);
  border-radius: var(--radius-lg); box-shadow: var(--shadow);
  overflow: hidden;
}

.vote-header {
  padding: 24px 24px 20px;
  border-bottom: 1px solid var(--border);
  background: var(--surface);
}

.vote-question {
  font-size: 19px; font-weight: 800; color: var(--text);
  letter-spacing: -.02em; line-height: 1.3; margin: 8px 0 6px;
}

.vote-type {
  display: inline-flex; align-items: center; gap: 5px;
  font-size: 12.5px; color: var(--text-4); font-weight: 600;
}

.vote-expired {
  margin: 16px 24px 0;
  display: flex; align-items: center; gap: 8px;
  padding: 12px 14px; border-radius: var(--radius);
  background: var(--red-light); color: #991b1b;
  border: 1px solid #fca5a5; font-size: 13.5px; font-weight: 500;
}

.vote-body { padding: 20px 24px 24px; }

/* Options */
.option-list { display: flex; flex-direction: column; gap: 8px; margin-bottom: 20px; }

.vote-option {
  display: flex; align-items: center; gap: 12px;
  padding: 13px 16px; border: 1.5px solid var(--border);
  border-radius: var(--radius); cursor: pointer;
  background: var(--surface); transition: all .12s;
  user-select: none; position: relative;
}

.vote-option:hover { border-color: var(--blue-border); background: var(--blue-light); }

.vote-option.selected {
  border-color: var(--blue); background: var(--blue-light);
  box-shadow: 0 0 0 1px var(--blue);
}

.vote-option.error { border-color: var(--red); }

.vote-option-radio {
  width: 18px; height: 18px; border-radius: 50%;
  border: 2px solid var(--border-2); flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  transition: border-color .12s;
}

.vote-option.selected .vote-option-radio { border-color: var(--blue); }

.radio-inner {
  width: 9px; height: 9px; border-radius: 50%;
  background: var(--blue); transform: scale(0);
  transition: transform .15s cubic-bezier(.34,1.56,.64,1);
}

.radio-inner.filled { transform: scale(1); }

.vote-option-text { flex: 1; font-size: 14.5px; font-weight: 500; color: var(--text); }
.vote-option.selected .vote-option-text { color: var(--blue); font-weight: 600; }

.vote-option-check { color: var(--blue); flex-shrink: 0; }

/* Rating */
.rating-wrap { text-align: center; padding: 8px 0 20px; }

.star-row { display: flex; gap: 4px; justify-content: center; margin-bottom: 10px; }

.star-btn {
  background: none; border: none; cursor: pointer; padding: 4px;
  color: var(--border-2); transition: color .1s, transform .12s;
  line-height: 0;
}

.star-btn.on, .star-btn.hover { color: var(--amber); }
.star-btn:hover { transform: scale(1.15); }

.rating-label { font-size: 14px; font-weight: 600; color: var(--text-3); min-height: 20px; }

/* Open text */
.opentext-wrap { margin-bottom: 20px; }
.form-control.is-error { border-color: var(--red); }

/* Inline error */
.inline-error {
  display: flex; align-items: center; gap: 5px;
  font-size: 12.5px; color: var(--red); font-weight: 600; margin-top: 8px;
}
.justify-center { justify-content: center; }

/* Submit button states */
.submit-btn { transition: all .2s; }
.submit-btn.done { background: var(--green) !important; border-color: var(--green) !important; }

/* OTP boxes (manual) */
.code-boxes-small { display: flex; gap: 7px; justify-content: center; }

.code-box {
  width: 42px; height: 50px;
  border: 1.5px solid var(--border); border-radius: var(--radius);
  text-align: center; font-size: 20px; font-weight: 800;
  color: var(--text); background: var(--surface);
  outline: none; transition: border-color .12s, box-shadow .12s;
}

.code-box:focus { border-color: var(--blue); box-shadow: 0 0 0 3px rgba(37,99,235,.1); }
.code-box.error { border-color: var(--red); }

@keyframes shake {
  0%,100% { transform: translateX(0); }
  15%     { transform: translateX(-6px); }
  30%     { transform: translateX(6px); }
  45%     { transform: translateX(-4px); }
  60%     { transform: translateX(4px); }
  75%     { transform: translateX(-2px); }
}

.shake { animation: shake .45s ease; }

@keyframes popIn {
  from { transform: scale(.5); opacity: 0; }
  to   { transform: scale(1); opacity: 1; }
}

.visually-hidden {
  position: absolute; width: 1px; height: 1px;
  padding: 0; margin: -1px; overflow: hidden;
  clip: rect(0,0,0,0); white-space: nowrap; border: 0;
}
</style>
