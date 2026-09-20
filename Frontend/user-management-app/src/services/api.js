import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5278';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

const getAuthHeader = () => {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
};

const setToken = (token) => {
  localStorage.setItem('jwt_token', token);
};

const getToken = () => {
  return localStorage.getItem('jwt_token');
};

const clearToken = () => {
  localStorage.removeItem('jwt_token');
};

const apiService = {
  setToken,
  getToken,
  clearToken,

  // Auth endpoints
  login: (username, password) => {
    return apiClient.post('/api/auth/login', { username, password });
  },

  // User endpoints
  getUsers: () => {
    return apiClient.get('/api/users', { headers: getAuthHeader() });
  },

  createUser: (username, password) => {
    return apiClient.post('/api/users', { username, password }, { headers: getAuthHeader() });
  },

  updateUser: (id, username, password) => {
    return apiClient.put(`/api/users/${id}`, { username, password }, { headers: getAuthHeader() });
  },

  deleteUser: (id) => {
    return apiClient.delete(`/api/users/${id}`, { headers: getAuthHeader() });
  },

  // Chat endpoints
  sendMessage: (message) => {
    return apiClient.post('/api/chat/send', { message }, { headers: getAuthHeader() });
  },

  searchLocalDatabase: (query) => {
    return apiClient.post('/api/chat/search-local', { query }, { headers: getAuthHeader() });
  },

  getChatHistory: () => {
    return apiClient.get('/api/chat/history', { headers: getAuthHeader() });
  },

  // Document endpoints
  uploadDocument: (formData) => {
    return apiClient.post('/api/document/upload', formData, {
      headers: {
        ...getAuthHeader(),
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  getMyDocuments: () => {
    return apiClient.get('/api/document/my-documents', { headers: getAuthHeader() });
  },

  deleteDocument: (id) => {
    return apiClient.delete(`/api/document/${id}`, { headers: getAuthHeader() });
  },

  searchDocuments: (query) => {
    return apiClient.post('/api/document/search', { query }, { headers: getAuthHeader() });
  },

  // Test endpoints
  runTests: (testName) => {
    return apiClient.post('/api/test/run', { testName }, { headers: getAuthHeader() });
  },

  getTestResults: () => {
    return apiClient.get('/api/test/results', { headers: getAuthHeader() });
  },
};

export default apiService;
