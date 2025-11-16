"use client";
import { useAuthStore } from "../../store/useAuthStore";

export default function SalesTopbar({ title }: { title: string }) {
  const { user } = useAuthStore();
  return (
    <header className="fixed left-64 right-0 top-0 z-30 h-16 surface">
      <div className="mx-auto flex h-full items-center justify-between px-6">
        <div className="flex items-center gap-3">
          <h1 className="text-white text-lg font-semibold">{title}</h1>
          <div className="hidden md:flex items-center gap-2 rounded-lg surface px-3 py-1 text-sm text-secondary">
            <img src="/icons/search.svg" alt="" className="h-4 w-4" />
            <input className="bg-transparent outline-none placeholder:text-gray-500" placeholder="Search..." />
          </div>
        </div>
        <div className="flex items-center gap-4">
          <button className="rounded-lg surface px-3 py-2 text-sm text-secondary">This Month</button>
          <img src="/icons/bell.svg" alt="" className="h-5 w-5 text-white" />
          <img src="/icons/settings.svg" alt="" className="h-5 w-5 text-white" />
          <div className="flex items-center gap-2">
            <img src="/icons/user.svg" alt="" className="h-6 w-6" />
            <div className="text-sm text-gray-300">{user ? `${user.firstName} ${user.lastName}` : "Guest"}</div>
          </div>
        </div>
      </div>
    </header>
  );
}
