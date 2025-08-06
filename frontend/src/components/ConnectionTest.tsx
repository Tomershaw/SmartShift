import React, { useState, useEffect } from 'react';

interface ConnectionStatus {
  Message: string;
  ProjectName: string;
  Technology: string;
  Language: string;
  Timestamp: string;
  Status: string;
  Developer: string;
}

export const ConnectionTest: React.FC = () => {
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const testConnection = async () => {
      try {
        const response = await fetch('/api/connection-test');
        if (response.ok) {
          const data = await response.json();
          setConnectionStatus(data);
        } else {
          setError('שגיאה בחיבור לשרת');
        }
      } catch (err) {
        setError('שגיאה בבדיקת החיבור');
        console.error('Connection test error:', err);
      } finally {
        setLoading(false);
      }
    };

    testConnection();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="text-lg text-gray-600">בודק חיבור...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
        <strong>שגיאה:</strong> {error}
      </div>
    );
  }

  if (!connectionStatus) {
    return (
      <div className="text-gray-500">אין נתוני חיבור</div>
    );
  }

  return (
    <div className="bg-green-50 border border-green-200 rounded-lg p-6 max-w-2xl mx-auto">
      <div className="text-center mb-4">
        <h2 className="text-2xl font-bold text-green-800 mb-2">
          {connectionStatus.Message}
        </h2>
        <div className="text-lg text-green-700">
          {connectionStatus.ProjectName}
        </div>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
        <div className="bg-white p-3 rounded border">
          <strong>טכנולוגיה:</strong>
          <div className="text-gray-700">{connectionStatus.Technology}</div>
        </div>
        
        <div className="bg-white p-3 rounded border">
          <strong>שפה:</strong>
          <div className="text-gray-700">{connectionStatus.Language}</div>
        </div>
        
        <div className="bg-white p-3 rounded border">
          <strong>סטטוס:</strong>
          <div className="text-green-600 font-semibold">{connectionStatus.Status}</div>
        </div>
        
        <div className="bg-white p-3 rounded border">
          <strong>זמן:</strong>
          <div className="text-gray-700">{connectionStatus.Timestamp}</div>
        </div>
      </div>
      
      <div className="mt-4 text-center bg-blue-50 p-3 rounded border">
        <div className="text-blue-800 font-medium">
          {connectionStatus.Developer}
        </div>
      </div>
    </div>
  );
};

export default ConnectionTest;