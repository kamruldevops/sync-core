import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';

const primaryNav = [
  {
    to: '/',
    end: true,
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M21 3H3v18h18V3zm-9 14l-5-5 1.41-1.41L12 14.17l7.59-7.59L21 8l-9 9z" opacity="0" />
        <path d="M22 16V4c0-1.1-.9-2-2-2H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-2 2-2zm-11-4l2.03 2.71L16 11l4 5H8l3-4zM2 6v14c0 1.1.9 2 2 2h14v-2H4V6H2z" />
      </svg>
    ),
    label: 'Photos',
  },
];

const collectionsNav = [
  {
    to: '/albums',
    end: false,
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M22 16V4c0-1.1-.9-2-2-2H8c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h12c1.1 0 2-2 2-2zm-11-4 2.03 2.71L16 11l4 5H8l3-4zM2 6v14c0 1.1.9 2 2 2h14v-2H4V6H2z" />
      </svg>
    ),
    label: 'Albums',
  },
  {
    to: '/favourites',
    end: false,
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
      </svg>
    ),
    label: 'Favourites',
  },
  {
    to: '/trash',
    end: false,
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
        <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z" />
      </svg>
    ),
    label: 'Trash',
  },
];

function NavItem({
  to,
  end,
  icon,
  label,
}: {
  to: string;
  end: boolean;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        `flex items-center gap-4 rounded-full px-4 py-2.5 text-sm font-medium transition-colors ${
          isActive
            ? 'bg-blue-100 text-blue-800'
            : 'text-gray-700 hover:bg-gray-100'
        }`
      }
    >
      {icon}
      {label}
    </NavLink>
  );
}

export function AppLayout() {
  const [searchValue, setSearchValue] = useState('');
  const navigate = useNavigate();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchValue.trim()) {
      navigate(`/?q=${encodeURIComponent(searchValue.trim())}`);
    } else {
      navigate('/');
    }
  };

  return (
    <div className="flex h-screen overflow-hidden bg-white">
      {/* Sidebar */}
      <nav className="hidden md:flex flex-col w-64 shrink-0 pt-4 pb-6 px-3 overflow-y-auto">
        {/* Logo */}
        <div className="flex items-center gap-2 px-4 mb-4">
          <svg viewBox="0 0 192 192" className="w-8 h-8" xmlns="http://www.w3.org/2000/svg">
            <circle cx="96" cy="48" r="40" fill="#EA4335" />
            <circle cx="144" cy="128" r="40" fill="#FBBC05" />
            <circle cx="48" cy="128" r="40" fill="#34A853" />
            <circle cx="96" cy="96" r="28" fill="#4285F4" />
          </svg>
          <span className="text-lg font-normal text-gray-800 tracking-tight">SyncCore Photos</span>
        </div>

        {/* Primary nav */}
        <div className="flex flex-col gap-0.5 mb-4">
          {primaryNav.map((item) => (
            <NavItem key={item.to} {...item} />
          ))}
        </div>

        {/* Collections */}
        <p className="px-4 text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1 mt-2">
          Collections
        </p>
        <div className="flex flex-col gap-0.5">
          {collectionsNav.map((item) => (
            <NavItem key={item.to} {...item} />
          ))}
        </div>

        {/* Storage footer */}
        <div className="mt-auto pt-4 px-4">
          <div className="text-xs text-gray-500 space-y-1">
            <div className="flex items-center gap-1.5 text-gray-600">
              <svg viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4 text-gray-400">
                <path d="M19.35 10.04A7.49 7.49 0 0 0 12 4C9.11 4 6.6 5.64 5.35 8.04A5.994 5.994 0 0 0 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96z" />
              </svg>
              <span>Azure Blob Storage</span>
            </div>
          </div>
          <div className="mt-3 flex gap-3 text-xs text-gray-400">
            <span className="hover:underline cursor-pointer">Privacy</span>
            <span>·</span>
            <span className="hover:underline cursor-pointer">Terms</span>
          </div>
        </div>
      </nav>

      {/* Right panel: topbar + content */}
      <div className="flex flex-col flex-1 min-w-0">
        {/* Top bar */}
        <header className="flex items-center gap-3 px-4 py-2 shrink-0">
          {/* Search */}
          <form onSubmit={handleSearch} className="flex-1 max-w-2xl">
            <div className="flex items-center gap-3 bg-gray-100 rounded-full px-5 py-2.5 hover:bg-gray-200 transition-colors focus-within:bg-white focus-within:shadow-md focus-within:ring-1 focus-within:ring-gray-200">
              <svg viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5 text-gray-500 shrink-0">
                <path d="M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
              </svg>
              <input
                type="text"
                placeholder="Search your photos and albums"
                value={searchValue}
                onChange={(e) => setSearchValue(e.target.value)}
                className="bg-transparent text-sm text-gray-800 placeholder-gray-500 outline-none w-full"
              />
            </div>
          </form>

          {/* Right actions */}
          <div className="flex items-center gap-1 ml-auto shrink-0">
            {/* Upload + button */}
            <button
              className="flex items-center justify-center w-10 h-10 rounded-full hover:bg-gray-100 text-gray-600"
              onClick={() => document.getElementById('global-upload-input')?.click()}
              title="Upload photos"
            >
              <svg viewBox="0 0 24 24" fill="currentColor" className="w-6 h-6">
                <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
              </svg>
            </button>
            {/* Avatar */}
            <div className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-white text-sm font-medium ml-1">
              D
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
