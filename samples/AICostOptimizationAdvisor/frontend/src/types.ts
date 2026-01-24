export interface CostData {
  date: string;
  service: string;
  region: string;
  amount: number;
  currency: string;
  tags?: Record<string, string>;
  usageType: string;
  usageQuantity: string;
}

export interface GetCostDataResponse {
  costs: CostData[];
  totalCost: number;
  currency: string;
  dateRange: string;
}

export interface CostDriver {
  service: string;
  region: string;
  amount: number;
  percentage: number;
  description: string;
}

export interface CostAnomaly {
  date: string;
  service: string;
  expectedAmount: number;
  actualAmount: number;
  variance: number;
  description: string;
}

export interface OptimizationSuggestion {
  category: string;
  description: string;
  estimatedSavings: string;
  priority: 'High' | 'Medium' | 'Low';
  actions?: string[];
  service: string;
}

export interface CostAnalysis {
  analysisId: string;
  dateRange: string;
  createdAt: string;
  costDrivers: CostDriver[];
  anomalies: CostAnomaly[];
  suggestions: OptimizationSuggestion[];
  totalCost: number;
  summary: string;
}

export interface AnalyzeCostsRequest {
  costData: GetCostDataResponse;
  modelId?: string;
}

export interface AnalyzeCostsResponse {
  analysis: CostAnalysis;
  success: boolean;
  errorMessage?: string;
}
