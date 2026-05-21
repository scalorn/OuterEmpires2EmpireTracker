import { NavLink } from 'react-router-dom';
import * as Collapsible from '@radix-ui/react-collapsible';
import { useState } from 'react';
import { useAuthStore } from '../../auth/store';

interface NavItem {
  label: string;
  to: string;
}

const publicNavItems: NavItem[] = [
  { label: 'Blueprints', to: '/blueprints' },
  { label: 'Surveys', to: '/surveys' },
  { label: 'Colony Planner', to: '/planner' },
];

const authenticatedNavItems: NavItem[] = [
  { label: 'Dashboard', to: '/app' },
  { label: 'Colonies', to: '/app/colonies' },
  { label: 'Blueprints', to: '/app/blueprints' },
  { label: 'Surveys', to: '/app/surveys' },
  { label: 'Profile', to: '/app/profile' },
  { label: 'Faction', to: '/app/faction' },
  { label: 'Sharing', to: '/app/sharing' },
];

export function Sidebar() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [isOpen, setIsOpen] = useState(true);

  return (
    <Collapsible.Root open={isOpen} onOpenChange={setIsOpen}>
      <aside className="flex h-full w-56 flex-col border-r border-gray-700 bg-gray-800">
        <div className="flex items-center justify-between p-4">
          <span className="text-sm font-semibold text-gray-300">Navigation</span>
          <Collapsible.Trigger asChild>
            <button
              className="rounded p-1 text-gray-400 hover:bg-gray-700 hover:text-white"
              aria-label={isOpen ? 'Collapse sidebar' : 'Expand sidebar'}
            >
              {isOpen ? '◀' : '▶'}
            </button>
          </Collapsible.Trigger>
        </div>

        <Collapsible.Content className="flex-1 overflow-y-auto">
          <nav className="space-y-1 px-2">
            <SectionLabel label="Public" />
            {publicNavItems.map((item) => (
              <SidebarLink key={item.to} {...item} />
            ))}

            {isAuthenticated && (
              <>
                <SectionLabel label="My Data" />
                {authenticatedNavItems.map((item) => (
                  <SidebarLink key={item.to} {...item} />
                ))}
              </>
            )}
          </nav>
        </Collapsible.Content>
      </aside>
    </Collapsible.Root>
  );
}

function SectionLabel({ label }: { label: string }) {
  return (
    <p className="px-3 pb-1 pt-4 text-xs font-semibold uppercase tracking-wider text-gray-500">
      {label}
    </p>
  );
}

function SidebarLink({ label, to }: NavItem) {
  return (
    <NavLink
      to={to}
      end={to === '/app'}
      className={({ isActive }) =>
        `block rounded px-3 py-2 text-sm ${
          isActive
            ? 'bg-blue-600 text-white'
            : 'text-gray-300 hover:bg-gray-700 hover:text-white'
        }`
      }
    >
      {label}
    </NavLink>
  );
}
