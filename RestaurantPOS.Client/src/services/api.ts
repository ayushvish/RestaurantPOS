import axios from 'axios';
import * as signalR from '@microsoft/signalr';

export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5122';

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('pos_token') || localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      console.warn('[API Client] 401 Unauthorized encountered');
    }
    return Promise.reject(error);
  }
);

// SignalR Real-Time POS Connection
let hubConnection: signalR.HubConnection | null = null;

export const getPosHubConnection = (): signalR.HubConnection => {
  if (!hubConnection) {
    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/pos`, {
        accessTokenFactory: () => {
          return (
            localStorage.getItem('token') ||
            localStorage.getItem('pos_token') ||
            ''
          );
        },
        skipNegotiation: false,
        transport:
          signalR.HttpTransportType.WebSockets |
          signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }
  return hubConnection;
};

export const resetPosHubConnection = async (): Promise<void> => {
  if (hubConnection) {
    try {
      await hubConnection.stop();
    } catch (e) {
      console.warn('[SignalR] Error stopping connection during reset:', e);
    }
    hubConnection = null;
  }
};
