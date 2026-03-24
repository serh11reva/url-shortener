<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';
import GlassField from '@/components/GlassField.vue';
import GlassButton from '@/components/GlassButton.vue';
import { ApiError, checkAliasAvailability } from '@/api/shortUrls';

const ALIAS_DEBOUNCE_MS = 400;

/** Matches server kebab rules (approximation for client-side gating before calling the API). */
const ALIAS_FORMAT = /^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$/;

const props = withDefaults(
  defineProps<{
    disabled?: boolean;
  }>(),
  { disabled: false },
);

const longUrl = ref('');
const alias = ref('');
const aliasExpanded = ref(false);

type AliasAvailabilityUi =
  | 'idle'
  | 'checking'
  | 'available'
  | 'taken'
  | 'invalid'
  | 'error'
  | 'invalid_server';

const aliasAvailability = ref<AliasAvailabilityUi>('idle');
const aliasServerMessage = ref<string | null>(null);

let debounceTimer: ReturnType<typeof setTimeout> | undefined;
let availabilityAbort: AbortController | undefined;

function clientAliasFormatOk(trimmed: string): boolean {
  return trimmed.length > 0 && trimmed.length <= 32 && ALIAS_FORMAT.test(trimmed);
}

function scheduleAliasAvailabilityCheck() {
  if (debounceTimer !== undefined) {
    clearTimeout(debounceTimer);
    debounceTimer = undefined;
  }
  availabilityAbort?.abort();

  if (props.disabled || !aliasExpanded.value) {
    aliasAvailability.value = 'idle';
    aliasServerMessage.value = null;
    return;
  }

  const trimmed = alias.value.trim();
  if (trimmed.length === 0) {
    aliasAvailability.value = 'idle';
    aliasServerMessage.value = null;
    return;
  }

  if (!clientAliasFormatOk(trimmed)) {
    aliasAvailability.value = 'invalid';
    aliasServerMessage.value = null;
    return;
  }

  aliasAvailability.value = 'checking';
  aliasServerMessage.value = null;

  debounceTimer = setTimeout(() => {
    debounceTimer = undefined;
    availabilityAbort = new AbortController();
    const signal = availabilityAbort.signal;

    void (async () => {
      const expectedTrimmed = trimmed;
      try {
        const result = await checkAliasAvailability(expectedTrimmed, signal);
        if (alias.value.trim() !== expectedTrimmed || props.disabled || !aliasExpanded.value) {
          return;
        }
        aliasAvailability.value = result.available ? 'available' : 'taken';
      } catch (e) {
        if (e instanceof Error && e.name === 'AbortError') {
          return;
        }
        if (e instanceof ApiError) {
          if (e.status === 400) {
            if (alias.value.trim() !== expectedTrimmed || props.disabled || !aliasExpanded.value) {
              return;
            }
            aliasAvailability.value = 'invalid_server';
            aliasServerMessage.value = e.message;
            return;
          }
        }
        if (alias.value.trim() !== expectedTrimmed || props.disabled || !aliasExpanded.value) {
          return;
        }
        aliasAvailability.value = 'error';
        aliasServerMessage.value = null;
      }
    })();
  }, ALIAS_DEBOUNCE_MS);
}

watch(
  () => [alias.value, aliasExpanded.value, props.disabled] as const,
  () => {
    scheduleAliasAvailabilityCheck();
  },
);

onBeforeUnmount(() => {
  if (debounceTimer !== undefined) {
    clearTimeout(debounceTimer);
  }
  availabilityAbort?.abort();
});

const emit = defineEmits<{
  submit: [payload: { longUrl: string; alias: string | undefined }];
}>();

function onSubmit() {
  emit('submit', {
    longUrl: longUrl.value.trim(),
    alias: alias.value.trim() || undefined,
  });
}

defineExpose({
  reset() {
    longUrl.value = '';
    alias.value = '';
    aliasExpanded.value = false;
    aliasAvailability.value = 'idle';
    aliasServerMessage.value = null;
    if (debounceTimer !== undefined) {
      clearTimeout(debounceTimer);
      debounceTimer = undefined;
    }
    availabilityAbort?.abort();
  },
});
</script>

<template>
  <form class="form" @submit.prevent="onSubmit">
    <GlassField
      id="long-url"
      label="Your URL"
    >
      <input
        id="long-url"
        v-model="longUrl"
        type="url"
        name="longUrl"
        required
        autocomplete="url"
        :disabled="disabled"
        placeholder="https://example.com/very/long/path"
      />
    </GlassField>

    <div class="advanced">
      <div class="advanced-row">
        <span class="advanced-label" id="advanced-heading">Advanced</span>
        <button
          type="button"
          class="expand-toggle"
          :aria-expanded="aliasExpanded"
          aria-controls="alias-advanced-region"
          :disabled="disabled"
          :aria-label="aliasExpanded ? 'Hide custom alias field' : 'Show custom alias field'"
          @click="aliasExpanded = !aliasExpanded"
        >
          <svg
            class="chevron"
            :class="{ open: aliasExpanded }"
            width="20"
            height="20"
            viewBox="0 0 20 20"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden="true"
          >
            <path
              d="M5 7.5L10 12.5L15 7.5"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </button>
      </div>
      <div
        v-show="aliasExpanded"
        id="alias-advanced-region"
        class="advanced-region"
        role="region"
        aria-labelledby="advanced-heading"
      >
        <GlassField
          id="alias"
          label="Custom alias"
          hint="Optional. Letters, numbers, and single hyphens between segments, up to 32 characters. Server validates the exact rules."
        >
          <input
            id="alias"
            v-model="alias"
            type="text"
            name="alias"
            maxlength="32"
            pattern="[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*"
            inputmode="text"
            autocomplete="off"
            :disabled="disabled"
            placeholder="Your alias"
            :aria-describedby="alias.trim() ? 'alias-availability-status' : undefined"
          />
        </GlassField>
        <p
          v-if="alias.trim().length > 0"
          id="alias-availability-status"
          class="alias-status"
          role="status"
          aria-live="polite"
        >
          <template v-if="aliasAvailability === 'checking'">Checking availability…</template>
          <template v-else-if="aliasAvailability === 'available'">This alias is available.</template>
          <template v-else-if="aliasAvailability === 'taken'">This alias is already taken.</template>
          <template v-else-if="aliasAvailability === 'invalid'">
            Use letters, numbers, and single hyphens between segments (for example <span class="nowrap">my-alias</span>).
          </template>
          <template v-else-if="aliasAvailability === 'invalid_server'">
            {{ aliasServerMessage ?? 'This alias cannot be used.' }}
          </template>
          <template v-else-if="aliasAvailability === 'error'">
            Could not check availability. You can still try to shorten; the server will validate.
          </template>
        </p>
      </div>
    </div>

    <GlassButton type="submit" class="submit" :disabled="disabled">Shorten</GlassButton>
  </form>
</template>

<style scoped>
.form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.advanced {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.advanced-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.advanced-label {
  font-weight: 800;
  font-size: 0.9rem;
  color: var(--c-text-muted);
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.expand-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  padding: 0;
  border: 1px solid var(--glass-border);
  border-radius: var(--radius-control);
  background: var(--glass-bg-subtle);
  color: var(--c-steel);
  cursor: pointer;
  box-shadow: var(--shadow-glass-inset);
  backdrop-filter: var(--glass-blur-soft);
  -webkit-backdrop-filter: var(--glass-blur-soft);
  transition:
    background 0.15s ease,
    color 0.15s ease,
    border-color 0.15s ease;
}

.expand-toggle:hover:not(:disabled) {
  background: var(--glass-bg-hover);
  color: var(--c-berry);
  border-color: var(--glass-border-strong);
}

.expand-toggle:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.expand-toggle:focus-visible {
  outline: 3px solid var(--c-coral);
  outline-offset: 2px;
}

.chevron {
  transition: transform 0.2s ease;
}

.chevron.open {
  transform: rotate(180deg);
}

.advanced-region {
  padding-top: 0.15rem;
}

.alias-status {
  margin: 0.5rem 0 0;
  font-size: 0.82rem;
  font-weight: 600;
  line-height: 1.35;
  color: var(--c-text-muted);
}

.submit {
  margin-top: 0.25rem;
  align-self: stretch;
}

.nowrap {
  white-space: nowrap;
}
</style>
