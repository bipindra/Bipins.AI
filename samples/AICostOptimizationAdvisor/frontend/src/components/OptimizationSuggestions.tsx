import { OptimizationSuggestion } from '../types';
import './OptimizationSuggestions.css';

interface OptimizationSuggestionsProps {
  suggestions: OptimizationSuggestion[];
}

export default function OptimizationSuggestions({ suggestions }: OptimizationSuggestionsProps) {
  const getPriorityClass = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high':
        return 'priority-high';
      case 'medium':
        return 'priority-medium';
      case 'low':
        return 'priority-low';
      default:
        return 'priority-medium';
    }
  };

  const groupedByCategory = suggestions.reduce((acc, suggestion) => {
    const category = suggestion.category || 'Other';
    if (!acc[category]) {
      acc[category] = [];
    }
    acc[category].push(suggestion);
    return acc;
  }, {} as Record<string, OptimizationSuggestion[]>);

  return (
    <section className="optimization-suggestions">
      <h2>Optimization Suggestions</h2>
      
      {Object.entries(groupedByCategory).map(([category, categorySuggestions]) => (
        <div key={category} className="category-group">
          <h3>{category}</h3>
          <div className="suggestions-list">
            {categorySuggestions.map((suggestion, index) => (
              <div key={index} className="suggestion-item">
                <div className="suggestion-header">
                  <span className={`priority-badge ${getPriorityClass(suggestion.priority)}`}>
                    {suggestion.priority}
                  </span>
                  {suggestion.service && (
                    <span className="service-badge">{suggestion.service}</span>
                  )}
                </div>
                <p className="suggestion-description">{suggestion.description}</p>
                {suggestion.estimatedSavings && (
                  <div className="savings">
                    <strong>Estimated Savings:</strong> {suggestion.estimatedSavings}
                  </div>
                )}
                {suggestion.actions && suggestion.actions.length > 0 && (
                  <div className="actions">
                    <strong>Recommended Actions:</strong>
                    <ul>
                      {suggestion.actions.map((action, actionIndex) => (
                        <li key={actionIndex}>{action}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      ))}
    </section>
  );
}
