"use client";

interface KpiCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  accent?: string;
}

export default function KpiCard({ title, value, subtitle, icon, accent = "from-purple-500 to-pink-500" }: KpiCardProps) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-purple-500/50 transition-colors">
      <div className="flex items-start justify-between mb-4">
        <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${accent} flex items-center justify-center text-white`}>
          {icon}
        </div>
        {subtitle && (
          <span className="text-xs text-green-400 bg-green-500/10 px-2 py-1 rounded-full">
            {subtitle}
          </span>
        )}
      </div>
      <div className="space-y-1">
        <p className="text-3xl font-bold text-white">{value}</p>
        <p className="text-sm text-gray-500">{title}</p>
      </div>
    </div>
  );
}