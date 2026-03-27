import api from './api';
import { PAGINATION } from '../config';

export const outageService = {
  getOutages: async ({ page = 1, limit = PAGINATION.DEFAULT_LIMIT, filters = {} } = {}) => {
    const params = { page, limit, sortBy: filters.sortBy, sortDir: filters.sortDir };
    if (filters.dateFrom) params.dateFrom = filters.dateFrom;
    if (filters.dateTo) params.dateTo = filters.dateTo;
    if (filters.minOutage) params.minOutage = filters.minOutage;
    if (filters.maxOutage) params.maxOutage = filters.maxOutage;

    const response = await api.get('/data', { params });
    return { data: response.data.data, total: response.data.total };
  },

  refreshPipeline: async () => {
    const response = await api.post('/refresh?force=true');
    return response.data.message;
  },
};