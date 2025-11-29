import axios from 'axios';

// Configure axios default base URL for the API.
// NOTE: adjust the port below to match your backend API port if it's different.
//const API_PORT = 5102;
axios.defaults.baseURL = process.env.REACT_APP_API_URL

// Response interceptor to log response errors centrally
axios.interceptors.response.use(
  response => response,
  error => {
    // Log the error for debugging (can be replaced with a more robust logger)
    console.error('API response error:', error);
    return Promise.reject(error);
  }
);

export default {
  // Fetch all tasks
  getTasks: async () => {
    const result = await axios.get('/api/items');
    return result.data;
  },

  // Add a new task. Expects `name` string. Returns created item (result.data).
  addTask: async (name) => {
    console.log('addTask', name);
    const payload = { name, isComplete: false };
    const result = await axios.post('/api/items', payload);
    return result.data;
  },

  // Set completed state for a task. Expects task id and boolean isComplete.
  // Uses PUT to update resource.
  setCompleted: async (id, isComplete) => {
    console.log('setCompleted', { id, isComplete });
    const payload = { isComplete: !!isComplete };
    const result = await axios.put(`/api/items/${id}`, payload);
    return result.data;
  },

  // Delete a task by id
  deleteTask: async (id) => {
    console.log('deleteTask', id);
    const result = await axios.delete(`/api/items/${id}`);
    return result.data;
  }
};
