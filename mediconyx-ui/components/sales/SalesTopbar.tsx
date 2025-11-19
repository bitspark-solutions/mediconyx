"use client";
import { useRouter } from "next/navigation";
import Image from "next/image";
import { useAuthStore } from "../../store/useAuthStore";

export default function SalesTopbar({ title }: { title: string }) {
  const router = useRouter();
  const { user, logout } = useAuthStore();
  
  const handleLogout = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765";
    const { accessToken } = useAuthStore.getState();
    if (accessToken) {
      await fetch(`${baseUrl}/api/auth/logout`, { 
        headers: { Authorization: `Bearer ${accessToken}` }, 
        method: "POST" 
      });
    }
    logout();
    router.push("/login");
  };
  return (
    <header className="fixed left-64 right-0 top-0 z-30 h-16 surface">
      <div className="mx-auto flex h-full items-center justify-between px-6">
        <div className="flex items-center gap-3">
          <h1 className="text-white text-lg font-semibold">{title}</h1>
          <div className="hidden md:flex items-center gap-2 rounded-lg surface px-3 py-1 text-sm text-secondary">
            <Image src="/icons/search.svg" alt="Search" width={16} height={16} />
            <input className="bg-transparent outline-none placeholder:text-gray-500" placeholder="Search..." />
          </div>
        </div>
        <div className="flex items-center gap-4">
          <button className="rounded-lg surface px-3 py-2 text-sm text-secondary">This Month</button>
          <Image src="/icons/bell.svg" alt="Notifications" width={20} height={20} />
          <Image src="/icons/settings.svg" alt="Settings" width={20} height={20} />
          <div className="flex items-center gap-3">
            <Image src="/icons/user.svg" alt="User profile" width={24} height={24} />
            <div className="text-sm text-gray-300">{user ? `${user.firstName} ${user.lastName}` : "Guest"}</div>
            <button 
              onClick={handleLogout}
              className="ml-2 rounded-lg bg-red-600 hover:bg-red-700 px-4 py-2 text-sm text-white transition"
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
