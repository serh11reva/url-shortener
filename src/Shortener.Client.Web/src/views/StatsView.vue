<script setup lang="ts">
import { computed, onMounted, ref, shallowRef, watch } from 'vue';
import { RouterLink } from 'vue-router';
import ClayPanel from '@/components/ClayPanel.vue';
import { ApiError, getAnalytics } from '@/api/shortUrls';
import { getShortLinkOrigin } from '@/config/shortLinkOrigin';

const props = defineProps<{
  shortCode: string;
}>();

const loading = ref(true);
const errorMessage = shallowRef<string | null>(null);
const clickCount = shallowRef<number | null>(null);
const lastAccessed = shallowRef<string | null>(null);

const shortUrl = computed(() => {
  const origin = getShortLinkOrigin();
  const code = props.shortCode;
  return `${origin}/${encodeURIComponent(code)}`;
});

const lastAccessedLabel = computed(() => {
  if (!lastAccessed.value) {
    return 'Never';
  }
  const d = new Date(lastAccessed.value);
  if (Number.isNaN(d.getTime())) {
    return lastAccessed.value;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(d);
});

let controller: AbortController | null = null;

async function load() {
  controller?.abort();
  controller = new AbortController();
  loading.value = true;
  errorMessage.value = null;
  clickCount.value = null;
  lastAccessed.value = null;
  try {
    const data = await getAnalytics(props.shortCode, controller.signal);
    if (!data) {
      errorMessage.value = 'No short link found for this code.';
      return;
    }
    clickCount.value = data.clickCount;
    lastAccessed.value = data.lastAccessed;
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') {
      return;
    }
    if (e instanceof ApiError) {
      errorMessage.value = e.message;
    } else if (e instanceof Error) {
      errorMessage.value = e.message;
    } else {
      errorMessage.value = 'Could not load stats.';
    }
  } finally {
    loading.value = false;
  }
}

onMounted(load);

watch(
  () => props.shortCode,
  () => {
    void load();
  },
);
</script>

<template>
  <div class="stats">
    <ClayPanel class="panel">
      <RouterLink to="/" class="back">← Back</RouterLink>
      <h1 class="title">Link stats</h1>
      <p class="code">
        <a :href="shortUrl" target="_blank" rel="noopener noreferrer" class="short-url">{{ shortUrl }}</a>
      </p>

      <p v-if="loading" class="muted" role="status">Loading…</p>
      <p v-else-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
      <dl v-else class="metrics">
        <div class="metric">
          <dt>Clicks</dt>
          <dd>{{ clickCount }}</dd>
        </div>
        <div class="metric">
          <dt>Last accessed</dt>
          <dd>{{ lastAccessedLabel }}</dd>
        </div>
      </dl>
    </ClayPanel>
  </div>
</template>

<style scoped>
.stats {
  width: 100%;
  max-width: 42rem;
}

.panel {
  padding: 1.5rem 1.65rem 1.65rem;
}

.back {
  display: inline-block;
  margin-bottom: 0.75rem;
  font-weight: 800;
  font-size: 0.92rem;
  text-decoration: none;
  color: var(--c-steel);
}

.back:hover {
  color: var(--c-berry);
}

.title {
  margin: 0 0 0.5rem;
  font-size: 1.65rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.code {
  margin: 0 0 1.25rem;
  word-break: break-all;
  font-weight: 700;
}

.short-url {
  text-decoration: none;
  color: var(--c-berry);
}

.short-url:hover {
  text-decoration: underline;
}

.muted {
  margin: 0;
  font-weight: 700;
  color: var(--c-text-muted);
}

.error {
  margin: 0;
  font-weight: 700;
  color: #9c2d4a;
}

.metrics {
  margin: 0;
  display: grid;
  gap: 1rem;
}

.metric {
  padding: 1rem 1.1rem;
  border-radius: var(--radius-control);
  background: rgba(255, 255, 255, 0.38);
  box-shadow: var(--shadow-clay-in);
  border: 1px solid rgba(255, 255, 255, 0.5);
}

.metric dt {
  margin: 0 0 0.35rem;
  font-size: 0.82rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--c-text-muted);
}

.metric dd {
  margin: 0;
  font-size: 1.35rem;
  font-weight: 800;
  color: var(--c-indigo);
}
</style>
