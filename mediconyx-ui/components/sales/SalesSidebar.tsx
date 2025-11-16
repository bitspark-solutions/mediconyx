"use client";
import Link from "next/link";

export default function SalesSidebar() {
  const items = [
    { href: "/portal/sales", label: "Dashboard", icon: "/icons/dashboard.svg" },
    { href: "/portal/sales#requests", label: "Requests", icon: "/icons/requests.svg" },
    { href: "/portal/sales#metrics", label: "Metrics", icon: "/icons/metrics.svg" },
    { href: "/portal/sales#contacts", label: "Contacts", icon: "/icons/contacts.svg" },
  ];
  return (
    <aside className="fixed left-0 top-0 z-40 h-screen w-64 surface">
      <div className="flex items-center gap-2 px-4 py-4">
        <div className="h-8 w-8 rounded bg-gradient-to-br from-indigo-500 to-blue-500" />
        <span className="text-white font-semibold">Sales</span>
      </div>
      <nav className="mt-2 space-y-1">
        {items.map((it) => (
          <Link key={it.href} href={it.href} className="flex items-center gap-3 px-4 py-2 text-sm text-secondary surface-hover hover:text-white">
            <img src={it.icon} alt="" className="h-5 w-5" />
            <span>{it.label}</span>
          </Link>
        ))}
      </nav>
    </aside>
  );
}
