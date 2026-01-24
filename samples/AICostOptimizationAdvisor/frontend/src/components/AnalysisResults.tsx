import { CostAnalysis } from '../types';
import './AnalysisResults.css';

interface AnalysisResultsProps {
  analysis: CostAnalysis;
}

export default function AnalysisResults({ analysis }: AnalysisResultsProps) {
  return (
    <section className="analysis-results">
      <h2>Cost Analysis Results</h2>
      
      {analysis.summary && (
        <div className="summary">
          <h3>Summary</h3>
          <p>{analysis.summary}</p>
        </div>
      )}

      {analysis.costDrivers.length > 0 && (
        <div className="cost-drivers">
          <h3>Top Cost Drivers</h3>
          <table>
            <thead>
              <tr>
                <th>Service</th>
                <th>Region</th>
                <th>Amount</th>
                <th>Percentage</th>
                <th>Description</th>
              </tr>
            </thead>
            <tbody>
              {analysis.costDrivers.map((driver, index) => (
                <tr key={index}>
                  <td>{driver.service}</td>
                  <td>{driver.region}</td>
                  <td>${driver.amount.toFixed(2)}</td>
                  <td>{driver.percentage.toFixed(2)}%</td>
                  <td>{driver.description}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {analysis.anomalies.length > 0 && (
        <div className="anomalies">
          <h3>Cost Anomalies</h3>
          <div className="anomaly-list">
            {analysis.anomalies.map((anomaly, index) => (
              <div key={index} className="anomaly-item">
                <div className="anomaly-header">
                  <strong>{anomaly.service}</strong> - {anomaly.date}
                </div>
                <div className="anomaly-details">
                  <p>Expected: ${anomaly.expectedAmount.toFixed(2)}</p>
                  <p>Actual: ${anomaly.actualAmount.toFixed(2)}</p>
                  <p className="variance">Variance: ${anomaly.variance.toFixed(2)}</p>
                </div>
                <p className="anomaly-description">{anomaly.description}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}
