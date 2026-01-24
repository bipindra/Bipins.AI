import { useState } from 'react';
import { getCostData, analyzeCosts, getAnalysisHistory } from './services/api';
import { GetCostDataResponse, AnalyzeCostsResponse, CostAnalysis } from './types';
import CostChart from './components/CostChart';
import AnalysisResults from './components/AnalysisResults';
import OptimizationSuggestions from './components/OptimizationSuggestions';
import './App.css';

function App() {
  const [startDate, setStartDate] = useState(() => {
    const date = new Date();
    date.setDate(date.getDate() - 30);
    return date.toISOString().split('T')[0];
  });
  const [endDate, setEndDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [costData, setCostData] = useState<GetCostDataResponse | null>(null);
  const [analysis, setAnalysis] = useState<CostAnalysis | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [history, setHistory] = useState<CostAnalysis[]>([]);

  const handleFetchCosts = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCostData(startDate, endDate);
      setCostData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch cost data');
    } finally {
      setLoading(false);
    }
  };

  const handleAnalyze = async () => {
    if (!costData) {
      setError('Please fetch cost data first');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response: AnalyzeCostsResponse = await analyzeCosts(costData);
      if (response.success) {
        setAnalysis(response.analysis);
      } else {
        setError(response.errorMessage || 'Analysis failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze costs');
    } finally {
      setLoading(false);
    }
  };

  const handleLoadHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getAnalysisHistory(10);
      setHistory(response.analyses);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>AI Cost Optimization Advisor</h1>
        <p>Analyze AWS costs and get AI-powered optimization suggestions</p>
      </header>

      <main className="app-main">
        <section className="controls">
          <div className="date-range">
            <label>
              Start Date:
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
            </label>
            <label>
              End Date:
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </label>
            <button onClick={handleFetchCosts} disabled={loading}>
              {loading ? 'Loading...' : 'Fetch Cost Data'}
            </button>
            <button onClick={handleAnalyze} disabled={loading || !costData}>
              Analyze Costs
            </button>
            <button onClick={handleLoadHistory} disabled={loading}>
              Load History
            </button>
          </div>
        </section>

        {error && <div className="error">{error}</div>}

        {costData && (
          <section className="cost-section">
            <h2>Cost Overview</h2>
            <div className="cost-summary">
              <p>Total Cost: <strong>${costData.totalCost.toFixed(2)}</strong></p>
              <p>Date Range: {costData.dateRange}</p>
            </div>
            <CostChart costData={costData} />
          </section>
        )}

        {analysis && (
          <>
            <AnalysisResults analysis={analysis} />
            <OptimizationSuggestions suggestions={analysis.suggestions} />
          </>
        )}

        {history.length > 0 && (
          <section className="history-section">
            <h2>Analysis History</h2>
            {history.map((item) => (
              <div key={item.analysisId} className="history-item">
                <p><strong>Date Range:</strong> {item.dateRange}</p>
                <p><strong>Created:</strong> {new Date(item.createdAt).toLocaleString()}</p>
                <p><strong>Total Cost:</strong> ${item.totalCost.toFixed(2)}</p>
                <p><strong>Summary:</strong> {item.summary}</p>
              </div>
            ))}
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
