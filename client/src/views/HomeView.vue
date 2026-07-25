<template>
  <div class="container">
    <div class="hero">
      <h1 class="hero-title">Tạo & Chia Sẻ Bình Chọn</h1>
      <p class="hero-sub">Không cần tài khoản. Kết quả trực tiếp. Chia sẻ qua mã 6 số.</p>
    </div>

    <div class="home-grid">
      <!-- Join -->
      <div class="card">
        <p class="label-upper mb-2">Tham gia bình chọn</p>
        <h2 class="card-h mb-1">Nhập mã phòng</h2>
        <p class="fs-sm text-3 mb-3">Nhận mã 6 chữ số từ người tạo phòng</p>

        <form @submit.prevent="joinPoll">
          <!-- OTP-style 6 boxes -->
          <div class="code-boxes" ref="boxesRef">
            <input
              v-for="(_, i) in 6" :key="i"
              :ref="el => { if(el) inputs[i] = el }"
              type="text"
              inputmode="numeric"
              maxlength="1"
              class="code-box"
              :class="{ error: codeError }"
              :value="digits[i]"
              @input="onInput(i, $event)"
              @keydown="onKeydown(i, $event)"
              @paste.prevent="onPaste($event)"
              autocomplete="off"
            />
          </div>
          <p v-if="codeError" class="code-error-msg">
            <AlertCircle :size="13" /> {{ codeError }}
          </p>

          <button type="submit" class="btn btn-primary btn-lg btn-block mt-3" :disabled="joinLoading">
            <span v-if="joinLoading" class="spinner"></span>
            <LogIn v-else :size="16" />
            {{ joinLoading ? 'Đang vào...' : 'Vào phòng' }}
          </button>
        </form>
      </div>

      <!-- Create -->
      <div class="card create-card">
        <p class="label-upper mb-2" style="color:rgba(255,255,255,.6);">Tạo cuộc bình chọn</p>
        <h2 class="card-h mb-1" style="color:#fff;">Bắt đầu ngay</h2>
        <p class="fs-sm mb-3" style="color:rgba(255,255,255,.7);">Câu hỏi, lựa chọn và kết quả real-time</p>

        <ul class="feature-list mb-3">
          <li><Check :size="13" /> Nhiều loại câu hỏi khác nhau</li>
          <li><Check :size="13" /> Kết quả cập nhật tức thì</li>
          <li><Check :size="13" /> Chia sẻ qua link hoặc mã số</li>
        </ul>

        <router-link to="/create" class="btn btn-block btn-white btn-lg">
          <Plus :size="15" />
          Tạo cuộc bình chọn
        </router-link>
      </div>
    </div>

    <!-- How it works -->
    <div class="how-section">
      <p class="label-upper" style="text-align:center;margin-bottom:24px;">Cách hoạt động</p>
      <div class="steps-row">
        <div class="step-card">
          <div class="step-num">1</div>
          <h3 class="step-title">Tạo câu hỏi</h3>
          <p class="step-desc">Điền câu hỏi và các đáp án lựa chọn</p>
        </div>
        <ChevronRight :size="18" class="step-arrow" />
        <div class="step-card">
          <div class="step-num">2</div>
          <h3 class="step-title">Chia sẻ link</h3>
          <p class="step-desc">Gửi mã phòng hoặc đường link cho người tham gia</p>
        </div>
        <ChevronRight :size="18" class="step-arrow" />
        <div class="step-card">
          <div class="step-num">3</div>
          <h3 class="step-title">Xem kết quả</h3>
          <p class="step-desc">Kết quả cập nhật ngay khi có người vote</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { AlertCircle, LogIn, Plus, Check, ChevronRight } from 'lucide-vue-next';

const router     = useRouter();
const inputs     = reactive([]);
const digits     = reactive(Array(6).fill(''));
const codeError  = ref('');
const joinLoading = ref(false);
const boxesRef   = ref(null);

const focusNext = (i) => { if (i < 5) inputs[i + 1]?.focus(); };
const focusPrev = (i) => { if (i > 0) inputs[i - 1]?.focus(); };

const onInput = (i, e) => {
  const val = e.target.value.replace(/\D/g, '').slice(-1);
  digits[i] = val;
  e.target.value = val;
  codeError.value = '';
  if (val) focusNext(i);
};

const onKeydown = (i, e) => {
  if (e.key === 'Backspace') {
    if (digits[i]) { digits[i] = ''; }
    else { focusPrev(i); }
  }
  if (e.key === 'ArrowLeft')  focusPrev(i);
  if (e.key === 'ArrowRight') focusNext(i);
};

const onPaste = (e) => {
  const text = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
  text.split('').forEach((c, i) => { digits[i] = c; });
  inputs[Math.min(text.length, 5)]?.focus();
  codeError.value = '';
};

const shake = () => {
  const el = boxesRef.value;
  if (!el) return;
  el.classList.add('shake');
  setTimeout(() => el.classList.remove('shake'), 500);
};

const joinPoll = async () => {
  const code = digits.join('');
  if (code.length < 6) {
    codeError.value = 'Vui lòng nhập đủ 6 chữ số';
    shake();
    return;
  }
  joinLoading.value = true;
  // Thực sự check xem phòng tồn tại không
  try {
    const { pollApi } = await import('../services/api');
    await pollApi.checkPoll(code);
    router.push(`/vote/${code}`);
  } catch {
    codeError.value = 'Không tìm thấy phòng bình chọn này';
    shake();
  } finally {
    joinLoading.value = false;
  }
};
</script>

<style scoped>
.hero { padding: 48px 0 36px; text-align: center; }
.hero-title { font-size: 34px; font-weight: 800; color: var(--text); letter-spacing: -.03em; line-height: 1.2; margin-bottom: 10px; }
.hero-sub { font-size: 15px; color: var(--text-3); max-width: 420px; margin: 0 auto; }
.card-h { font-size: 17px; font-weight: 700; }

.home-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; max-width: 760px; margin: 0 auto; }

/* OTP boxes */
.code-boxes {
  display: flex; gap: 8px; justify-content: center;
}

.code-box {
  width: 46px; height: 54px;
  border: 1.5px solid var(--border); border-radius: var(--radius);
  text-align: center; font-size: 22px; font-weight: 800;
  color: var(--text); background: var(--surface);
  outline: none; caret-color: var(--blue);
  transition: border-color .12s, box-shadow .12s;
}

.code-box:focus {
  border-color: var(--blue);
  box-shadow: 0 0 0 3px rgba(37,99,235,.1);
}

.code-box.error { border-color: var(--red); }

.code-error-msg {
  display: flex; align-items: center; gap: 5px;
  font-size: 12.5px; color: var(--red); font-weight: 600;
  margin-top: 8px; justify-content: center;
}

/* Shake animation */
@keyframes shake {
  0%,100% { transform: translateX(0); }
  15%      { transform: translateX(-6px); }
  30%      { transform: translateX(6px); }
  45%      { transform: translateX(-4px); }
  60%      { transform: translateX(4px); }
  75%      { transform: translateX(-2px); }
}

.shake { animation: shake .45s ease; }

/* Create card */
.create-card { background: var(--blue); border-color: var(--blue); }

.feature-list {
  list-style: none; display: flex; flex-direction: column; gap: 6px;
  font-size: 13px; color: rgba(255,255,255,.85);
}

.feature-list li { display: flex; align-items: center; gap: 7px; }

.btn-white { background: #fff; color: var(--blue); border-color: transparent; font-weight: 700; }
.btn-white:hover { background: rgba(255,255,255,.9); }

/* Steps */
.how-section { margin-top: 52px; padding-bottom: 20px; }
.steps-row { display: flex; align-items: center; justify-content: center; max-width: 620px; margin: 0 auto; }
.step-card { flex: 1; text-align: center; padding: 0 16px; }
.step-num {
  width: 36px; height: 36px; border-radius: 50%;
  background: var(--blue); color: #fff; font-size: 15px; font-weight: 800;
  display: flex; align-items: center; justify-content: center; margin: 0 auto 10px;
}
.step-title { font-size: 14px; font-weight: 700; color: var(--text); margin-bottom: 4px; }
.step-desc  { font-size: 12.5px; color: var(--text-4); line-height: 1.5; }
.step-arrow { color: var(--border-2); flex-shrink: 0; }

@media (max-width: 600px) {
  .home-grid { grid-template-columns: 1fr; }
  .hero { padding: 28px 0 24px; }
  .hero-title { font-size: 26px; }
  .steps-row { flex-direction: column; gap: 16px; }
  .step-arrow { transform: rotate(90deg); }
}
</style>
