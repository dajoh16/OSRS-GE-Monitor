import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import App from './App.vue';
import HomePage from './pages/HomePage.vue';
import PriceHistory from './pages/PriceHistory.vue';
import './style.css';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomePage },
    { path: '/history/:itemId', component: PriceHistory }
  ]
});

createApp(App).use(router).mount('#app');
