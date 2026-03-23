import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '@/views/HomeView.vue';
import StatsView from '@/views/StatsView.vue';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/stats/:shortCode', name: 'stats', component: StatsView, props: true },
  ],
  scrollBehavior() {
    return { top: 0 };
  },
});
