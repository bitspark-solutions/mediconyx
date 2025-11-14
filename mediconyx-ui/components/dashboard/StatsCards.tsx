interface Stat {
  title: string
  count: string
  change: string
  trend: 'up' | 'down'
  icon: string
}

interface StatsCardsProps {
  stats: Stat[]
}

export default function StatsCards({ stats }: StatsCardsProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
      {stats.map((stat, idx) => (
        <div key={idx} className="bg-gray-800 rounded-xl p-6 border border-gray-700 hover:border-blue-500 transition">
          <div className="flex items-center justify-between mb-4">
            <div className="text-4xl">{stat.icon}</div>
            <span className={`text-xs font-semibold px-2 py-1 rounded-full ${
              stat.trend === 'up' ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'
            }`}>
              {stat.change}
            </span>
          </div>
          <h3 className="text-gray-400 text-sm mb-1">{stat.title}</h3>
          <p className="text-3xl font-bold text-white">{stat.count}</p>
        </div>
      ))}
    </div>
  )
}
