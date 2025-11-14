interface Department {
  name: string
  count: number
  color: string
}

interface TopDepartmentsProps {
  departments: Department[]
}

export default function TopDepartments({ departments }: TopDepartmentsProps) {
  return (
    <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
      <h3 className="text-xl font-bold text-white mb-4">Top Departments</h3>
      <div className="space-y-3">
        {departments.map((dept, idx) => (
          <div key={idx} className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 rounded-full ${dept.color}`}></div>
              <span className="text-gray-300">{dept.name}</span>
            </div>
            <span className="text-white font-semibold">{dept.count}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
