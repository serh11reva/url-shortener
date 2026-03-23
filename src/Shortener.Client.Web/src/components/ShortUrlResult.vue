<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import ClayPanel from '@/components/ClayPanel.vue';
import ClayButton from '@/components/ClayButton.vue';

const props = defineProps<{
  shortUrl: string;
  shortCode: string;
}>();

const copied = ref(false);
let copyTimer: ReturnType<typeof setTimeout> | null = null;

const statsTo = computed(() => ({ name: 'stats' as const, params: { shortCode: props.shortCode } }));

async function copyLink() {
  try {
    await navigator.clipboard.writeText(props.shortUrl);
    copied.value = true;
    if (copyTimer) {
      clearTimeout(copyTimer);
    }
    copyTimer = setTimeout(() => {
      copied.value = false;
      copyTimer = null;
    }, 2000);
  } catch {
    /* clipboard may be denied; ignore */
  }
}
</script>

<template>
  <ClayPanel class="result" aria-live="polite">
    <h2 class="title">Your short link</h2>
    <p class="url-line">
      <a :href="shortUrl" class="short-url" target="_blank" rel="noopener noreferrer">{{ shortUrl }}</a>
    </p>
    <div class="actions">
      <ClayButton type="button" @click="copyLink">{{ copied ? 'Copied!' : 'Copy' }}</ClayButton>
      <RouterLink :to="statsTo" class="stats-link">View stats</RouterLink>
    </div>
  </ClayPanel>
</template>

<style scoped>
.result {
  padding: 1.35rem 1.5rem;
  margin-top: 1.5rem;
}

.title {
  margin: 0 0 0.75rem;
  font-size: 1.05rem;
  font-weight: 800;
  color: var(--c-indigo);
}

.url-line {
  margin: 0 0 1.1rem;
  word-break: break-all;
}

.short-url {
  font-size: 1.05rem;
  font-weight: 800;
  text-decoration: none;
  color: var(--c-berry);
}

.short-url:hover {
  text-decoration: underline;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem 1rem;
}

.stats-link {
  font-weight: 800;
  font-size: 0.98rem;
  text-decoration: none;
  padding: 0.5rem 0.85rem;
  border-radius: var(--radius-control);
  background: rgba(247, 153, 110, 0.28);
  color: var(--c-text);
  border: 1px solid rgba(255, 255, 255, 0.45);
  box-shadow: 0 2px 0 rgba(113, 47, 121, 0.12);
  transition: background 0.15s ease, transform 0.12s ease;
}

.stats-link:hover {
  background: rgba(247, 153, 110, 0.42);
}

.stats-link:active {
  transform: scale(0.98);
}
</style>
