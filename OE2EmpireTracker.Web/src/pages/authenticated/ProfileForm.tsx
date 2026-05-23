import { useState, useCallback } from 'react';
import { useAuthStore } from '../../auth/store';
import { useProfiles, useProfileDetail, useProfileMutations } from '../../api/hooks/useProfiles';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { PlayerProfile } from '../../api/types/domain';

interface FormState {
  name: string;
  faction: string;
  totalCredits: number;
  skillPoints: number;
}

const emptyForm: FormState = {
  name: '',
  faction: '',
  totalCredits: 0,
  skillPoints: 0,
};

function formFromProfile(profile: PlayerProfile): FormState {
  return {
    name: profile.name,
    faction: profile.faction ?? '',
    totalCredits: profile.totalCredits,
    skillPoints: profile.skillPoints,
  };
}

export function ProfileForm() {
  const { characterUUID } = useAuthStore();
  const { data: profiles, isLoading, isError, refetch } = useProfiles(characterUUID);
  const { create, save, remove } = useProfileMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Fetch detail for selected profile
  const { data: selectedProfile } = useProfileDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const profile = profiles?.find((p) => p.uuid === uuid);
    if (profile) {
      setForm(formFromProfile(profile));
      setIsDirty(false);
    }
  }, [profiles]);

  const handleBack = useCallback(() => {
    setSelectedId(null);
    setIsNewMode(false);
    setIsDirty(false);
  }, []);

  const handleNew = useCallback(() => {
    setSelectedId('new');
    setIsNewMode(true);
    setForm(emptyForm);
    setIsDirty(false);
  }, []);

  const handleFieldChange = useCallback(
    (field: keyof FormState, value: string | number) => {
      setForm((prev) => ({ ...prev, [field]: value }));
      setIsDirty(true);
    },
    [],
  );

  const handleSave = useCallback(async () => {
    if (isNewMode) {
      const result = await create.mutateAsync({
        name: form.name,
        faction: form.faction || undefined,
        totalCredits: form.totalCredits,
        skillPoints: form.skillPoints,
        ranks: { public: { level: 0, currentXP: 0, xpToNext: 0 }, private: { level: 0, currentXP: 0, xpToNext: 0 }, military: { level: 0, currentXP: 0, xpToNext: 0 } },
        skillGroups: [],
      });
      setSelectedId(result.uuid);
      setIsNewMode(false);
    } else if (selectedId) {
      await save.mutateAsync({
        entityUUID: selectedId,
        data: {
          name: form.name,
          faction: form.faction || undefined,
          totalCredits: form.totalCredits,
          skillPoints: form.skillPoints,
        },
      });
    }
    setIsDirty(false);
  }, [isNewMode, selectedId, form, create, save]);

  const handleDelete = useCallback(async () => {
    if (!selectedId || isNewMode) return;
    await remove.mutateAsync(selectedId);
    setSelectedId(null);
    setForm(emptyForm);
    setIsDirty(false);
    setShowDeleteConfirm(false);
  }, [selectedId, isNewMode, remove]);

  // Sync form when detail loads from server
  const detailUUID = selectedProfile?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedProfile && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromProfile(selectedProfile));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading profiles..." />;
  if (isError) return <RetryableError message="Failed to load profiles." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Profiles</h2>
      </div>
      {(!profiles || profiles.length === 0) ? (
        <EmptyState title="No profiles" message="Create a new profile to get started." />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {profiles.map((p) => (
            <li key={p.uuid}>
              <button
                onClick={() => handleSelect(p.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === p.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{p.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">{p.faction || 'No faction'}</div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-white">
            {isNewMode ? 'New Profile' : 'Profile Details'}
          </h2>
          <div className="flex gap-2">
            <button
              onClick={handleNew}
              className="rounded bg-green-600 px-3 py-1.5 text-sm text-white hover:bg-green-700"
            >
              New
            </button>
            <button
              onClick={() => void handleSave()}
              disabled={!isDirty && !isNewMode}
              className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {save.isPending || create.isPending ? 'Saving...' : 'Save'}
            </button>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={isNewMode || !selectedId}
              className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700 disabled:opacity-50"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      {!selectedId && !isNewMode ? (
        <EmptyState title="No profile selected" message="Select a profile from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="profile-name" className="mb-1 block text-sm text-gray-400">Name</label>
              <input
                id="profile-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="profile-faction" className="mb-1 block text-sm text-gray-400">Faction</label>
              <input
                id="profile-faction"
                type="text"
                value={form.faction}
                onChange={(e) => handleFieldChange('faction', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="profile-credits" className="mb-1 block text-sm text-gray-400">Total Credits</label>
              <input
                id="profile-credits"
                type="number"
                value={form.totalCredits}
                onChange={(e) => handleFieldChange('totalCredits', Number(e.target.value) || 0)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="profile-skillpoints" className="mb-1 block text-sm text-gray-400">Skill Points</label>
              <input
                id="profile-skillpoints"
                type="number"
                value={form.skillPoints}
                onChange={(e) => handleFieldChange('skillPoints', Number(e.target.value) || 0)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <>
      <MasterDetailLayout
        listPanel={listPanel}
        detailPanel={detailPanel}
        selectedId={selectedId}
        onBack={handleBack}
      />
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        title="Delete Profile"
        message={`Are you sure you want to delete "${form.name || 'this profile'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
