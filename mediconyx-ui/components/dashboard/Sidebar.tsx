import { useAuthStore } from '../../store/useAuthStore'

interface SidebarProps {
  sidebarOpen: boolean
  setSidebarOpen: (open: boolean) => void
  activeMenu: string
  setActiveMenu: (menu: string) => void
}

export default function Sidebar({ sidebarOpen, setSidebarOpen, activeMenu, setActiveMenu }: SidebarProps) {
  const logout = useAuthStore((state) => state.logout)
  return (
    <aside className={`${
      sidebarOpen ? 'w-64' : 'w-20'
    } bg-gray-800 transition-all duration-300 flex flex-col border-r border-gray-700`}>
      <div className="p-4 border-b border-gray-700">
        <div className="flex items-center justify-between">
          {sidebarOpen && <h1 className="text-xl font-bold text-blue-400">Mediconyx</h1>}
          <button onClick={() => setSidebarOpen(!sidebarOpen)} className="text-gray-400 hover:text-white">
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        </div>
      </div>
      
      <nav className="flex-1 overflow-y-auto py-4">
        <div className="px-3 mb-4">
          {sidebarOpen && <p className="text-xs font-semibold text-gray-500 uppercase">Main</p>}
          <button onClick={() => setActiveMenu('dashboard')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'dashboard' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            </svg>
            {sidebarOpen && <span>Dashboard</span>}
          </button>
        </div>

        <div className="px-3 mb-4">
          {sidebarOpen && <p className="text-xs font-semibold text-gray-500 uppercase">Healthcare</p>}
          <button onClick={() => setActiveMenu('patients')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'patients' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            {sidebarOpen && <span>Patients</span>}
          </button>
          <button onClick={() => setActiveMenu('doctors')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'doctors' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            {sidebarOpen && <span>Doctors</span>}
          </button>
          <button onClick={() => setActiveMenu('appointments')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'appointments' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            {sidebarOpen && <span>Appointments</span>}
          </button>
        </div>

        <div className="px-3 mb-4">
          {sidebarOpen && <p className="text-xs font-semibold text-gray-500 uppercase">Manage</p>}
          <button onClick={() => setActiveMenu('staff')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'staff' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
            {sidebarOpen && <span>Staff</span>}
          </button>
          <button onClick={() => setActiveMenu('settings')} className={`w-full flex items-center gap-3 px-3 py-2 mt-2 rounded-lg ${
            activeMenu === 'settings' ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-700'
          }`}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            {sidebarOpen && <span>Settings</span>}
          </button>
        </div>
      </nav>

      <div className="p-4 border-t border-gray-700">
        <button onClick={logout} className="flex items-center gap-3 px-3 py-2 text-gray-400 hover:bg-gray-700 rounded-lg w-full">
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
          </svg>
          {sidebarOpen && <span>Logout</span>}
        </button>
      </div>
    </aside>
  )
}
