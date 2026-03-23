<script setup lang="ts">
withDefaults(
  defineProps<{
    tag?: string;
  }>(),
  { tag: 'section' },
);
</script>

<template>
  <component :is="tag" class="glass-panel">
    <slot />
  </component>
</template>

<style scoped>
.glass-panel {
  width: 100%;
  border-radius: var(--radius-glass);
  background: var(--glass-bg);
  border: 1px solid var(--glass-border);
  box-shadow: var(--shadow-glass-elevated);
  backdrop-filter: var(--glass-blur);
  -webkit-backdrop-filter: var(--glass-blur);
}
</style>
