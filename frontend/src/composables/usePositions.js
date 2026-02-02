import { computed, ref } from 'vue';
import { getPositions, removePosition } from '../api';

export const usePositions = (positionDisplayLimit) => {
  const positions = ref([]);

  const displayedPositions = computed(() => {
    return positions.value.slice(0, positionDisplayLimit.value);
  });

  const loadPositions = async () => {
    const data = await getPositions();
    positions.value = data.map((position) => {
      const isRecovered = Boolean(position.recoveredAt || position.recoveryPrice);
      return {
        ...position,
        recoveryPrice: position.recoveryPrice ?? 'N/A',
        status: isRecovered ? 'recovered' : 'open',
        statusLabel: isRecovered ? 'Recovered' : 'Open'
      };
    });
  };

  const removePositionItem = async (positionId) => {
    await removePosition(positionId);
    await loadPositions();
  };

  return {
    positions,
    displayedPositions,
    loadPositions,
    removePositionItem
  };
};
