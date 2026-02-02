<template>
  <div class="page">
    <header class="header">
      <div>
        <h1>Price History</h1>
        <p class="muted">
          {{ itemLabel }}
        </p>
      </div>
      <router-link class="ghost" to="/">Back to dashboard</router-link>
    </header>

    <section class="card">
      <div class="toolbar">
        <div class="ranges">
          <button
            v-for="range in ranges"
            :key="range.value"
            class="range-button"
            :class="{ active: selectedRange === range.value }"
            @click="selectRange(range.value)"
          >
            {{ range.label }}
          </button>
        </div>
        <div class="meta muted" v-if="lastUpdated">
          Last updated: {{ lastUpdated }}
        </div>
      </div>

      <div v-if="error" class="error-banner">
        {{ error }}
      </div>

      <div v-if="loading" class="empty">Loading price history…</div>
      <div v-else-if="!points.length" class="empty">
        No price history available for this range yet.
      </div>
      <div v-else class="chart-wrap">
        <Line :data="chartData" :options="chartOptions" />
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { Line } from 'vue-chartjs';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';
import { getPriceHistory, getWatchlistItem } from '../api';
import { useRoute } from 'vue-router';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

const route = useRoute();
const itemId = computed(() => Number(route.params.itemId));
const itemLabel = ref('Item');
const points = ref([]);
const loading = ref(false);
const error = ref('');
const lastUpdated = ref('');
const selectedRange = ref('7d');

const ranges = [
  { label: '24h', value: '24h', hours: 24 },
  { label: '7d', value: '7d', hours: 24 * 7 },
  { label: '30d', value: '30d', hours: 24 * 30 },
  { label: '90d', value: '90d', hours: 24 * 90 }
];

const updateTimestamp = () => {
  lastUpdated.value = new Date().toLocaleTimeString();
};

const formatLabel = (timestamp) => {
  const date = new Date(timestamp);
  return date.toLocaleString();
};

const chartData = computed(() => ({
  labels: points.value.map((point) => formatLabel(point.timestamp)),
  datasets: [
    {
      label: 'Price (gp)',
      data: points.value.map((point) => point.price),
      borderColor: '#2563eb',
      backgroundColor: 'rgba(37, 99, 235, 0.2)',
      borderWidth: 2,
      pointRadius: 0,
      tension: 0.25
    }
  ]
}));

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false
    },
    tooltip: {
      callbacks: {
        label: (context) =>
          ` ${Number(context.parsed.y).toLocaleString()} gp`
      }
    }
  },
  scales: {
    x: {
      ticks: {
        maxTicksLimit: 10
      }
    },
    y: {
      ticks: {
        callback: (value) => Number(value).toLocaleString()
      }
    }
  }
}));

const selectRange = (value) => {
  selectedRange.value = value;
};

const resolveRange = () => {
  const range = ranges.find((entry) => entry.value === selectedRange.value) ?? ranges[1];
  const to = new Date();
  const from = new Date(to.getTime() - range.hours * 60 * 60 * 1000);
  return { from, to };
};

const loadItemLabel = async () => {
  try {
    const item = await getWatchlistItem(itemId.value);
    itemLabel.value = `${item.name} (#${item.id})`;
  } catch {
    itemLabel.value = `Item #${itemId.value}`;
  }
};

const loadHistory = async () => {
  if (!Number.isFinite(itemId.value)) {
    error.value = 'Invalid item id.';
    return;
  }
  loading.value = true;
  error.value = '';
  try {
    const { from, to } = resolveRange();
    points.value = await getPriceHistory(
      itemId.value,
      from.toISOString(),
      to.toISOString(),
      500
    );
    updateTimestamp();
  } catch (err) {
    error.value = err?.message || 'Failed to load price history.';
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  await loadItemLabel();
  await loadHistory();
});

watch([itemId, selectedRange], async () => {
  await loadItemLabel();
  await loadHistory();
});
</script>

<style scoped>
.page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 32px 20px 60px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.header {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
}

.header h1 {
  margin: 0 0 6px;
  font-size: 2rem;
}

.card {
  background: white;
  border-radius: 16px;
  padding: 20px 24px;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
}

.toolbar {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
  flex-wrap: wrap;
  margin-bottom: 16px;
}

.ranges {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.range-button {
  border: 1px solid #cbd5f5;
  border-radius: 999px;
  padding: 6px 12px;
  background: white;
  color: #0f172a;
  cursor: pointer;
}

.range-button.active {
  background: #2563eb;
  color: white;
  border-color: #2563eb;
}

.chart-wrap {
  height: 420px;
}

.muted {
  color: #64748b;
  font-size: 0.9rem;
}

.ghost {
  background: transparent;
  border: none;
  color: #2563eb;
  padding: 8px 12px;
  border-radius: 10px;
  cursor: pointer;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
}

.error-banner {
  background: #fee2e2;
  color: #991b1b;
  padding: 10px 14px;
  border-radius: 12px;
  font-size: 0.9rem;
  margin-bottom: 12px;
}

.empty {
  color: #94a3b8;
  font-style: italic;
  padding: 20px 0;
}

@media (max-width: 720px) {
  .header {
    flex-direction: column;
  }
}
</style>
