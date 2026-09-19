import React, { useState, useEffect } from 'react';
import apiService from '../services/api';
import './DocumentUpload.css';

const DocumentUpload = () => {
  const [documents, setDocuments] = useState([]);
  const [dragOver, setDragOver] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    try {
      const response = await apiService.getMyDocuments();
      setDocuments(response.data);
      setError('');
    } catch (err) {
      setError('Failed to load documents');
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    setDragOver(true);
  };

  const handleDragLeave = () => {
    setDragOver(false);
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setDragOver(false);
    const files = e.dataTransfer.files;
    if (files.length) {
      handleFileUpload(files[0]);
    }
  };

  const handleFileSelect = (e) => {
    if (e.target.files.length) {
      handleFileUpload(e.target.files[0]);
    }
  };

  const handleFileUpload = async (file) => {
    if (file.type !== 'application/pdf') {
      setError('Only PDF files are allowed');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setError('File size must be less than 10MB');
      return;
    }

    setUploading(true);
    setError('');
    setSuccess('');

    const formData = new FormData();
    formData.append('file', file);

    try {
      await apiService.uploadDocument(formData);
      setSuccess('Document uploaded successfully');
      loadDocuments();
    } catch (err) {
      setError(err.response?.data?.message || 'Error uploading document');
    } finally {
      setUploading(false);
    }
  };

  const handleDeleteDocument = async (id) => {
    if (!window.confirm('Are you sure you want to delete this document?')) return;

    try {
      await apiService.deleteDocument(id);
      setSuccess('Document deleted successfully');
      loadDocuments();
      setError('');
    } catch (err) {
      setError(err.response?.data?.message || 'Error deleting document');
    }
  };

  return (
    <div className="document-container">
      <div className="upload-section">
        <h2>Upload Document</h2>
        <div
          className={`drop-zone ${dragOver ? 'drag-over' : ''}`}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
        >
          <svg className="upload-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
            <polyline points="17 8 12 3 7 8"></polyline>
            <line x1="12" y1="3" x2="12" y2="15"></line>
          </svg>
          <p>Drag and drop your PDF here or click to select</p>
          <input
            type="file"
            accept=".pdf"
            onChange={handleFileSelect}
            disabled={uploading}
            style={{ display: 'none' }}
            id="file-input"
          />
          <label htmlFor="file-input" className="file-label">
            Choose File
          </label>
          <small>Max 10MB</small>
        </div>

        {error && <div className="alert error">{error}</div>}
        {success && <div className="alert success">{success}</div>}
        {uploading && <div className="alert info">Uploading...</div>}
      </div>

      <div className="documents-section">
        <h2>My Documents ({documents.length})</h2>
        {documents.length === 0 ? (
          <p className="no-documents">No documents uploaded yet.</p>
        ) : (
          <div className="documents-grid">
            {documents.map((doc) => (
              <div key={doc.id} className="document-card">
                <div className="doc-icon">📄</div>
                <div className="doc-info">
                  <h3>{doc.fileName}</h3>
                  <p className="doc-type">{doc.fileType}</p>
                  <p className="doc-date">
                    {new Date(doc.uploadedAt).toLocaleDateString()}
                  </p>
                </div>
                <button
                  className="btn-delete-doc"
                  onClick={() => handleDeleteDocument(doc.id)}
                  disabled={uploading}
                >
                  Delete
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default DocumentUpload;
