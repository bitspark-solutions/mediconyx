"use client";

import { AlertTriangle, Bell, HelpCircle, Hospital, LogOut, Search, ShieldAlert } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useAuthStore } from "../../store/useAuthStore";

type ClinicalNavItem = {
  id: string;
  label: string;
  icon: LucideIcon;
  isAi?: boolean;
};

type ClinicalPortalLayoutProps = {
  portalLabel: string;
  activeItem: string;
  navItems: ClinicalNavItem[];
  onNavigate: (itemId: string) => void;
  children: React.ReactNode;
  searchPlaceholder?: string;
  primaryActionLabel?: string;
  onPrimaryAction?: () => void;
  showEmergencyAction?: boolean;
};

export default function ClinicalPortalLayout({
  portalLabel,
  activeItem,
  navItems,
  onNavigate,
  children,
  searchPlaceholder = "Search patients, records...",
  primaryActionLabel = "New Admission",
  onPrimaryAction,
  showEmergencyAction = true,
}: ClinicalPortalLayoutProps) {
  const { user, logout } = useAuthStore();
  const initials = `${user?.firstName?.[0] ?? ""}${user?.lastName?.[0] ?? ""}`.trim() || "M";

  return (
    <main className="clinical-shell min-h-screen text-slate-900">
      <nav className="fixed left-0 top-0 z-50 flex h-screen w-64 flex-col border-r border-slate-200 bg-slate-50 py-6 text-sm font-medium text-blue-700">
        <div className="mb-8 flex items-center gap-3 px-6">
          <div className="flex h-8 w-8 items-center justify-center rounded bg-blue-800 text-blue-100">
            <Hospital className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-xl font-black text-blue-950">Mediconyx</h1>
            <p className="text-xs text-slate-500">{portalLabel}</p>
          </div>
        </div>

        <div className="mb-6 px-4">
          <button
            type="button"
            onClick={onPrimaryAction}
            className="flex w-full items-center justify-center gap-2 rounded bg-blue-800 px-4 py-2.5 font-semibold text-blue-100 transition-colors hover:bg-blue-900"
          >
            <ShieldAlert className="h-4 w-4" />
            {primaryActionLabel}
          </button>
        </div>

        <ul className="flex-1 space-y-1 overflow-y-auto px-4">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeItem === item.id;
            return (
              <li key={item.id}>
                <button
                  type="button"
                  onClick={() => onNavigate(item.id)}
                  className={`flex w-full items-center gap-3 rounded-l px-4 py-3 text-left transition-colors active:opacity-80 ${
                    isActive
                      ? "border-r-4 border-blue-700 bg-white font-bold text-blue-700 shadow-sm"
                      : "rounded-r text-slate-600 hover:bg-white/70 hover:text-blue-700"
                  }`}
                >
                  <Icon className={`h-5 w-5 ${item.isAi ? "text-purple-700" : ""}`} />
                  <span className={item.isAi ? "bg-gradient-to-r from-blue-700 to-purple-700 bg-clip-text text-transparent" : ""}>
                    {item.label}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>

        <div className="mt-auto space-y-1 border-t border-slate-200 px-4 pt-4">
          <button
            type="button"
            className="flex w-full items-center gap-3 rounded px-4 py-3 text-left text-slate-600 transition-colors hover:bg-white/70 hover:text-blue-700 active:opacity-80"
          >
            <HelpCircle className="h-5 w-5" />
            Support
          </button>
          <button
            type="button"
            onClick={logout}
            className="flex w-full items-center gap-3 rounded px-4 py-3 text-left text-slate-600 transition-colors hover:bg-white/70 hover:text-blue-700 active:opacity-80"
          >
            <LogOut className="h-5 w-5" />
            Logout
          </button>
        </div>
      </nav>

      <div className="ml-64 flex h-screen flex-col overflow-hidden bg-[var(--clinical-background)]">
        <header className="sticky top-0 z-40 flex h-16 w-full items-center justify-between border-b border-slate-200 bg-white/95 px-6 text-sm tracking-tight text-blue-800 backdrop-blur-sm">
          <div className="flex w-1/3 items-center">
            <div className="relative w-full max-w-md">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                className="w-full rounded border border-slate-200 bg-slate-50 py-2 pl-10 pr-4 text-sm text-slate-700 outline-none transition-all placeholder:text-slate-400 focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                placeholder={searchPlaceholder}
                type="text"
              />
            </div>
          </div>

          <div className="flex w-1/3 items-center justify-center" />

          <div className="flex w-1/3 items-center justify-end gap-4">
            {showEmergencyAction && (
              <button
                type="button"
                className="flex items-center gap-2 rounded bg-red-100 px-3 py-1.5 text-xs font-semibold text-red-900 transition-colors hover:bg-red-200"
              >
                <AlertTriangle className="h-4 w-4" />
                Emergency Alert
              </button>
            )}
            <div className="flex items-center gap-1 border-l border-slate-200 pl-4">
              <button type="button" className="rounded-full p-2 text-slate-500 transition-colors hover:bg-slate-50">
                <Bell className="h-5 w-5" />
              </button>
              <button type="button" className="rounded-full p-2 text-slate-500 transition-colors hover:bg-slate-50">
                <ShieldAlert className="h-5 w-5" />
              </button>
              <button type="button" className="rounded-full p-2 text-slate-500 transition-colors hover:bg-slate-50">
                <HelpCircle className="h-5 w-5" />
              </button>
            </div>
            <div className="flex h-8 w-8 items-center justify-center overflow-hidden rounded-full border border-slate-200 bg-slate-100 text-xs font-bold text-blue-900">
              {initials}
            </div>
          </div>
        </header>

        <section className="clinical-grid-bg flex-1 overflow-auto p-6">
          <div className="relative z-10 mx-auto max-w-7xl">{children}</div>
        </section>
      </div>
    </main>
  );
}

export type { ClinicalNavItem };
