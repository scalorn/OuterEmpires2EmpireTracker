# Requirements Document

## Introduction

BL-124 ensures that read-only display forms (FormColonyActivity, FormColonyDailyBuild) and pure dialog forms (FormAutoFill) consistently use ReadOnly wrapper types for all data access. These forms do not mutate entities --- they only display data or return user selections. No services or ViewModels are needed.

### Key Characteristics

1. **FormColonyActivity** --- Read-only display form showing colony activity data. Needs verification that all data access uses ReadOnly consistently.
2. **FormColonyDailyBuild** --- Read-only display form showing daily build schedules. Needs verification.
3. **FormAutoFill** --- Pure dialog form that returns auto-fill options. Does not access entity data beyond preferences.
4. **No services needed** --- These forms do not mutate entities.
5. **No ViewModels needed** --- These forms do not have edit buffers.
6. **No unsaved changes prompts** --- These forms do not have editable state.

## Glossary

- **FormColonyActivity**: A read-only display form showing colony activity data.
- **FormColonyDailyBuild**: A read-only display form showing daily build schedules.
- **FormAutoFill**: A pure dialog form that returns auto-fill configuration options.

## Requirements

### Requirement 1: FormColonyActivity Uses ReadOnly Types
#### Acceptance Criteria
1. ALL data access in FormColonyActivity SHALL use ReadOnly wrapper types.
2. THE form SHALL NOT hold direct references to mutable entities in any code path.
3. THE ColonyActivityCollector SHALL receive ReadOnly types or be adapted to work with them.

### Requirement 2: FormColonyDailyBuild Uses ReadOnly Types
#### Acceptance Criteria
1. ALL data access in FormColonyDailyBuild SHALL use ReadOnly wrapper types.
2. THE form SHALL NOT hold direct references to mutable entities.
3. Route selection and colony data lookup SHALL use ReadOnly accessors.

### Requirement 3: FormAutoFill Verified Clean
#### Acceptance Criteria
1. FormAutoFill SHALL be verified to not access any mutable entity data.
2. FormAutoFill only accesses PreferencesStore --- this is acceptable.

### Requirement 4: No Mutable Entity References in Display Forms
#### Acceptance Criteria
1. AFTER migration, a grep for mutable entity type usage in these forms SHALL find no direct mutable entity references.

## Correctness Properties

### Property 1: No Mutable Entity References in Display Forms

AFTER migration, static analysis SHALL confirm that FormColonyActivity and FormColonyDailyBuild do not hold direct references to mutable entities.

## Out of Scope

- Adding services or ViewModels to these forms.
- Adding unsaved changes prompts.
- Changing ColonyActivityCollector internals beyond ReadOnly compatibility.
- FormAutoFill behavior changes.
