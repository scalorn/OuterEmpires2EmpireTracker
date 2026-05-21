import { getRuntimeConfig } from '../hooks/useRuntimeConfig';
import {
  WS_PING_INTERVAL_MS,
  WS_MAX_RECONNECT_ATTEMPTS,
  WS_MAX_BACKOFF_MS,
} from '../utils/constants';

export interface ServerPushEvent {
  type: string;
  entityType: string;
  entityUUID: string;
  timestamp: string;
  ownerCharacterUUID?: string;
}

export class WebSocketClient {
  private ws: WebSocket | null = null;
  private reconnectAttempts = 0;
  private pingInterval: ReturnType<typeof setInterval> | null = null;
  private reconnectTimeout: ReturnType<typeof setTimeout> | null = null;
  private token: string | null = null;
  private onEvent: (event: ServerPushEvent) => void;

  constructor(onEvent: (event: ServerPushEvent) => void) {
    this.onEvent = onEvent;
  }

  connect(token: string): void {
    this.token = token;
    const baseUrl = getRuntimeConfig().apiBaseUrl || window.location.origin;
    const wsUrl = baseUrl.replace(/^http/, 'ws') + `/ws?token=${token}`;

    this.ws = new WebSocket(wsUrl);
    this.ws.onopen = () => this.handleOpen();
    this.ws.onmessage = (e) => this.handleMessage(e);
    this.ws.onclose = () => this.handleClose();
    this.ws.onerror = () => {
      // onclose will fire after onerror, reconnect handled there
    };
  }

  disconnect(): void {
    this.token = null;
    this.stopPing();
    this.clearReconnectTimeout();
    if (this.ws) {
      this.ws.onclose = null;
      this.ws.close();
      this.ws = null;
    }
  }

  private handleOpen(): void {
    this.reconnectAttempts = 0;
    this.startPing();
  }

  private handleMessage(event: MessageEvent): void {
    try {
      const data = JSON.parse(event.data as string);
      if (data.type === 'pong' || data.type === 'connected') return;
      this.onEvent(data as ServerPushEvent);
    } catch {
      // Ignore malformed messages
    }
  }

  private handleClose(): void {
    this.stopPing();
    this.reconnectWithBackoff();
  }

  private startPing(): void {
    this.pingInterval = setInterval(() => {
      if (this.ws?.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify({ type: 'ping' }));
      }
    }, WS_PING_INTERVAL_MS);
  }

  private stopPing(): void {
    if (this.pingInterval) {
      clearInterval(this.pingInterval);
      this.pingInterval = null;
    }
  }

  private clearReconnectTimeout(): void {
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }
  }

  private reconnectWithBackoff(): void {
    if (!this.token) return;
    if (this.reconnectAttempts >= WS_MAX_RECONNECT_ATTEMPTS) return;

    const delay = Math.min(
      1000 * Math.pow(2, this.reconnectAttempts),
      WS_MAX_BACKOFF_MS
    );
    this.reconnectAttempts++;

    this.reconnectTimeout = setTimeout(() => {
      if (this.token) {
        this.connect(this.token);
      }
    }, delay);
  }
}
