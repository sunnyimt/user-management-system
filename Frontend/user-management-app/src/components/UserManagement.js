import React, { useState, useEffect } from 'react';
import apiService from '../services/api';
import Chat from './Chat';
import DocumentUpload from './DocumentUpload';
import Tests from './Tests';
import './UserManagement.css';

const UserManagement = ({ onLogout }) => {
  const [users, setUsers] = useState([]);
  const [activeTab, setActiveTab] = useState('users');
  const [newUsername, setNewUsername] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [editUsername, setEditUsername] = useState('');
  const [editPassword, setEditPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      const response = await apiService.getUsers();
      setUsers(response.data);
      setError('');
    } catch (err) {
      setError('Failed to load users');
    }
  };

  const handleCreateUser = async (e) => {
    e.preventDefault();
    if (!newUsername || !newPassword) {
      setError('Username and password are required');
      return;
    }

    setLoading(true);
    try {
      await apiService.createUser(newUsername, newPassword);
      setSuccess('User created successfully');
      setNewUsername('');
      setNewPassword('');
      loadUsers();
      setError('');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create user');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateUser = async (e) => {
    e.preventDefault();
    if (!editUsername || !editPassword) {
      setError('Username and password are required');
      return;
    }

    setLoading(true);
    try {
      await apiService.updateUser(editingId, editUsername, editPassword);
      setSuccess('User updated successfully');
      setEditingId(null);
      setEditUsername('');
      setEditPassword('');
      loadUsers();
      setError('');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update user');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteUser = async (id) => {
    if (!window.confirm('Are you sure you want to delete this user?')) return;

    try {
      await apiService.deleteUser(id);
      setSuccess('User deleted successfully');
      loadUsers();
      setError('');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to delete user');
    }
  };

  const startEdit = (user) => {
    setEditingId(user.id);
    setEditUsername(user.username);
    setEditPassword('');
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditUsername('');
    setEditPassword('');
  };

  return (
    <div className="container">
      <header className="header">
        <h1>User Management System</h1>
        <button onClick={onLogout} className="logout-btn">
          Logout
        </button>
      </header>

      <div className="tabs">
        <button
          className={activeTab === 'users' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('users')}
        >
          User Management
        </button>
        <button
          className={activeTab === 'chat' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('chat')}
        >
          Chat
        </button>
        <button
          className={activeTab === 'documents' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('documents')}
        >
          Documents
        </button>
        <button
          className={activeTab === 'tests' ? 'tab active' : 'tab'}
          onClick={() => setActiveTab('tests')}
        >
          Tests
        </button>
      </div>

      <div className="content">
        {error && <div className="alert error">{error}</div>}
        {success && <div className="alert success">{success}</div>}

        {activeTab === 'users' && (
          <div className="users-section">
            <div className="form-section">
              <h2>{editingId ? 'Edit User' : 'Create New User'}</h2>
              <form onSubmit={editingId ? handleUpdateUser : handleCreateUser}>
                <input
                  type="text"
                  placeholder="Username"
                  value={editingId ? editUsername : newUsername}
                  onChange={(e) =>
                    editingId ? setEditUsername(e.target.value) : setNewUsername(e.target.value)
                  }
                  disabled={loading}
                  required
                />
                <input
                  type="password"
                  placeholder="Password"
                  value={editingId ? editPassword : newPassword}
                  onChange={(e) =>
                    editingId ? setEditPassword(e.target.value) : setNewPassword(e.target.value)
                  }
                  disabled={loading}
                  required
                />
                <button type="submit" disabled={loading}>
                  {loading ? 'Processing...' : editingId ? 'Update User' : 'Create User'}
                </button>
                {editingId && (
                  <button type="button" onClick={cancelEdit} disabled={loading}>
                    Cancel
                  </button>
                )}
              </form>
            </div>

            <div className="users-list-section">
              <h2>Users</h2>
              <table>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Username</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td>{user.id}</td>
                      <td>{user.username}</td>
                      <td>
                        <button
                          className="btn-edit"
                          onClick={() => startEdit(user)}
                          disabled={editingId !== null}
                        >
                          Edit
                        </button>
                        <button
                          className="btn-delete"
                          onClick={() => handleDeleteUser(user.id)}
                          disabled={editingId !== null}
                        >
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'chat' && <Chat />}
        {activeTab === 'documents' && <DocumentUpload />}
        {activeTab === 'tests' && <Tests />}
      </div>
    </div>
  );
};

export default UserManagement;
