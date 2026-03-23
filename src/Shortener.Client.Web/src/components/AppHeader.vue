<script setup lang="ts">
import { RouterLink } from 'vue-router';
import ClayPanel from '@/components/ClayPanel.vue';
</script>

<template>
  <header class="header">
    <ClayPanel class="header-panel" tag="div">
      <RouterLink to="/" class="brand">
        <span class="brand-mark" aria-hidden="true" />
        <span class="brand-text">Shortener</span>
      </RouterLink>
      <nav class="nav" aria-label="Main">
        <RouterLink to="/" class="nav-link">New link</RouterLink>
      </nav>
    </ClayPanel>
  </header>
</template>

<style scoped>
.header {
  width: 100%;
  max-width: 42rem;
  margin: 0 auto 1.25rem;
}

.header-panel {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.85rem 1.25rem;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
  text-decoration: none;
  color: inherit;
  font-family: var(--font-display);
  font-weight: 800;
  font-size: 1.35rem;
  letter-spacing: -0.02em;
}

.brand-mark {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.75rem;
  background: linear-gradient(145deg, var(--c-berry), var(--c-indigo));
  box-shadow: var(--shadow-clay-out);
  position: relative;
}

.brand-mark::after {
  content: '';
  position: absolute;
  inset: 22%;
  border-radius: 0.35rem;
  background: linear-gradient(135deg, var(--c-coral), #ffc49a);
  opacity: 0.95;
}

.brand-text {
  background: linear-gradient(90deg, var(--c-berry), var(--c-steel));
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}

.nav-link {
  text-decoration: none;
  font-weight: 700;
  font-size: 0.95rem;
  color: var(--c-steel);
  padding: 0.4rem 0.75rem;
  border-radius: var(--radius-control);
  transition: background 0.15s ease, color 0.15s ease;
}

.nav-link:hover {
  background: var(--c-clay-mid);
  color: var(--c-berry);
}

.nav-link.router-link-active {
  background: var(--c-clay-light);
  color: var(--c-berry);
}
</style>
