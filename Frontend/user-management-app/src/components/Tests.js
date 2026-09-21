import React, { useState } from 'react';
import apiService from '../services/api';
import './Tests.css';

const Tests = () => {
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const testSuites = [
    { name: 'E1', description: 'Verify JWT token is generated on login' },
    { name: 'E2', description: 'Verify JWT token is sent in Authorization header' },
    { name: 'E3', description: 'Verify endpoints without JWT token return 401' },
    { name: 'E4', description: 'Verify endpoints with invalid JWT token return 401' },
    { name: 'E5', description: 'Verify user can access their own resources' },
    { name: 'E6', description: 'Verify user cannot access other user resources' },
    { name: 'E7', description: 'Verify password is hashed before storage' },
  ];

  const runTest = async (testName) => {
    setLoading(true);
    setError('');

    try {
      const response = await apiService.runTests(testName);
      setResults((prev) => [
        ...prev,
        {
          id: Date.now(),
          name: testName,
          status: response.data.status,
          message: response.data.message,
          timestamp: new Date(),
        },
      ]);
    } catch (err) {
      setError(`Test ${testName} failed: ${err.response?.data?.message || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const runAllTests = async () => {
    setResults([]);
    setLoading(true);
    setError('');

    for (const suite of testSuites) {
      try {
        const response = await apiService.runTests(suite.name);
        setResults((prev) => [
          ...prev,
          {
            id: Date.now() + Math.random(),
            name: suite.name,
            status: response.data.status,
            message: response.data.message,
            timestamp: new Date(),
          },
        ]);
      } catch (err) {
        setResults((prev) => [
          ...prev,
          {
            id: Date.now() + Math.random(),
            name: suite.name,
            status: 'FAILED',
            message: err.response?.data?.message || err.message,
            timestamp: new Date(),
          },
        ]);
      }
    }

    setLoading(false);
  };

  const clearResults = () => {
    setResults([]);
    setError('');
  };

  const passedCount = results.filter((r) => r.status === 'PASSED').length;
  const failedCount = results.filter((r) => r.status === 'FAILED').length;

  return (
    <div className="tests-container">
      <div className="tests-panel">
        <h2>Section E: Authorization & Access Control Tests</h2>
        <p className="description">Run automated tests to verify JWT authentication and authorization.</p>

        <div className="test-buttons">
          <button onClick={runAllTests} disabled={loading} className="btn-run-all">
            {loading ? 'Running...' : '▶ Run All Tests'}
          </button>
          <button onClick={clearResults} disabled={loading} className="btn-clear">
            Clear Results
          </button>
        </div>

        <div className="individual-tests">
          <h3>Individual Tests</h3>
          <div className="test-list">
            {testSuites.map((suite) => (
              <div key={suite.name} className="test-item">
                <div className="test-info">
                  <strong>{suite.name}</strong>
                  <p>{suite.description}</p>
                </div>
                <button
                  onClick={() => runTest(suite.name)}
                  disabled={loading}
                  className="btn-run-single"
                >
                  Run
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="results-panel">
        <h2>Test Results</h2>

        {results.length === 0 ? (
          <p className="no-results">No test results yet. Click "Run All Tests" to start.</p>
        ) : (
          <>
            <div className="summary">
              <div className="summary-item">
                <span className="summary-label">Total:</span>
                <span className="summary-value">{results.length}</span>
              </div>
              <div className="summary-item passed">
                <span className="summary-label">Passed:</span>
                <span className="summary-value">{passedCount}</span>
              </div>
              <div className="summary-item failed">
                <span className="summary-label">Failed:</span>
                <span className="summary-value">{failedCount}</span>
              </div>
            </div>

            <div className="results-list">
              {results.map((result) => (
                <div key={result.id} className={`result-item ${result.status.toLowerCase()}`}>
                  <div className="result-header">
                    <span className="result-name">{result.name}</span>
                    <span className={`result-status ${result.status.toLowerCase()}`}>
                      {result.status}
                    </span>
                  </div>
                  <p className="result-message">{result.message}</p>
                  <p className="result-time">
                    {result.timestamp.toLocaleTimeString()}
                  </p>
                </div>
              ))}
            </div>
          </>
        )}

        {error && <div className="alert error">{error}</div>}
      </div>
    </div>
  );
};

export default Tests;
