<script setup lang="ts">
import { ref } from 'vue';
import GlassField from '@/components/GlassField.vue';
import GlassButton from '@/components/GlassButton.vue';

withDefaults(
  defineProps<{
    disabled?: boolean;
  }>(),
  { disabled: false },
);

const longUrl = ref('');
const alias = ref('');
const aliasExpanded = ref(false);

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
          hint="Optional. Letters and numbers, up to 7 characters."
        >
          <input
            id="alias"
            v-model="alias"
            type="text"
            name="alias"
            maxlength="7"
            pattern="[a-zA-Z0-9]*"
            inputmode="text"
            autocomplete="off"
            :disabled="disabled"
            placeholder="Your alias"
          />
        </GlassField>
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

.submit {
  margin-top: 0.25rem;
  align-self: stretch;
}
</style>
