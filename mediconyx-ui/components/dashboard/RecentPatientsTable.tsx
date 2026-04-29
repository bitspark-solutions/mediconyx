interface Patient {
  name: string
  diagnosis: string
  dept: string
  lastVisit: string
}

interface RecentPatientsProps {
  patients: Patient[]
}

export default function RecentPatientsTable({ patients }: RecentPatientsProps) {
  return (
    <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
      <h3 className="text-xl font-bold text-white mb-4">Recent Patients</h3>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-700">
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Patient</th>
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Diagnosis</th>
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Last Visit</th>
            </tr>
          </thead>
          <tbody>
            {patients.map((patient, idx) => (
              <tr key={idx} className="border-b border-gray-700 hover:bg-gray-700 transition">
                <td className="py-3 px-4">
                  <a href="#" className="text-blue-400 hover:underline font-medium">{patient.name}</a>
                  <p className="text-xs text-gray-500">{patient.dept}</p>
                </td>
                <td className="py-3 px-4 text-gray-300">{patient.diagnosis}</td>
                <td className="py-3 px-4 text-gray-400 text-sm">{patient.lastVisit}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
