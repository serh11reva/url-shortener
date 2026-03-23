<script setup lang="ts">
import { ref, shallowRef } from 'vue';
import ClayPanel from '@/components/ClayPanel.vue';
import UrlShortenForm from '@/components/UrlShortenForm.vue';
import ShortUrlResult from '@/components/ShortUrlResult.vue';
import { ApiError, createShortUrl } from '@/api/shortUrls';
import { getShortLinkOrigin } from '@/config/shortLinkOrigin';

const formRef = ref<InstanceType<typeof UrlShortenForm> | null>(null);
const shortCode = shallowRef<string | null>(null);
const errorMessage = shallowRef<string | null>(null);
const submitting = ref(false);
const controller = shallowRef<AbortController | null>(null);

const shortUrl = ref('');

function buildShortUrl(code: string) {
  const origin = getShortLinkOrigin();
  return `${origin}/${encodeURIComponent(code)}`;
}

async function onSubmit(payload: { longUrl: string; alias: string | undefined }) {
  errorMessage.value = null;
  shortCode.value = null;
  controller.value?.abort();
  const ac = new AbortController();
  controller.value = ac;
  submitting.value = true;
  try {
    const result = await createShortUrl(payload.longUrl, payload.alias, ac.signal);
    shortCode.value = result.shortCode;
    shortUrl.value = buildShortUrl(result.shortCode);
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') {
      return;
    }
    if (e instanceof ApiError) {
      errorMessage.value = e.message;
    } else if (e instanceof Error) {
      errorMessage.value = e.message;
    } else {
      errorMessage.value = 'Something went wrong.';
    }
  } finally {
    submitting.value = false;
  }
}

function resetFlow() {
  shortCode.value = null;
  errorMessage.value = null;
  formRef.value?.reset();
}
</script>

<template>
  <div class="home">
    <ClayPanel class="hero">
      <p class="eyebrow">Clay-soft & fast</p>
      <h1 class="headline">Shrink your links</h1>
      <p class="lede">Paste a long URL, add an optional alias, and share a short link that redirects in a flash.</p>
    </ClayPanel>

    <ClayPanel class="card">
      <UrlShortenForm
        v-if="!shortCode"
        ref="formRef"
        :disabled="submitting"
        @submit="onSubmit"
      />
      <template v-else>
        <ShortUrlResult :short-url="shortUrl" :short-code="shortCode" />
        <button type="button" class="text-action" @click="resetFlow">Shorten another</button>
      </template>

      <p v-if="submitting" class="status" role="status">Working…</p>
      <p v-else-if="errorMessage" class="error" role="alert">{{ errorMessage }}</p>
    </ClayPanel>
  </div>
</template>

<style scoped>
.home {
  width: 100%;
  max-width: 42rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.hero {
  padding: 1.5rem 1.65rem 1.35rem;
}

.eyebrow {
  margin: 0 0 0.35rem;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.14em;
  color: var(--c-steel);
}

.headline {
  margin: 0 0 0.5rem;
  font-size: clamp(1.65rem, 5vw, 2.15rem);
  font-weight: 800;
  font-family: var(--font-display);
  letter-spacing: -0.03em;
  color: var(--c-text);
}

.lede {
  margin: 0;
  font-size: 1.02rem;
  line-height: 1.5;
  font-weight: 600;
  color: var(--c-text-muted);
}

.card {
  padding: 1.65rem 1.5rem 1.5rem;
}

.status {
  margin: 1rem 0 0;
  font-weight: 700;
  color: var(--c-steel);
}

.error {
  margin: 1rem 0 0;
  font-weight: 700;
  color: #9c2d4a;
}

.text-action {
  margin-top: 1.25rem;
  align-self: flex-start;
  border: none;
  background: none;
  font-family: inherit;
  font-weight: 800;
  font-size: 0.95rem;
  color: var(--c-steel);
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 0.2em;
}

.text-action:focus-visible {
  outline: 3px solid var(--c-coral);
  outline-offset: 3px;
  border-radius: 0.25rem;
}
</style>
