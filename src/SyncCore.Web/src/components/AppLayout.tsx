import { NavLink, Outlet } from 'react-router-dom';

const navItems = [
  { to: '/', label: 'Photos', icon: '🖼' },
  { to: '/favourites', label: 'Favourites', icon: '★' },
  { to: '/albums', label: 'Albums', icon: '📁' },
  { to: '/trash', label: 'Trash', icon: '🗑' },
];

export function AppLayout() {

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      {/* Sidebar */}
      <nav className="hidden md:flex flex-col w-56 bg-white border-r border-gray-100 p-4 gap-1 shrink-0">
        <p className="text-xl font-bold text-blue-600 mb-4 px-2">SyncCore</p>
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                isActive ? 'bg-blue-50 text-blue-700' : 'text-gray-600 hover:bg-gray-50'
              }`
            }
          >
            <span>{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
        <div className="mt-auto pt-4 border-t border-gray-100">
          <p className="text-xs text-gray-500 px-2 truncate">local-dev-user</p>
        </div>
      </nav>

      {/* Main */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}
