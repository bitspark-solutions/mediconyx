"use client";

export default function KpiCard({
  title,
  value,
  icon,
  accent = "from-indigo-500 to-blue-500",
}: {
  title: string;
  value: number | string;
  icon?: React.ReactNode;
  accent?: string;
}) {
  return (
    <div className="rounded-xl surface p-5">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-xs uppercase tracking-wide text-muted">{title}</div>
          <div className="mt-1 text-3xl font-semibold">{value}</div>
        </div>
        <div className={`h-12 w-12 rounded-xl accent-gradient grid place-items-center text-white`}>{icon}</div>
      </div>
    </div>
  );
}
