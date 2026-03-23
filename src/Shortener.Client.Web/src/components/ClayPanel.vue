<script setup lang="ts">
withDefaults(
  defineProps<{
    tag?: string;
  }>(),
  { tag: 'section' },
);
</script>

<template>
  <component :is="tag" class="clay-panel">
    <slot />
  </component>
</template>

<style scoped>
.clay-panel {
  width: 100%;
  border-radius: var(--radius-clay);
  background: linear-gradient(
    145deg,
    rgba(255, 255, 255, 0.52) 0%,
    rgba(255, 255, 255, 0.22) 100%
  );
  border: 1px solid rgba(255, 255, 255, 0.55);
  box-shadow: var(--shadow-clay-out), var(--shadow-clay-in);
  backdrop-filter: blur(10px);
}
</style>
