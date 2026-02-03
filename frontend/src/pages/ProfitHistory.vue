<template>
  <div class="page">
    <header class="header">
      <div>
        <h1>Profit &amp; Loss</h1>
        <p class="muted">Track cumulative P&amp;L and item performance.</p>
      </div>
      <router-link class="ghost" to="/">Back to dashboard</router-link>
    </header>

    <section class="card">
      <div class="toolbar">
        <label class="inline-field">
          Item
          <select v-model.number="selectedItemId" @change="loadData">
            <option :value="0">All items</option>
            <option
              v-for="item in summaryItems"
              :key="item.itemId"
              :value="item.itemId"
            >
              {{ item.itemName }}
            </option>
          </select>
        </label>
        <div class="meta muted" v-if="lastUpdated">
          Last updated: {{ lastUpdated }}
        </div>
      </div>

      <div v-if="error" class="error-banner">
        {{ error }}
      </div>

      <div v-if="loading" class="empty">Loading profit history…</div>
      <div v-else-if="!history.length" class="empty">
        No sold positions yet. Sell a position to build your P&amp;L history.
      </div>
      <div v-else class="chart-wrap">
        <Line :data="historyChartData" :options="lineOptions" />
      </div>
    </section>

    <section class="card">
      <div class="card__header">
        <h2>Top Items</h2>
        <span class="muted">Ranked by total profit</span>
      </div>

      <div v-if="!summaryItems.length" class="empty">
        No sold items yet.
      </div>
      <div v-else class="chart-wrap">
        <Bar :data="itemChartData" :options="barOptions" />
      </div>
    </section>

    <section class="card">
      <div class="card__header">
        <h2>Item Rankings</h2>
        <span class="muted">Profit, average profit, and win rate</span>
      </div>
      <div v-if="!summaryItems.length" class="empty">
        No sold items yet.
      </div>
      <div v-else class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>Item</th>
              <th>Trades</th>
              <th>Total Profit</th>
              <th>Avg Profit</th>
              <th>Win Rate</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in rankedItems" :key="item.itemId">
              <td>{{ item.itemName }}</td>
              <td>{{ item.count }}</td>
              <td>{{ formatGp(item.totalProfit) }}</td>
              <td>{{ formatGp(item.averageProfit) }}</td>
              <td>{{ formatPercent(item.winRate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { Line, Bar } from 'vue-chartjs';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';
import { getPositionHistory, getPositionSummary } from '../api';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend
);

const history = ref([]);
const summaryItems = ref([]);
const selectedItemId = ref(0);
const loading = ref(false);
const error = ref('');
const lastUpdated = ref('');

const updateTimestamp = () => {
  lastUpdated.value = new Date().toLocaleTimeString();
};

const loadData = async () => {
  loading.value = true;
  error.value = '';
  try {
    const [summary, historyData] = await Promise.all([
      getPositionSummary(),
      getPositionHistory(selectedItemId.value || undefined)
    ]);
    history.value = historyData;
    summaryItems.value = summary.perItem ?? [];
    updateTimestamp();
  } catch (err) {
    error.value = err.message ?? 'Failed to load profit data.';
  } finally {
    loading.value = false;
  }
};

const historyChartData = computed(() => ({
  labels: history.value.map((point) => point.date),
  datasets: [
    {
      label: 'Total Profit (gp)',
      data: history.value.map((point) => point.totalProfit),
      borderColor: '#16a34a',
      backgroundColor: 'rgba(22, 163, 74, 0.2)',
      borderWidth: 2,
      pointRadius: 2,
      tension: 0.25
    }
  ]
}));

const topItems = computed(() => {
  return [...summaryItems.value]
    .sort((a, b) => b.totalProfit - a.totalProfit)
    .slice(0, 10);
});

const rankedItems = computed(() => {
  return [...summaryItems.value].sort((a, b) => b.totalProfit - a.totalProfit);
});

const itemChartData = computed(() => ({
  labels: topItems.value.map((item) => item.itemName),
  datasets: [
    {
      label: 'Total Profit (gp)',
      data: topItems.value.map((item) => item.totalProfit),
      backgroundColor: 'rgba(234, 179, 8, 0.65)',
      borderColor: '#a16207',
      borderWidth: 1
    }
  ]
}));

const lineOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  },
  scales: {
    y: {
      ticks: {
        callback: (value) => `${value} gp`
      }
    }
  }
};

const barOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  },
  scales: {
    x: {
      ticks: { autoSkip: false }
    },
    y: {
      ticks: {
        callback: (value) => `${value} gp`
      }
    }
  }
};

const formatGp = (value) => `${Math.round(value).toLocaleString()} gp`;
const formatPercent = (value) => `${(value * 100).toFixed(1)}%`;

onMounted(async () => {
  await loadData();
});
</script>
