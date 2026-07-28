<template>
  <div class="container">
    <button class="btn btn-ghost btn-sm mb-3" @click="router.push('/')">
      <ChevronLeft :size="15" /> Trang chủ
    </button>

    <div v-if="denied" class="card text-center" style="max-width:420px;margin:0 auto;">
      <Lock :size="32" color="var(--red)" style="margin-bottom:12px;" />
      <h2 style="font-size:17px;font-weight:800;margin-bottom:6px;">Không có quyền truy cập</h2>
      <p class="page-sub mb-3">Chỉ người tạo phòng <strong>{{ deniedCode }}</strong> mới xem được.</p>
      <router-link to="/" class="btn btn-ghost">Về trang chủ</router-link>
    </div>

    <div v-else-if="loading" class="text-center" style="padding:80px 0;">
      <div class="spinner spinner-blue" style="width:28px;height:28px;margin:0 auto 10px;"></div>
      <p class="fs-sm text-3">Đang tải...</p>
    </div>

    <template v-else-if="poll">
      <div class="card mb-3">
        <div class="d-flex align-center gap-2 mb-2 flex-wrap">
          <span class="badge badge-blue">{{ poll.code }}</span>
          <span class="badge" :class="isClosed ? 'badge-red' : 'badge-green'">
            <span v-if="!isClosed" class="live-dot"></span>
            {{ isClosed ? 'Đã đóng' : 'Đang mở' }}
          </span>
          <span class="badge badge-gray">{{ poll.questionType }}</span>
        </div>

        <h1 class="page-title mb-3" style="font-size:20px;">{{ poll.question }}</h1>

        <div class="d-flex align-center gap-2 flex-wrap mb-2">
          <button class="btn btn-outline" @click="copyLink">
            <Copy :size="14" /> Sao chép link
          </button>
          <router-link :to="`/vote/${poll.code}`" class="btn btn-outline" target="_blank">
            <ExternalLink :size="14" /> Mở trang vote
          </router-link>
          <button v-if="!isClosed" class="btn-close-poll" @click="showConfirm = true">
            <StopCircle :size="14" /> Dừng bình chọn
          </button>
          <button v-if="isClosed" class="btn-delete-poll" @click="showDeleteConfirm = true">
            <Trash2 :size="14" /> Xoá phòng
          </button>
          <span v-if="isClosed && false" class="badge badge-red" style="padding:7px 12px;">Đã đóng</span>
        </div>

        <div class="sr-pill" :class="hubConnected ? 'sr-ok' : 'sr-off'">
          <span class="live-dot"></span>
          {{ hubConnected ? 'SignalR kết nối' : 'Đang kết nối SignalR...' }}
        </div>

        <div class="d-flex gap-2 mt-3">
          <input class="form-control fs-sm" :value="shareLink" readonly style="background:var(--surface-2);" />
          <button class="btn btn-light" style="flex-shrink:0;" @click="copyLink"><Copy :size="13" /></button>
        </div>

        <div class="meta-row mt-3">
          <span><Calendar :size="12" /> {{ createdText }}</span>
          <span><Clock :size="12" /> {{ expireText }}</span>
        </div>
      </div>

      <div class="stat-grid mb-3">
        <div class="stat-card">
          <div class="stat-icon-box"><Users :size="16" /></div>
          <div class="stat-num">{{ totalVotes }}</div>
          <div class="stat-label">Tổng lượt vote</div>
        </div>
        <div class="stat-card" v-if="topText">
          <div class="stat-icon-box"><Trophy :size="16" /></div>
          <div class="stat-top">{{ topText }}</div>
          <div class="stat-label">Dẫn đầu</div>
        </div>
      </div>

      <div class="card mb-3">
        <div class="section-title">
          <BarChart2 :size="14" /> Kết quả
          <span class="live-badge ms-auto"><span class="live-dot"></span>Live</span>
        </div>

        <template v-if="['Multiple Choice', 'Yes / No'].includes(poll.questionType)">
          <div v-if="!totalVotes" class="empty-state">
            <p class="fs-sm">Chưa có lượt bình chọn.</p>
          </div>
          <div v-else class="bar-list">
            <div v-for="(opt, i) in choiceResults" :key="opt.id" class="bar-row" :class="{ win: i===0 && opt.count>0 }">
              <div class="bar-meta">
                <span class="bar-label">
                  <Trophy v-if="i===0 && opt.count>0" :size="12" class="trophy" /> {{ opt.text }}
                </span>
                <span class="bar-right"><strong>{{ opt.count }}</strong> <span class="bar-pct">{{ opt.pct }}%</span></span>
              </div>
              <div class="bar-track">
                <div class="bar-fill" :class="{ 'bar-win': i===0 && opt.count>0 }"
                  :style="{ width: opt.pct + '%', transitionDelay: i*50+'ms' }"></div>
              </div>
            </div>
          </div>
        </template>

        <template v-else-if="poll.questionType === 'Rating'">
          <div v-if="!totalVotes" class="empty-state"><p class="fs-sm">Chưa có lượt đánh giá.</p></div>
          <div v-else>
            <div class="rating-header mb-3">
              <div class="avg-score">{{ avgRating }}</div>
              <div>
                <div class="star-row mb-1">
                  <Star v-for="s in 5" :key="s" :size="20"
                    :fill="s <= Math.round(parseFloat(avgRating)) ? 'var(--amber)' : 'none'"
                    :color="s <= Math.round(parseFloat(avgRating)) ? 'var(--amber)' : 'var(--border-2)'" />
                </div>
                <p class="fs-xs text-3">{{ totalVotes }} lượt đánh giá</p>
              </div>
            </div>
            <div class="bar-list">
              <div v-for="s in [5,4,3,2,1]" :key="s" class="bar-row">
                <div class="bar-meta">
                  <span class="bar-label" style="gap:4px;">
                    <Star :size="13" fill="var(--amber)" color="var(--amber)" />
                    {{ s }} sao
                  </span>
                  <span class="bar-right">
                    <strong>{{ ratingBreakdown[s] }}</strong>
                    <span class="bar-pct">{{ totalVotes > 0 ? Math.round(ratingBreakdown[s]/totalVotes*100) : 0 }}%</span>
                  </span>
                </div>
                <div class="bar-track">
                  <div class="bar-fill"
                    :style="{ width: totalVotes > 0 ? (ratingBreakdown[s]/totalVotes*100)+'%' : '0%' }">
                  </div>
                </div>
              </div>
            </div>
          </div>
        </template>

        <template v-else-if="poll.questionType === 'Open Text'">
          <p class="fs-sm fw-600 mb-2">{{ textList.length }} phản hồi</p>
          <div v-if="!textList.length" class="empty-state"><p class="fs-sm">Chưa có phản hồi.</p></div>
          <div v-for="(t, i) in textList" :key="i" class="response-item">{{ t }}</div>
        </template>
      </div>

    </template>

    <!-- Modal dừng bình chọn -->
    <div v-if="showConfirm" class="modal-bg" @click.self="showConfirm=false">
      <div class="modal-box">
        <StopCircle :size="28" color="var(--red)" style="margin-bottom:12px;" />
        <h3 style="font-size:17px;font-weight:800;margin-bottom:8px;">Dừng bình chọn?</h3>
        <p class="fs-sm text-3 mb-3">Người dùng sẽ không thể vote thêm sau khi đóng.</p>
        <div class="d-flex gap-2 justify-center">
          <button class="btn btn-ghost" @click="showConfirm=false">Hủy</button>
          <button class="btn-close-poll" @click="closePoll"><StopCircle :size="14" /> Dừng ngay</button>
        </div>
      </div>
    </div>

    <!-- Modal xoá phòng -->
    <div v-if="showDeleteConfirm" class="modal-bg" @click.self="showDeleteConfirm=false">
      <div class="modal-box">
        <Trash2 :size="28" color="var(--red)" style="margin-bottom:12px;" />
        <h3 style="font-size:17px;font-weight:800;margin-bottom:8px;">Xoá phòng bình chọn?</h3>
        <p class="fs-sm text-3 mb-3">Toàn bộ dữ liệu phòng <strong>{{ poll?.code }}</strong> sẽ bị xoá vĩnh viễn và không thể khôi phục.</p>
        <div class="d-flex gap-2 justify-center">
          <button class="btn btn-ghost" @click="showDeleteConfirm=false">Hủy</button>
          <button class="btn-delete-poll" :disabled="deleting" @click="deletePoll">
            <span v-if="deleting" class="spinner"></span>
            <Trash2 v-else :size="14" />
            {{ deleting ? 'Đang xoá...' : 'Xoá vĩnh viễn' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { pollApi } from '../services/api';
import { useToastStore } from '../stores/toastStore';
import { usePollHub } from '../composables/usePollHub';
import {
  ChevronLeft, Lock, Copy, ExternalLink, StopCircle, Trash2,
  Calendar, Clock, Users, Trophy, Star, BarChart2,
} from 'lucide-vue-next';

const route  = useRoute();
const router = useRouter();
const toast  = useToastStore();
const code   = route.query.code;

const loading           = ref(false);
const denied            = ref(false);
const deniedCode        = ref('');
const poll              = ref(null);
const totalVotes        = ref(0);
const choiceResults     = ref([]);
const textList          = ref([]);
const ratingVotes       = ref([]);
const showConfirm       = ref(false);
const showDeleteConfirm = ref(false);
const deleting          = ref(false);

const isClosed  = computed(() => !poll.value || poll.value.status !== 'Active' || new Date(poll.value.expireAt) <= new Date());
const shareLink = computed(() => `${location.origin}/vote/${poll.value?.code}`);
const topText   = computed(() => choiceResults.value[0]?.count > 0 ? choiceResults.value[0].text : '');

const avgRating = computed(() => {
  const nums = ratingVotes.value.map(v => parseFloat(v.voteValue)).filter(n => n > 0);
  return nums.length ? (nums.reduce((a, b) => a + b, 0) / nums.length).toFixed(1) : '0.0';
});

const ratingBreakdown = computed(() => {
  const counts = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };
  ratingVotes.value.forEach(v => {
    const s = parseInt(v.voteValue);
    if (s >= 1 && s <= 5) counts[s]++;
  });
  return counts;
});

const createdText = computed(() => poll.value
  ? new Date(poll.value.createdAt).toLocaleDateString('vi-VN') : '');

const expireText = computed(() => {
  if (!poll.value) return '';
  const d = new Date(poll.value.expireAt);
  return d.getFullYear() > new Date().getFullYear() + 50 ? 'Không giới hạn'
    : d.toLocaleString('vi-VN', { day:'2-digit', month:'2-digit', year:'numeric', hour:'2-digit', minute:'2-digit' });
});

const { connected: hubConnected, start: hubStart } = usePollHub(code, async (data) => {
  totalVotes.value = data.total;

  if (poll.value && ['Multiple Choice', 'Yes / No'].includes(poll.value.questionType)) {
    buildBars(data.results, data.total);
  } else if (poll.value?.questionType === 'Rating') {
    const res = await pollApi.getVoteList(code);
    ratingVotes.value = res.data;
  } else if (poll.value?.questionType === 'Open Text') {
    const res = await pollApi.getVoteList(code);
    textList.value = res.data.map(v => v.voteValue).filter(Boolean);
  }
});

onMounted(async () => {
  if (!code) return;
  const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]');
  if (!saved.includes(code)) { denied.value = true; deniedCode.value = code; return; }

  loading.value = true;
  try {
    const [pollRes, totalRes] = await Promise.all([
      pollApi.getPollByCode(code),
      pollApi.getVoteTotal(code),
    ]);
    poll.value       = pollRes.data;
    totalVotes.value = totalRes.data.totalVotes;

    await loadResults();
    hubStart();
  } catch {
    toast.error('Không thể tải dữ liệu.');
  } finally {
    loading.value = false;
  }
});

const buildBars = (results, total) => {
  const counts = Object.fromEntries(results.map(r => [r.optionId, r.count]));
  choiceResults.value = poll.value.options
    .map(o => ({ id: o.id, text: o.text, count: counts[o.id] || 0,
      pct: total > 0 ? Math.round(((counts[o.id] || 0) / total) * 100) : 0 }))
    .sort((a, b) => b.count - a.count);
};

const loadResults = async () => {
  if (!poll.value) return;
  const totalRes = await pollApi.getVoteTotal(code);
  totalVotes.value = totalRes.data.totalVotes;

  const type = poll.value.questionType;
  if (['Multiple Choice', 'Yes / No'].includes(type)) {
    const res = await pollApi.getVoteResults(code);
    buildBars(res.data.map(r => ({ optionId: r.optionId, count: r.count })), totalVotes.value);
  } else {
    const res = await pollApi.getVoteList(code);
    if (type === 'Rating')        ratingVotes.value = res.data;
    else if (type === 'Open Text') textList.value = res.data.map(v => v.voteValue).filter(Boolean);
  }
};

const closePoll = async () => {
  showConfirm.value = false;
  try {
    await pollApi.updatePoll(poll.value.id, { ...poll.value, status: 'Closed' });
    poll.value.status = 'Closed';
    toast.success('Đã dừng bình chọn.');
  } catch {
    toast.error('Không thể đóng phòng.');
  }
};

const deletePoll = async () => {
  deleting.value = true;
  try {
    await pollApi.deletePoll(poll.value.id);
    const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]');
    localStorage.setItem('createdPolls', JSON.stringify(saved.filter(c => c !== code)));
    toast.success('Đã xoá phòng bình chọn.');
    router.push('/');
  } catch (e) {
    toast.error(e.message || 'Không thể xoá phòng.');
    showDeleteConfirm.value = false;
  } finally {
    deleting.value = false;
  }
};

const copyLink = async () => {
  try   { await navigator.clipboard.writeText(shareLink.value); toast.success('Đã sao chép!'); }
  catch { toast.error('Không thể sao chép.'); }
};
</script>

<style scoped>
.btn-close-poll {
  display: inline-flex; align-items: center; gap: 7px;
  padding: 9px 18px; font-size: 14px; font-weight: 600;
  background: var(--red); color: #fff; border: 1.5px solid var(--red);
  border-radius: var(--radius); cursor: pointer; transition: background .12s;
}
.btn-close-poll:hover { background: #b91c1c; }

.btn-delete-poll {
  display: inline-flex; align-items: center; gap: 7px;
  padding: 9px 18px; font-size: 14px; font-weight: 600;
  background: transparent; color: var(--red); border: 1.5px solid var(--red);
  border-radius: var(--radius); cursor: pointer; transition: all .12s;
}
.btn-delete-poll:hover:not(:disabled) { background: var(--red); color: #fff; }
.btn-delete-poll:disabled { opacity: .6; cursor: not-allowed; }

.sr-pill {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600;
}
.sr-ok  { background: var(--green-light); color: var(--green); }
.sr-off { background: var(--surface-3); color: var(--text-4); border: 1px solid var(--border); }

.meta-row {
  display: flex; gap: 16px; flex-wrap: wrap;
  padding-top: 10px; border-top: 1px solid var(--border);
  font-size: 13px; color: var(--text-3);
}
.meta-row span { display: flex; align-items: center; gap: 5px; }

.stat-card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); padding: 16px; text-align: center; }
.stat-icon-box { width: 34px; height: 34px; border-radius: 8px; background: var(--blue-light); color: var(--blue); display: flex; align-items: center; justify-content: center; margin: 0 auto 8px; }
.stat-top { font-size: 13px; font-weight: 700; color: var(--text-2); line-height: 1.3; margin-bottom: 2px; }

.bar-list { display: flex; flex-direction: column; gap: 12px; }
.bar-row.win .bar-label { color: var(--blue); font-weight: 700; }
.bar-meta { display: flex; align-items: center; justify-content: space-between; margin-bottom: 5px; }
.bar-label { font-size: 14px; font-weight: 600; color: var(--text); display: flex; align-items: center; gap: 5px; }
.trophy { color: var(--amber); }
.bar-right { font-size: 13px; color: var(--text-3); display: flex; gap: 5px; align-items: baseline; }
.bar-right strong { font-size: 15px; font-weight: 800; color: var(--text-2); }
.bar-pct { font-size: 12px; color: var(--text-4); }
.bar-track { height: 22px; background: var(--surface-3); border-radius: var(--radius-sm); overflow: hidden; border: 1px solid var(--border); }
.bar-fill { height: 100%; background: var(--blue); border-radius: var(--radius-sm); min-width: 2px; transition: width .55s cubic-bezier(.4,0,.2,1); }
.bar-fill.bar-win { background: var(--blue-dark); }

.rating-header { display: flex; align-items: center; gap: 16px; padding-bottom: 16px; border-bottom: 1px solid var(--border); }
.avg-score { font-size: 56px; font-weight: 800; color: var(--blue); line-height: 1; letter-spacing: -.04em; }
.star-row { display: flex; gap: 3px; justify-content: center; margin-top: 6px; }

.section-title { font-size: 14px; font-weight: 700; color: var(--text-2); display: flex; align-items: center; gap: 8px; padding-bottom: 12px; border-bottom: 1px solid var(--border); margin-bottom: 14px; }
.live-badge { display: inline-flex; align-items: center; gap: 5px; padding: 3px 9px; border-radius: 999px; background: var(--green-light); color: var(--green); font-size: 12px; font-weight: 700; }
.ms-auto { margin-left: auto; }

.live-dot { width: 7px; height: 7px; border-radius: 50%; background: currentColor; display: inline-block; animation: lp 1.8s ease infinite; }
@keyframes lp { 0%,100%{opacity:1}50%{opacity:.3} }

.modal-bg { position: fixed; inset: 0; background: rgba(15,23,42,.4); backdrop-filter: blur(4px); z-index: 200; display: flex; align-items: center; justify-content: center; padding: 20px; }
.modal-box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); padding: 28px; max-width: 360px; width: 100%; text-align: center; box-shadow: var(--shadow-md); }

.empty-state { text-align: center; padding: 28px; color: var(--text-4); }
</style>
