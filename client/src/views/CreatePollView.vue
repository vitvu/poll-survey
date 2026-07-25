<template>
  <div class="container">
    <div class="page-nav">
      <button class="btn btn-ghost btn-sm" @click="router.back()">
        <ChevronLeft :size="15" />
        Quay lại
      </button>
    </div>

    <div class="create-layout">
      <div class="card">
        <p class="label-upper mb-1">Tạo mới</p>
        <h1 class="page-title mb-3" style="font-size:20px;">Cấu hình cuộc bình chọn</h1>

        <form @submit.prevent="submit">
          <!-- Question -->
          <div class="form-group">
            <label class="form-label">Câu hỏi <span style="color:var(--red)">*</span></label>
            <input v-model="form.question" type="text" class="form-control"
              placeholder="Ví dụ: Bạn thích ngôn ngữ lập trình nào nhất?" required />
          </div>

          <!-- Type selector -->
          <div class="form-group">
            <label class="form-label">Loại câu hỏi</label>
            <div class="type-grid">
              <div v-for="t in questionTypes" :key="t.value"
                class="type-card" :class="{ active: form.questionType === t.value }"
                @click="form.questionType = t.value">
                <div class="type-card-icon" :class="{ active: form.questionType === t.value }">
                  <component :is="t.icon" :size="18" />
                </div>
                <div>
                  <div class="type-card-name">{{ t.label }}</div>
                  <div class="type-card-desc">{{ t.desc }}</div>
                </div>
                <div v-if="form.questionType === t.value" class="type-check">
                  <CheckCircle2 :size="16" />
                </div>
              </div>
            </div>
          </div>

          <!-- Expire -->
          <div class="form-group">
            <label class="form-label">Thời hạn bình chọn</label>
            <div class="expire-row">
              <label class="expire-opt" :class="{ active: expireMode === 'none' }" @click="expireMode = 'none'">
                <Infinity :size="16" />
                Không giới hạn
              </label>
              <label class="expire-opt" :class="{ active: expireMode === 'custom' }" @click="expireMode = 'custom'">
                <CalendarClock :size="16" />
                Đặt thời hạn
              </label>
            </div>
            <div v-if="expireMode === 'custom'" class="mt-2">
              <input v-model="form.expireAt" type="datetime-local" class="form-control" style="max-width:280px;" required />
            </div>
          </div>

          <!-- Options -->
          <div v-if="form.questionType === 'Multiple Choice'">
            <hr class="divider" />
            <div class="d-flex align-center justify-between mb-2">
              <label class="form-label mb-0">Các lựa chọn</label>
              <span class="fs-xs text-3">{{ validOptions }} / 6</span>
            </div>
            <div style="display:flex;flex-direction:column;gap:7px;">
              <div v-for="(opt, i) in form.options" :key="i" class="d-flex align-center gap-2">
                <span class="opt-num">{{ i + 1 }}</span>
                <input v-model="opt.text" type="text" class="form-control"
                  :placeholder="`Lựa chọn ${i + 1}`" style="flex:1;" />
                <button type="button" class="btn btn-danger btn-icon btn-sm"
                  :disabled="form.options.length <= 2" @click="removeOption(i)">
                  <X :size="13" />
                </button>
              </div>
            </div>
            <button v-if="form.options.length < 6" type="button"
              class="btn btn-ghost btn-sm mt-2" @click="addOption">
              <Plus :size="14" /> Thêm lựa chọn
            </button>
          </div>

          <hr class="divider" />
          <button type="submit" class="btn btn-primary btn-lg btn-block" :disabled="loading">
            <span v-if="loading" class="spinner"></span>
            <span>{{ loading ? 'Đang tạo...' : 'Tạo Cuộc Bình Chọn' }}</span>
          </button>
        </form>
      </div>

      <!-- Sidebar -->
      <div>
        <div class="card" style="background:var(--blue-light);border-color:var(--blue-border);">
          <p class="label-upper mb-2" style="color:var(--blue);">Hướng dẫn</p>
          <div class="tip-list">
            <div v-for="t in questionTypes" :key="t.value" class="tip-item">
              <component :is="t.icon" :size="13" class="tip-icon" />
              <div>
                <strong>{{ t.label }}</strong>
                {{ t.desc }}
              </div>
            </div>
          </div>
        </div>
        <div class="card mt-2">
          <p class="fs-sm text-3">Sau khi tạo bạn sẽ thấy link chia sẻ và kết quả vote ngay tại đây.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { pollApi } from '../services/api';
import { useToastStore } from '../stores/toastStore';
import {
  ChevronLeft, CheckCircle2, Plus, X,
  BarChart2, ToggleLeft, Star, MessageSquare,
  Infinity, CalendarClock,
} from 'lucide-vue-next';

const router = useRouter();
const toast  = useToastStore();
const loading    = ref(false);
const expireMode = ref('none');

const getDefaultExpire = () => {
  const d = new Date(); d.setDate(d.getDate() + 1);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};

const form = ref({
  question: '',
  questionType: 'Multiple Choice',
  expireAt: getDefaultExpire(),
  options: [{ text: '' }, { text: '' }],
});

const validOptions = computed(() => form.value.options.filter(o => o.text.trim()).length);

const questionTypes = [
  { value: 'Multiple Choice', icon: BarChart2,      label: 'Nhiều lựa chọn', desc: 'Chọn 1 trong nhiều đáp án' },
  { value: 'Yes / No',        icon: ToggleLeft,      label: 'Có / Không',     desc: 'Chỉ 2 đáp án Yes / No' },
  { value: 'Rating',          icon: Star,            label: 'Đánh giá sao',   desc: 'Chọn 1–5 sao' },
  { value: 'Open Text',       icon: MessageSquare,   label: 'Trả lời tự do',  desc: 'Nhập văn bản tùy ý' },
];

const addOption    = () => { if (form.value.options.length < 6) form.value.options.push({ text: '' }); };
const removeOption = (i) => { if (form.value.options.length > 2) form.value.options.splice(i, 1); };

const saveCreatorCode = (code) => {
  try {
    const list = JSON.parse(localStorage.getItem('createdPolls') || '[]');
    if (!list.includes(code)) { list.push(code); localStorage.setItem('createdPolls', JSON.stringify(list)); }
  } catch {}
};

const submit = async () => {
  if (!form.value.question.trim()) { toast.error('Vui lòng nhập câu hỏi.'); return; }
  if (form.value.questionType === 'Multiple Choice' && validOptions.value < 2) {
    toast.error('Cần ít nhất 2 lựa chọn hợp lệ.'); return;
  }
  loading.value = true;
  try {
    const code = Math.floor(100000 + Math.random() * 900000).toString();
    const far  = new Date(); far.setFullYear(far.getFullYear() + 100);
    const payload = {
      code, question: form.value.question.trim(),
      questionType: form.value.questionType,
      expireAt: expireMode.value === 'custom' ? new Date(form.value.expireAt).toISOString() : far.toISOString(),
      options: form.value.questionType === 'Multiple Choice'
        ? form.value.options.filter(o => o.text.trim()).map(o => ({ text: o.text.trim() }))
        : [],
    };
    const res = await pollApi.createPoll(payload);
    const pollData = res.data.poll || res.data;
    saveCreatorCode(pollData.code);
    toast.success('Đã tạo phòng bình chọn!');
    router.push({ name: 'Analytics', query: { code: pollData.code } });
  } catch (e) {
    toast.error(e.message || 'Lỗi khi tạo.');
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.page-nav { margin-bottom: 16px; }

.create-layout {
  display: grid; grid-template-columns: 1fr 260px; gap: 16px; align-items: start;
}

/* Type grid */
.type-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }

.type-card {
  display: flex; align-items: center; gap: 10px;
  padding: 12px 14px; border: 1.5px solid var(--border);
  border-radius: var(--radius); cursor: pointer;
  background: var(--surface); transition: border-color .12s, background .12s;
  user-select: none; position: relative;
}

.type-card:hover { border-color: var(--blue-border); background: var(--blue-light); }

.type-card.active {
  border-color: var(--blue); background: var(--blue-light);
  box-shadow: 0 0 0 1px var(--blue);
}

.type-card-icon {
  width: 36px; height: 36px; border-radius: 8px;
  background: var(--surface-3); border: 1px solid var(--border);
  display: flex; align-items: center; justify-content: center;
  color: var(--text-3); flex-shrink: 0; transition: all .12s;
}

.type-card-icon.active { background: var(--blue); border-color: var(--blue); color: #fff; }

.type-card-name { font-size: 13px; font-weight: 700; color: var(--text); line-height: 1.2; }
.type-card.active .type-card-name { color: var(--blue); }
.type-card-desc { font-size: 11.5px; color: var(--text-4); margin-top: 2px; }

.type-check { position: absolute; top: 8px; right: 8px; color: var(--blue); }

/* Expire row */
.expire-row { display: flex; gap: 8px; }

.expire-opt {
  display: flex; align-items: center; gap: 7px;
  padding: 8px 14px; border: 1.5px solid var(--border);
  border-radius: var(--radius); cursor: pointer; font-size: 13.5px; font-weight: 500;
  color: var(--text-3); background: var(--surface); transition: all .12s; user-select: none;
}

.expire-opt:hover { border-color: var(--blue-border); color: var(--text); }
.expire-opt.active { border-color: var(--blue); color: var(--blue); background: var(--blue-light); font-weight: 600; }

/* Options */
.opt-num {
  width: 24px; height: 24px; border-radius: 50%; flex-shrink: 0;
  background: var(--surface-3); border: 1px solid var(--border);
  font-size: 12px; font-weight: 700; color: var(--text-4);
  display: flex; align-items: center; justify-content: center;
}

/* Tips */
.tip-list { display: flex; flex-direction: column; gap: 9px; }
.tip-item {
  display: flex; align-items: flex-start; gap: 8px;
  font-size: 12.5px; color: var(--text-2); line-height: 1.5;
}
.tip-item strong { display: block; color: var(--blue); font-size: 12px; }
.tip-icon { color: var(--blue); margin-top: 2px; flex-shrink: 0; }

@media (max-width: 768px) {
  .create-layout { grid-template-columns: 1fr; }
  .type-grid { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 420px) {
  .type-grid { grid-template-columns: 1fr; }
}
</style>
