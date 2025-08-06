import React, { useState } from 'react';
import axios from 'axios';

interface ConnectionResponse {
  message: string;
  englishMessage: string;
  projectName: string;
  status: string;
  timestamp: string;
}

export const ConnectionTest: React.FC = () => {
  const [response, setResponse] = useState<ConnectionResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const testConnection = async () => {
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const result = await axios.get<ConnectionResponse>('http://localhost:5000/api/connection/test');
      setResponse(result.data);
    } catch (err) {
      setError('Failed to connect to the API. Make sure the server is running on http://localhost:5000');
      console.error('Connection test failed:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="connection-test-container p-6 max-w-2xl mx-auto">
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h2 className="text-2xl font-bold mb-4 text-center text-gray-800">
          בדיקת חיבור לפרוייקט SmartShift
        </h2>
        <h3 className="text-lg text-center text-gray-600 mb-6">
          SmartShift Project Connection Test
        </h3>

        <div className="text-center mb-6">
          <p className="text-lg mb-4 text-gray-700">
            השאלה: <strong>האם אתה מחובר לפרוייקט שלי ?</strong>
          </p>
          <p className="text-md text-gray-600">
            Question: <em>Are you connected to my project?</em>
          </p>
        </div>

        <div className="text-center mb-6">
          <button
            onClick={testConnection}
            disabled={loading}
            className="bg-blue-500 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold py-2 px-4 rounded transition-colors"
          >
            {loading ? 'בודק חיבור...' : 'בדוק חיבור'}
            <span className="block text-sm">
              {loading ? 'Testing Connection...' : 'Test Connection'}
            </span>
          </button>
        </div>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            <p className="font-bold">שגיאה / Error:</p>
            <p>{error}</p>
          </div>
        )}

        {response && (
          <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
            <h4 className="font-bold mb-2">תשובה מהשרת / Server Response:</h4>
            
            <div className="mb-4">
              <p className="text-lg font-semibold text-right mb-2">
                🔗 {response.message}
              </p>
              <p className="text-md text-left">
                🔗 {response.englishMessage}
              </p>
            </div>

            <div className="text-sm space-y-1">
              <p><strong>Project:</strong> {response.projectName}</p>
              <p><strong>Status:</strong> {response.status}</p>
              <p><strong>Timestamp:</strong> {response.timestamp}</p>
            </div>
          </div>
        )}

        <div className="mt-6 text-sm text-gray-600 text-center">
          <p>This test calls: <code className="bg-gray-100 px-2 py-1 rounded">GET /api/connection/test</code></p>
        </div>
      </div>
    </div>
  );
};