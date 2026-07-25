<template>
  <div class="toast-wrap" aria-live="polite">
    <div
      v-for="t in toasts"
      :key="t.id"
      class="toast-item"
      :class="[t.type, { leaving: t.leaving }]"
      @click="dismiss(t.id)"
    >
      <!-- icon -->
      <span class="toast-icon">
        <CheckCircle2 v-if="t.type === 'success'" :size="16" />
        <XCircle      v-else-if="t.type === 'error'"   :size="16" />
        <AlertTriangle v-else-if="t.type === 'warning'" :size="16" />
        <Info          v-else                            :size="16" />
      </span>

      <span class="toast-msg">{{ t.message }}</span>

      <button class="toast-close" @click.stop="dismiss(t.id)" aria-label="Đóng">
        <X :size="13" />
      </button>

      <!-- progress bar -->
      <div class="toast-progress" :class="t.type"></div>
    </div>
  </div>
</template>

<script setup>
import { useToastStore } from '../stores/toastStore';
import { CheckCircle2, XCircle, AlertTriangle, Info, X } from 'lucide-vue-next';

const store = useToastStore();
const { toasts, dismiss } = store;
</script>

<style scoped>
.toast-wrap {
  position: fixed; bottom: 24px; right: 20px;
  z-index: 9999; display: flex; flex-direction: column;
  gap: 8px; pointer-events: none;
}

.toast-item {
  pointer-events: all;
  display: flex; align-items: center; gap: 10px;
  padding: 11px 14px; min-width: 240px; max-width: 340px;
  background: var(--surface); border-radius: var(--radius);
  border: 1px solid var(--border);
  box-shadow: 0 4px 16px rgba(0,0,0,.10), 0 1px 4px rgba(0,0,0,.06);
  cursor: pointer; overflow: hidden; position: relative;

  /* enter */
  animation: toastIn .22s cubic-bezier(.34,1.56,.64,1) both;
}

.toast-item.leaving {
  animation: toastOut .26s ease forwards;
}

/* colored left border */
.toast-item.success { border-left: 3px solid var(--green); }
.toast-item.error   { border-left: 3px solid var(--red);   }
.toast-item.warning { border-left: 3px solid var(--amber); }
.toast-item.info    { border-left: 3px solid var(--blue);  }

.toast-icon { flex-shrink: 0; display: flex; }
.toast-item.success .toast-icon { color: var(--green); }
.toast-item.error   .toast-icon { color: var(--red);   }
.toast-item.warning .toast-icon { color: var(--amber); }
.toast-item.info    .toast-icon { color: var(--blue);  }

.toast-msg  {
  flex: 1; font-size: 13.5px; font-weight: 600; color: var(--text);
  line-height: 1.4;
}

.toast-close {
  flex-shrink: 0; background: none; border: none; cursor: pointer;
  color: var(--text-4); padding: 2px; border-radius: 4px;
  display: flex; align-items: center;
  transition: color .12s, background .12s;
}
.toast-close:hover { color: var(--text); background: var(--surface-3); }

/* shrink progress bar over ~3 s */
.toast-progress {
  position: absolute; bottom: 0; left: 0; right: 0; height: 3px;
  transform-origin: left;
  animation: progress 3s linear forwards;
}
.toast-progress.success { background: var(--green); }
.toast-progress.error   { background: var(--red); animation-duration: 4s; }
.toast-progress.warning { background: var(--amber); }
.toast-progress.info    { background: var(--blue); }

@keyframes toastIn {
  from { transform: translateX(40px) scale(.95); opacity: 0; }
  to   { transform: none; opacity: 1; }
}

@keyframes toastOut {
  from { transform: none; opacity: 1; max-height: 80px; margin-bottom: 0; }
  to   { transform: translateX(40px); opacity: 0; max-height: 0; margin-bottom: -8px; padding: 0; }
}

@keyframes progress {
  from { transform: scaleX(1); }
  to   { transform: scaleX(0); }
}
</style>
