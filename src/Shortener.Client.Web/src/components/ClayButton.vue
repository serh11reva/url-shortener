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
  <button :type="type" class="clay-btn" :class="variant" :disabled="disabled">
    <span class="clay-btn-inner">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.clay-btn {
  appearance: none;
  font-family: inherit;
  font-weight: 800;
  font-size: 1rem;
  cursor: pointer;
  border: none;
  border-radius: var(--radius-control);
  padding: 0;
  transition: transform 0.12s ease, filter 0.12s ease;
}

.clay-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
  transform: none;
}

.clay-btn:not(:disabled):active {
  transform: scale(0.98);
}

.clay-btn:focus-visible {
  outline: 3px solid var(--c-coral);
  outline-offset: 3px;
}

.clay-btn-inner {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 3rem;
  padding: 0 1.5rem;
  border-radius: var(--radius-control);
  width: 100%;
}

.clay-btn.primary .clay-btn-inner {
  color: #fff;
  background: linear-gradient(160deg, var(--c-berry) 0%, var(--c-indigo) 100%);
  box-shadow:
    0.3rem 0.3rem 0.55rem rgba(55, 32, 72, 0.35),
    inset 0 1px 0 rgba(255, 255, 255, 0.25),
    inset 0 -2px 0 rgba(0, 0, 0, 0.12);
}

.clay-btn.primary:not(:disabled):hover .clay-btn-inner {
  filter: brightness(1.06);
}

.clay-btn.ghost .clay-btn-inner {
  color: var(--c-steel);
  background: rgba(255, 255, 255, 0.35);
  box-shadow: var(--shadow-clay-in);
  border: 1px solid rgba(255, 255, 255, 0.5);
}

.clay-btn.ghost:not(:disabled):hover .clay-btn-inner {
  background: rgba(255, 255, 255, 0.5);
  color: var(--c-berry);
}
</style>
