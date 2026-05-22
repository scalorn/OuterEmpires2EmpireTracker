import { useState, useEffect } from 'react';
import { useAuthStore } from '../../auth/store';
import { profilesApi } from '../../api/endpoints/profiles';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';

interface ProfileData {
  Name?: string;
  Rank?: string;
  Profession?: string;
  Skills?: Record<string, number>;
  [key: string]: unknown;
}

export function ProfileEditor() {
  const { characterUUID } = useAuthStore();
  const [profile, setProfile] = useState<ProfileData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [editName, setEditName] = useState('');
  const [editRank, setEditRank] = useState('');
  const [editProfession, setEditProfession] = useState('');

  useEffect(() => {
    if (!characterUUID) return;
    setIsLoading(true);
    profilesApi.getAll(characterUUID)
      .then((data) => {
        const profiles = Array.isArray(data) ? data : [];
        const p = (profiles[0] ?? {}) as ProfileData;
        setProfile(p);
        setEditName(p.Name ?? '');
        setEditRank(p.Rank ?? '');
        setEditProfession(p.Profession ?? '');
      })
      .catch(() => setError('Failed to load profile.'))
      .finally(() => setIsLoading(false));
  }, [characterUUID]);

  const handleSave = async () => {
    if (!characterUUID || !profile) return;
    setIsSaving(true);
    try {
      const updated = { ...profile, Name: editName, Rank: editRank, Profession: editProfession };
      const uuid = String(profile.UUID ?? profile.uuid ?? characterUUID);
      await profilesApi.update(characterUUID, uuid, updated);
      setProfile(updated);
    } catch {
      setError('Failed to save profile.');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) return <LoadingSpinner message="Loading profile..." />;
  if (error) return <RetryableError message={error} onRetry={() => window.location.reload()} />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold text-white">Profile Editor</h1>
      <div className="max-w-lg space-y-4">
        <div>
          <label className="mb-1 block text-sm text-gray-400">Name</label>
          <input
            type="text"
            value={editName}
            onChange={(e) => setEditName(e.target.value)}
            className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm text-gray-400">Rank</label>
          <input
            type="text"
            value={editRank}
            onChange={(e) => setEditRank(e.target.value)}
            className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm text-gray-400">Profession</label>
          <input
            type="text"
            value={editProfession}
            onChange={(e) => setEditProfession(e.target.value)}
            className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
          />
        </div>

        {profile?.Skills && Object.keys(profile.Skills).length > 0 && (
          <div>
            <label className="mb-1 block text-sm text-gray-400">Skills</label>
            <div className="rounded border border-gray-700 p-3">
              {Object.entries(profile.Skills).map(([skill, level]) => (
                <div key={skill} className="flex justify-between border-b border-gray-700 py-1 text-sm last:border-0">
                  <span className="text-gray-300">{skill}</span>
                  <span className="text-white">{level}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <button
          onClick={() => void handleSave()}
          disabled={isSaving}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {isSaving ? 'Saving...' : 'Save Profile'}
        </button>
      </div>
    </div>
  );
}
