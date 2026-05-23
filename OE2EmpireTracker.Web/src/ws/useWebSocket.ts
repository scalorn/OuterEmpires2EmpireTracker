import { useEffect, useRef, useCallback, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../auth/store';
import { WebSocketClient, type ServerPushEvent, type ConnectionState } from './WebSocketClient';
import { queryKeys } from '../api/hooks/queryKeys';

function handleServerEvent(
  event: ServerPushEvent,
  invalidate: (queryKey: readonly unknown[]) => void
): void {
  const { entityType, ownerCharacterUUID } = event;

  switch (entityType) {
    case 'colony':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.colonies(ownerCharacterUUID));
      }
      break;
    case 'blueprint':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.blueprints(ownerCharacterUUID));
      }
      invalidate(['public', 'blueprints']);
      break;
    case 'survey':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.surveys(ownerCharacterUUID));
      }
      invalidate(['public', 'surveys']);
      break;
    case 'deliveryRoute':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.deliveryRoutes(ownerCharacterUUID));
      }
      break;
    case 'deliveryPlan':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.deliveryPlans(ownerCharacterUUID));
      }
      break;
    case 'shipTemplate':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.shipTemplates(ownerCharacterUUID));
      }
      break;
    case 'marketListing':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.marketListings(ownerCharacterUUID));
      }
      break;
    case 'marketTransaction':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.marketTransactions(ownerCharacterUUID));
      }
      break;
    case 'buildPlan':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.buildPlans(ownerCharacterUUID));
      }
      break;
    case 'playerProfile':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.profiles(ownerCharacterUUID));
      }
      break;
    case 'station':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.stations(ownerCharacterUUID));
      }
      break;
    case 'asteroid':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.asteroids(ownerCharacterUUID));
      }
      break;
    case 'externalCharacter':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.contacts(ownerCharacterUUID));
      }
      break;
    case 'supplyChain':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.supplyChains(ownerCharacterUUID));
      }
      break;
    case 'stockProfile':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.stockProfiles(ownerCharacterUUID));
      }
      break;
    case 'stockPlan':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.stockPlans(ownerCharacterUUID));
      }
      break;
    case 'pricingPlan':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.pricingPlans(ownerCharacterUUID));
      }
      break;
    case 'faction':
      invalidate(queryKeys.factions);
      break;
    case 'sharing':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.sharing(ownerCharacterUUID));
      }
      break;
  }
}

export type WebSocketStatus = 'connected' | 'reconnecting' | 'disconnected';

export interface WebSocketState {
  status: WebSocketStatus;
  retry: () => void;
}


const WS_DISCONNECT_THRESHOLD_MS = 60_000;

export function useWebSocket(): WebSocketState {
  const queryClient = useQueryClient();
  const token = useAuthStore((s) => s.token);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const clientRef = useRef<WebSocketClient | null>(null);
  const disconnectedAtRef = useRef<number | null>(null);
  const [status, setStatus] = useState<WebSocketStatus>('connected');

  const handleStateChange = useCallback(
    (state: ConnectionState) => {
      if (state === 'connected') {
        const wasDisconnected = disconnectedAtRef.current !== null;
        disconnectedAtRef.current = null;
        setStatus('connected');
        // On reconnection, invalidate all active queries
        if (wasDisconnected) {
          queryClient.invalidateQueries();
        }
      } else if (state === 'reconnecting') {
        if (disconnectedAtRef.current === null) {
          disconnectedAtRef.current = Date.now();
        }
        // Check if we've been disconnected longer than threshold
        const elapsed = Date.now() - (disconnectedAtRef.current ?? Date.now());
        if (elapsed >= WS_DISCONNECT_THRESHOLD_MS) {
          setStatus('disconnected');
        } else {
          setStatus('reconnecting');
        }
      } else {
        // closed / gave up
        setStatus('disconnected');
      }
    },
    [queryClient],
  );

  // Periodically check if reconnecting has exceeded the threshold
  useEffect(() => {
    if (status !== 'reconnecting') return;

    const interval = setInterval(() => {
      if (disconnectedAtRef.current !== null) {
        const elapsed = Date.now() - disconnectedAtRef.current;
        if (elapsed >= WS_DISCONNECT_THRESHOLD_MS) {
          setStatus('disconnected');
        }
      }
    }, 5_000);

    return () => clearInterval(interval);
  }, [status]);

  const retry = useCallback(() => {
    if (clientRef.current && token) {
      disconnectedAtRef.current = null;
      setStatus('reconnecting');
      clientRef.current.reconnect(token);
    }
  }, [token]);

  useEffect(() => {
    if (!isAuthenticated || !token) {
      clientRef.current?.disconnect();
      clientRef.current = null;
      setStatus('connected');
      return;
    }

    const client = new WebSocketClient(
      (event) => {
        handleServerEvent(event, (queryKey) => {
          queryClient.invalidateQueries({ queryKey });
        });
      },
      handleStateChange,
    );

    client.connect(token);
    clientRef.current = client;

    return () => {
      client.disconnect();
      clientRef.current = null;
    };
  }, [isAuthenticated, token, queryClient, handleStateChange]);

  return { status, retry };
}
