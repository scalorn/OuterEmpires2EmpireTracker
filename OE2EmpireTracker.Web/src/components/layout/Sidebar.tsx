import { NavLink } from 'react-router-dom';
import * as Collapsible from '@radix-ui/react-collapsible';
import { useState, useEffect } from 'react';
import { useAuthStore } from '../../auth/store';

interface NavItem {
  label: string;
  to: string;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

const publicNavItems: NavItem[] = [
  { label: 'Blueprints', to: '/blueprints' },
  { label: 'Surveys', to: '/surveys' },
  { label: 'Colony Planner', to: '/planner' },
  { label: 'Ship Builder', to: '/ship-builder' },
];

const authenticatedNavGroups: NavGroup[] = [
  {
    label: 'Empire',
    items: [
      { label: 'Dashboard', to: '/app' },
      { label: 'Colonies', to: '/app/colonies' },
      { label: 'Blueprints', to: '/app/blueprints' },
      { label: 'Surveys', to: '/app/surveys' },
      { label: 'Profiles', to: '/app/profiles' },
    ],
  },
  {
    label: 'Logistics',
    items: [
      { label: 'Delivery Routes', to: '/app/routes' },
      { label: 'Delivery Execution', to: '/app/delivery' },
      { label: 'Ship Templates', to: '/app/ships' },
    ],
  },
  {
    label: 'Market',
    items: [
      { label: 'Market', to: '/app/market' },
      { label: 'Supply Chains', to: '/app/supply-chains' },
      { label: 'Stock Targets', to: '/app/stock-targets' },
      { label: 'Pricing Plans', to: '/app/pricing-plans' },
    ],
  },
  {
    label: 'Planning',
    items: [
      { label: 'Colony Activity', to: '/app/activity' },
      { label: 'Daily Build', to: '/app/daily-build' },
      { label: 'Build Planner', to: '/app/build-planner' },
    ],
  },
  {
    label: 'Admin',
    items: [
      { label: 'Contacts', to: '/app/contacts' },
      { label: 'Stations', to: '/app/stations' },
      { label: 'Asteroids', to: '/app/asteroids' },
      { label: 'Shared Data', to: '/app/shared' },
    ],
  },
];

export function Sidebar() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [isOpen, setIsOpen] = useState(() => window.innerWidth >= 1024);

  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth >= 1024) {
        setIsOpen(true);
      }
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return (
    <Collapsible.Root open={isOpen} onOpenChange={setIsOpen}>
      <aside className="flex h-full w-56 flex-col border-r border-gray-700 bg-gray-800">
        <div className="flex items-center justify-between p-4">
          <span className="text-sm font-semibold text-gray-300">Navigation</span>
          <Collapsible.Trigger asChild>
            <button
              className="rounded p-1 text-gray-400 hover:bg-gray-700 hover:text-white lg:hidden"
              aria-label={isOpen ? 'Collapse sidebar' : 'Expand sidebar'}
            >
              {isOpen ? '◀' : '▶'}
            </button>
          </Collapsible.Trigger>
        </div>

        <Collapsible.Content className="flex-1 overflow-y-auto">
          <nav className="space-y-1 px-2 pb-4">
            <SectionLabel label="Public" />
            {publicNavItems.map((item) => (
              <SidebarLink key={item.to} {...item} />
            ))}

            {isAuthenticated &&
              authenticatedNavGroups.map((group) => (
                <div key={group.label}>
                  <SectionLabel label={group.label} />
                  {group.items.map((item) => (
                    <SidebarLink key={item.to} {...item} />
                  ))}
                </div>
              ))}
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
