<script setup lang="ts">
defineProps<{
  label: string;
  id: string;
  hint?: string;
}>();
</script>

<template>
  <div class="field">
    <label :for="id" class="label">{{ label }}</label>
    <div class="control">
      <slot />
    </div>
    <p v-if="hint" class="hint">{{ hint }}</p>
  </div>
</template>

<style scoped>
.field {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.label {
  font-weight: 800;
  font-size: 0.9rem;
  color: var(--c-text);
  letter-spacing: 0.02em;
}

.control {
  border-radius: var(--radius-control);
  background: var(--glass-bg-subtle);
  border: 1px solid var(--glass-border);
  box-shadow: var(--shadow-glass-inset);
  backdrop-filter: var(--glass-blur-soft);
  -webkit-backdrop-filter: var(--glass-blur-soft);
  padding: 0.15rem 0.25rem;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.control:focus-within {
  border-color: rgba(247, 153, 110, 0.65);
  box-shadow:
    var(--shadow-glass-inset),
    0 0 0 3px rgba(247, 153, 110, 0.28);
}

.control :deep(input) {
  width: 100%;
  border: none;
  background: transparent;
  font-family: inherit;
  font-size: 1rem;
  font-weight: 600;
  padding: 0.75rem 0.85rem;
  color: var(--c-text);
  border-radius: calc(var(--radius-control) - 4px);
}

.control :deep(input::placeholder) {
  color: var(--c-text-muted);
  font-weight: 500;
}

.control :deep(input:focus) {
  outline: none;
}

.hint {
  margin: 0;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--c-text-muted);
  line-height: 1.35;
}
</style>
