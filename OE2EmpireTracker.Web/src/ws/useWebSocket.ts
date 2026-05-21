import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../auth/store';
import { WebSocketClient, type ServerPushEvent } from './WebSocketClient';
import { queryKeys } from '../api/hooks/queryKeys';

function handleServerEvent(
  event: ServerPushEvent,
  invalidate: (queryKey: readonly unknown[]) => void
): void {
  const { entityType, ownerCharacterUUID } = event;

  switch (entityType) {
    case 'colony':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.characterData(ownerCharacterUUID, 'Colonies'));
      }
      break;
    case 'blueprint':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.characterData(ownerCharacterUUID, 'Blueprints'));
      }
      invalidate(['public', 'blueprints']);
      break;
    case 'survey':
      if (ownerCharacterUUID) {
        invalidate(queryKeys.characterData(ownerCharacterUUID, 'Surveys'));
      }
      invalidate(['public', 'surveys']);
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

export function useWebSocket(): void {
  const queryClient = useQueryClient();
  const token = useAuthStore((s) => s.token);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const clientRef = useRef<WebSocketClient | null>(null);

  useEffect(() => {
    if (!isAuthenticated || !token) {
      clientRef.current?.disconnect();
      clientRef.current = null;
      return;
    }

    const client = new WebSocketClient((event) => {
      handleServerEvent(event, (queryKey) => {
        queryClient.invalidateQueries({ queryKey });
      });
    });

    client.connect(token);
    clientRef.current = client;

    return () => {
      client.disconnect();
      clientRef.current = null;
    };
  }, [isAuthenticated, token, queryClient]);
}
