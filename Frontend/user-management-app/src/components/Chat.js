import React, { useState, useEffect, useRef } from 'react';
import apiService from '../services/api';
import './Chat.css';

const Chat = () => {
  const [messages, setMessages] = useState([]);
  const [inputMessage, setInputMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [pendingQuery, setPendingQuery] = useState('');
  const [localResults, setLocalResults] = useState(null);
  const messagesEndRef = useRef(null);

  useEffect(() => {
    loadChatHistory();
  }, []);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, localResults]);

  const loadChatHistory = async () => {
    try {
      const response = await apiService.getChatHistory();
      setMessages(response.data);
      setError('');
    } catch (err) {
      setError('Failed to load chat history');
    }
  };

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!inputMessage.trim()) return;

    const query = inputMessage.trim();
    setInputMessage('');
    setPendingQuery(query);
    setLoading(true);
    setError('');

    try {
      // Step 1: Search local database
      const response = await apiService.searchLocalDatabase(query);
      setLocalResults(response.data);

      // Add user message to chat
      setMessages((prev) => [
        ...prev,
        { id: Date.now(), role: 'user', content: query, createdAt: new Date() },
      ]);
    } catch (err) {
      setError('Error processing query. Please try again.');
      setLoading(false);
      setPendingQuery('');
    }
  };

  const handleAcceptLocalResults = () => {
    const resultContent = localResults.hasResults
      ? localResults.context
      : 'No relevant documents found.';

    setMessages((prev) => [
      ...prev,
      { id: Date.now(), role: 'assistant', content: resultContent, createdAt: new Date() },
    ]);

    setLocalResults(null);
    setPendingQuery('');
    setLoading(false);
  };

  const handleSearchWithOllama = async () => {
    try {
      const response = await apiService.sendMessage(pendingQuery);
      setMessages((prev) => [
        ...prev,
        { id: Date.now(), role: 'assistant', content: response.data.response, createdAt: new Date() },
      ]);
      setLocalResults(null);
      setPendingQuery('');
    } catch (err) {
      setError(err.response?.data?.message || 'Error getting AI response. Make sure Ollama is running.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="chat-container">
      <div className="chat-messages">
        {messages.map((msg) => (
          <div key={msg.id} className={`message ${msg.role}`}>
            <div className="message-role">{msg.role === 'user' ? 'You' : 'Assistant'}</div>
            <div className="message-content">{msg.content}</div>
          </div>
        ))}

        {localResults && (
          <div className="local-results">
            <h3>Local Database Results</h3>
            {localResults.hasResults ? (
              <>
                <div className="results-list">
                  <div className="result-item">
                    <p>{localResults.context}</p>
                  </div>
                </div>
                <div className="chat-actions">
                  <button className="btn-accept" onClick={handleAcceptLocalResults} disabled={loading}>
                    ✓ Accept Local Results
                  </button>
                  <button className="btn-ai-help" onClick={handleSearchWithOllama} disabled={loading}>
                    ? Need AI Help from Ollama
                  </button>
                </div>
              </>
            ) : (
              <>
                <p>No relevant documents found locally.</p>
                <div className="chat-actions">
                  <button className="btn-accept" onClick={handleAcceptLocalResults} disabled={loading}>
                    ✓ Accept
                  </button>
                  <button className="btn-ai-help" onClick={handleSearchWithOllama} disabled={loading}>
                    ? Ask Ollama
                  </button>
                </div>
              </>
            )}
          </div>
        )}

        {loading && !localResults && <div className="loading">Loading...</div>}
        {error && <div className="error-message">{error}</div>}
        <div ref={messagesEndRef} />
      </div>

      {pendingQuery ? null : (
        <form onSubmit={handleSendMessage} className="chat-form">
          <input
            type="text"
            value={inputMessage}
            onChange={(e) => setInputMessage(e.target.value)}
            placeholder="Ask a question or search documents..."
            disabled={loading || localResults !== null}
            autoFocus
          />
          <button type="submit" disabled={loading || localResults !== null || !inputMessage.trim()}>
            Send
          </button>
        </form>
      )}
    </div>
  );
};

export default Chat;
