import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useContacts, useContactDetail, useContactMutations } from '../../api/hooks/useContacts';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { ExternalCharacter } from '../../api/types/domain';

interface FormState {
  name: string;
  faction: string;
  notes: string;
}

const emptyForm: FormState = {
  name: '',
  faction: '',
  notes: '',
};

function formFromContact(contact: ExternalCharacter): FormState {
  return {
    name: contact.name,
    faction: contact.faction ?? '',
    notes: contact.notes ?? '',
  };
}

const filterDefs: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Search contacts...' },
];

export function ContactsForm() {
  const { characterUUID } = useAuthStore();
  const { data: contacts, isLoading, isError, refetch } = useContacts(characterUUID);
  const { save, remove } = useContactMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });

  // Fetch detail for selected contact
  const { data: selectedContact } = useContactDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Filtered contact list
  const filteredContacts = useMemo(() => {
    if (!contacts) return [];
    const search = ((filterValues.search as string) ?? '').toLowerCase();
    if (!search) return contacts;
    return contacts.filter(
      (c) =>
        c.name.toLowerCase().includes(search) ||
        (c.faction ?? '').toLowerCase().includes(search) ||
        (c.notes ?? '').toLowerCase().includes(search),
    );
  }, [contacts, filterValues.search]);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const contact = contacts?.find((c) => c.uuid === uuid);
    if (contact) {
      setForm(formFromContact(contact));
      setIsDirty(false);
    }
  }, [contacts]);

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

  const handleFieldChange = useCallback((field: keyof FormState, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setIsDirty(true);
  }, []);

  const handleSave = useCallback(async () => {
    const data: Partial<ExternalCharacter> = {
      name: form.name,
      faction: form.faction || undefined,
      notes: form.notes || undefined,
    };

    if (isNewMode) {
      const result = await save.mutateAsync({ data });
      setSelectedId(result.uuid);
      setIsNewMode(false);
    } else if (selectedId) {
      await save.mutateAsync({ entityUUID: selectedId, data });
    }
    setIsDirty(false);
  }, [isNewMode, selectedId, form, save]);

  const handleDelete = useCallback(async () => {
    if (!selectedId || isNewMode) return;
    await remove.mutateAsync(selectedId);
    setSelectedId(null);
    setForm(emptyForm);
    setIsDirty(false);
    setShowDeleteConfirm(false);
  }, [selectedId, isNewMode, remove]);

  // Sync form when detail loads from server
  const detailUUID = selectedContact?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedContact && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromContact(selectedContact));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading contacts..." />;
  if (isError) return <RetryableError message="Failed to load contacts." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Contacts</h2>
      </div>
      <div className="px-3 pt-3">
        <FilterBar filters={filterDefs} values={filterValues} onChange={setFilterValues} />
      </div>
      {filteredContacts.length === 0 ? (
        <EmptyState
          title="No contacts"
          message={contacts?.length ? 'No contacts match the current filter.' : 'Create a new contact to get started.'}
        />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {filteredContacts.map((c) => (
            <li key={c.uuid}>
              <button
                onClick={() => handleSelect(c.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === c.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{c.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">
                  {c.faction ?? 'No faction'} {c.notes ? `— ${c.notes.slice(0, 40)}` : ''}
                </div>
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
            {isNewMode ? 'New Contact' : 'Contact Details'}
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
              {save.isPending ? 'Saving...' : 'Save'}
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
        <EmptyState title="No contact selected" message="Select a contact from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="contact-name" className="mb-1 block text-sm text-gray-400">Name</label>
              <input
                id="contact-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="contact-faction" className="mb-1 block text-sm text-gray-400">Faction</label>
              <input
                id="contact-faction"
                type="text"
                value={form.faction}
                onChange={(e) => handleFieldChange('faction', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="contact-notes" className="mb-1 block text-sm text-gray-400">Notes</label>
              <textarea
                id="contact-notes"
                value={form.notes}
                onChange={(e) => handleFieldChange('notes', e.target.value)}
                rows={4}
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
        title="Delete Contact"
        message={`Are you sure you want to delete "${form.name || 'this contact'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
