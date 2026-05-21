import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { registerAuthStore } from '../api/client';
import { charactersApi } from '../api/endpoints/characters';
import type { TokenRole } from '../api/types/generated';

export interface AuthState {
  token: string | null;
  characterName: string | null;
  characterUUID: string | null;
  role: TokenRole | null;
  isAuthenticated: boolean;
  loginError: string | null;
  login: (token: string) => Promise<void>;
  logout: () => void;
  switchCharacter: (uuid: string) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      characterName: null,
      characterUUID: null,
      role: null,
      isAuthenticated: false,
      loginError: null,

      login: async (token: string) => {
        // Temporarily set token so the API client can use it
        set({ token, loginError: null });

        try {
          const characters = await charactersApi.getAll();
          if (characters.length > 0) {
            const first = characters[0];
            set({
              token,
              characterName: first.name,
              characterUUID: first.uuid,
              role: first.role,
              isAuthenticated: true,
              loginError: null,
            });
          } else {
            set({
              token,
              isAuthenticated: true,
              loginError: null,
            });
          }
        } catch {
          set({
            token: null,
            characterName: null,
            characterUUID: null,
            role: null,
            isAuthenticated: false,
            loginError: 'Invalid or expired token. Please try again.',
          });
        }
      },

      logout: () => {
        set({
          token: null,
          characterName: null,
          characterUUID: null,
          role: null,
          isAuthenticated: false,
          loginError: null,
        });
      },

      switchCharacter: (uuid: string) => {
        set({ characterUUID: uuid });
      },
    }),
    { name: 'oe2-auth' }
  )
);

// Register the auth store with the API client to break circular dependency
registerAuthStore(() => useAuthStore.getState());
