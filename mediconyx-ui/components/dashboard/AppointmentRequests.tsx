interface AppointmentRequest {
  id: number
  patient: string
  date: string
  time: string
  dept: string
  status: string
}

interface AppointmentRequestsProps {
  requests: AppointmentRequest[]
}

export default function AppointmentRequests({ requests }: AppointmentRequestsProps) {
  return (
    <div className="bg-gray-800 rounded-xl p-6 border border-gray-700">
      <h3 className="text-xl font-bold text-white mb-4">Appointment Requests</h3>
      <div className="space-y-3">
        {requests.map((req) => (
          <div key={req.id} className="flex items-center justify-between p-4 bg-gray-700 rounded-lg hover:bg-gray-600 transition">
            <div className="flex-1">
              <h4 className="font-semibold text-white">{req.patient}</h4>
              <p className="text-sm text-gray-400">{req.dept} · {req.date} at {req.time}</p>
            </div>
            <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
              req.status === 'Confirmed' ? 'bg-green-900 text-green-300' : 'bg-yellow-900 text-yellow-300'
            }`}>
              {req.status}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}
