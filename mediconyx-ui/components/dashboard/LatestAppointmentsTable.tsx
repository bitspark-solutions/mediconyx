interface Appointment {
  id: string
  patient: string
  type: string
  doctor: string
  dateTime: string
  status: string
}

interface LatestAppointmentsProps {
  appointments: Appointment[]
}

export default function LatestAppointmentsTable({ appointments }: LatestAppointmentsProps) {
  return (
    <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
      <h3 className="text-xl font-bold text-white mb-4">Latest Appointments</h3>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-700">
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Patient</th>
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Doctor</th>
              <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Status</th>
            </tr>
          </thead>
          <tbody>
            {appointments.map((apt, idx) => (
              <tr key={idx} className="border-b border-gray-700 hover:bg-gray-700 transition">
                <td className="py-3 px-4">
                  <a href="#" className="text-blue-400 hover:underline font-medium">{apt.patient}</a>
                  <p className="text-xs text-gray-500">{apt.type}</p>
                </td>
                <td className="py-3 px-4">
                  <span className="text-gray-300">{apt.doctor}</span>
                  <p className="text-xs text-gray-500">{apt.dateTime}</p>
                </td>
                <td className="py-3 px-4">
                  <span className={`px-2 py-1 rounded-full text-xs font-semibold ${
                    apt.status === 'Completed' ? 'bg-green-900 text-green-300' :
                    apt.status === 'In Progress' ? 'bg-blue-900 text-blue-300' :
                    'bg-yellow-900 text-yellow-300'
                  }`}>
                    {apt.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
