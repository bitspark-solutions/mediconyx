'use client'
import { useState } from 'react'
import ProtectedRoute from '../../../components/ProtectedRoute'
import SalesUsersPanel from '../../../components/sales/SalesUsersPanel'
import {
  Sidebar,
  TopBar,
  StatsCards,
  AppointmentRequests,
  TopDepartments,
  RecentPatientsTable,
  LatestAppointmentsTable,
  DashboardFooter
} from '../../../components/dashboard'

export default function MasterPortal() {
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [activeMenu, setActiveMenu] = useState('dashboard')

  const stats = [
    { title: 'Total Patients', count: '1,234', change: '+12%', trend: 'up' as const, icon: '👥' },
    { title: 'Appointments', count: '456', change: '+8%', trend: 'up' as const, icon: '📅' },
    { title: 'Doctors', count: '89', change: '+5%', trend: 'up' as const, icon: '⚕️' },
    { title: 'Revenue', count: '$45,890', change: '+15%', trend: 'up' as const, icon: '💰' },
  ]

  const appointmentRequests = [
    { id: 1, patient: 'John Smith', date: '2025-11-15', time: '10:00 AM', dept: 'Cardiology', status: 'Pending' },
    { id: 2, patient: 'Sarah Johnson', date: '2025-11-15', time: '11:30 AM', dept: 'Neurology', status: 'Confirmed' },
    { id: 3, patient: 'Mike Brown', date: '2025-11-15', time: '02:00 PM', dept: 'Orthopedics', status: 'Pending' },
    { id: 4, patient: 'Emma Davis', date: '2025-11-16', time: '09:00 AM', dept: 'Dermatology', status: 'Confirmed' },
  ]

  const recentPatients = [
    { name: 'Alice Cooper', diagnosis: 'Hypertension', dept: 'Cardiology', lastVisit: '2025-11-14' },
    { name: 'Bob Wilson', diagnosis: 'Migraine', dept: 'Neurology', lastVisit: '2025-11-13' },
    { name: 'Carol White', diagnosis: 'Arthritis', dept: 'Orthopedics', lastVisit: '2025-11-12' },
    { name: 'David Lee', diagnosis: 'Eczema', dept: 'Dermatology', lastVisit: '2025-11-11' },
    { name: 'Eve Martin', diagnosis: 'Asthma', dept: 'Pulmonology', lastVisit: '2025-11-10' },
  ]

  const latestAppointments = [
    { id: 'P001', patient: 'John Smith', type: 'Consultation', doctor: 'Dr. Franklin', dateTime: '2025-11-15 10:00', status: 'In Progress' },
    { id: 'P002', patient: 'Sarah Johnson', type: 'Follow-up', doctor: 'Dr. Wilson', dateTime: '2025-11-15 11:30', status: 'Completed' },
    { id: 'P003', patient: 'Mike Brown', type: 'Check-up', doctor: 'Dr. Cannon', dateTime: '2025-11-15 14:00', status: 'Scheduled' },
    { id: 'P004', patient: 'Emma Davis', type: 'Consultation', doctor: 'Dr. Jackson', dateTime: '2025-11-16 09:00', status: 'Scheduled' },
    { id: 'P005', patient: 'Tom Anderson', type: 'Emergency', doctor: 'Dr. Franklin', dateTime: '2025-11-14 16:00', status: 'Completed' },
  ]

  const departments = [
    { name: 'Cardiology', count: 145, color: 'bg-red-500' },
    { name: 'Neurology', count: 98, color: 'bg-purple-500' },
    { name: 'Dermatology', count: 76, color: 'bg-blue-500' },
    { name: 'Orthopedics', count: 112, color: 'bg-green-500' },
    { name: 'Urology', count: 54, color: 'bg-yellow-500' },
    { name: 'Radiology', count: 89, color: 'bg-pink-500' },
  ]

  return (
    <ProtectedRoute allowedRoles={['Master']}>
      <div className="flex h-screen bg-gray-900 text-gray-100">
        <Sidebar 
          sidebarOpen={sidebarOpen}
          setSidebarOpen={setSidebarOpen}
          activeMenu={activeMenu}
          setActiveMenu={setActiveMenu}
        />

        <div className="flex-1 flex flex-col overflow-hidden">
          <TopBar />

          <main className="flex-1 overflow-y-auto p-6 bg-gray-900">
            <StatsCards stats={stats} />

            <div className="mb-6">
              <SalesUsersPanel />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
              <div className="lg:col-span-2">
                <AppointmentRequests requests={appointmentRequests} />
              </div>
              <TopDepartments departments={departments} />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <RecentPatientsTable patients={recentPatients} />
              <LatestAppointmentsTable appointments={latestAppointments} />
            </div>
          </main>

          <DashboardFooter />
        </div>
      </div>
    </ProtectedRoute>
  )
}
