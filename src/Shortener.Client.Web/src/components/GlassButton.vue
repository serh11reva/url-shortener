<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'ghost';
    type?: 'button' | 'submit';
    disabled?: boolean;
  }>(),
  {
    variant: 'primary',
    type: 'button',
    disabled: false,
  },
);
</script>

<template>
  <button :type="type" class="glass-btn" :class="variant" :disabled="disabled">
    <span class="glass-btn-inner">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.glass-btn {
  appearance: none;
  font-family: inherit;
  font-weight: 800;
  font-size: 1rem;
  cursor: pointer;
  border: none;
  border-radius: var(--radius-control);
  padding: 0;
  transition: transform 0.12s ease, filter 0.12s ease, box-shadow 0.15s ease;
}

.glass-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
  transform: none;
}

.glass-btn:not(:disabled):active {
  transform: scale(0.98);
}

.glass-btn:focus-visible {
  outline: 3px solid var(--c-coral);
  outline-offset: 3px;
}

.glass-btn-inner {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 3rem;
  padding: 0 1.5rem;
  border-radius: var(--radius-control);
  width: 100%;
}

.glass-btn.primary .glass-btn-inner {
  color: #fff;
  background: linear-gradient(
    145deg,
    rgba(113, 47, 121, 0.92) 0%,
    rgba(76, 76, 157, 0.88) 100%
  );
  border: 1px solid rgba(255, 255, 255, 0.35);
  box-shadow:
    0 0.35rem 1.25rem rgba(76, 47, 96, 0.45),
    inset 0 1px 0 rgba(255, 255, 255, 0.35),
    inset 0 -1px 0 rgba(0, 0, 0, 0.15);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
}

.glass-btn.primary:not(:disabled):hover .glass-btn-inner {
  filter: brightness(1.08);
  box-shadow:
    0 0.5rem 1.75rem rgba(76, 47, 96, 0.5),
    inset 0 1px 0 rgba(255, 255, 255, 0.4),
    inset 0 -1px 0 rgba(0, 0, 0, 0.12);
}

.glass-btn.ghost .glass-btn-inner {
  color: var(--c-steel);
  background: var(--glass-bg-subtle);
  border: 1px solid var(--glass-border);
  box-shadow: var(--shadow-glass-inset);
  backdrop-filter: var(--glass-blur-soft);
  -webkit-backdrop-filter: var(--glass-blur-soft);
}

.glass-btn.ghost:not(:disabled):hover .glass-btn-inner {
  background: var(--glass-bg-hover);
  color: var(--c-berry);
  border-color: var(--glass-border-strong);
}
</style>
