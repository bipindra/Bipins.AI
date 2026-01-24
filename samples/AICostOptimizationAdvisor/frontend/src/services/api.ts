import axios from 'axios';
import { GetCostDataResponse, AnalyzeCostsRequest, AnalyzeCostsResponse, CostAnalysis } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://your-api-gateway-url.execute-api.region.amazonaws.com/Prod';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const getCostData = async (startDate: string, endDate: string, granularity: string = 'DAILY'): Promise<GetCostDataResponse> => {
  const response = await api.get('/costs', {
    params: { startDate, endDate, granularity },
  });
  return response.data;
};

export const analyzeCosts = async (costData: GetCostDataResponse, modelId?: string): Promise<AnalyzeCostsResponse> => {
  const request: AnalyzeCostsRequest = { costData, modelId };
  const response = await api.post('/analyze', request);
  return response.data;
};

export const getAnalysisHistory = async (limit: number = 10): Promise<{ analyses: CostAnalysis[] }> => {
  const response = await api.get('/history', {
    params: { limit },
  });
  return response.data;
};
