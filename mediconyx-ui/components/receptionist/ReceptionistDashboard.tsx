"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";
import { Users, Calendar, Clock, CheckCircle2, AlertCircle } from "lucide-react";
import KpiCard from "./ReceptionistKpiCard";

interface DashboardStats {
  totalPatients: number;
  todayAppointments: number;
  waitingPatients: number;
  completedToday: number;
}

interface RecentPatient {
  id: number;
  firstName: string;
  lastName: string;
  phone: string;
  createdAt: string;
}

interface RecentAppointment {
  id: number;
  patientName: string;
  doctorName: string;
  appointmentTime: string;
  status: string;
}

export default function ReceptionistDashboard() {
  const [stats, setStats] = useState<DashboardStats>({
    totalPatients: 0,
    todayAppointments: 0,
    waitingPatients: 0,
    completedToday: 0,
  });
  const [recentPatients, setRecentPatients] = useState<RecentPatient[]>([]);
  const [recentAppointments, setRecentAppointments] = useState<RecentAppointment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    setLoading(true);
    try {
      // Load patients count
      const patientsRes = await apiClient.get("/api/patient?pageSize=100");
      if (patientsRes.ok) {
        const patientsData = await patientsRes.json();
        setStats(prev => ({ ...prev, totalPatients: patientsData.totalCount || 0 }));
      }

      // Load today's appointments
      const today = new Date().toISOString().split("T")[0];
      const appointmentsRes = await apiClient.get(`/api/appointment?date=${today}&pageSize=50`);
      if (appointmentsRes.ok) {
        const apptData = await appointmentsRes.json();
        const appointments = apptData.items || [];
        setStats(prev => ({
          ...prev,
          todayAppointments: apptData.totalCount || 0,
          waitingPatients: appointments.filter((a: any) => a.status === "CheckedIn" || a.status === "Scheduled").length,
          completedToday: appointments.filter((a: any) => a.status === "Completed").length,
        }));
        setRecentAppointments(appointments.slice(0, 5).map((a: any) => ({
          id: a.id,
          patientName: a.patientName || "Unknown",
          doctorName: a.doctorName || "TBD",
          appointmentTime: a.appointmentTime,
          status: a.status,
        })));
      }

      // Load recent patients
      const recentRes = await apiClient.get("/api/patient?sortBy=createdAt&sortOrder=desc&pageSize=5");
      if (recentRes.ok) {
        const recentData = await recentRes.json();
        setRecentPatients((recentData.items || []).map((p: any) => ({
          id: p.id,
          firstName: p.firstName,
          lastName: p.lastName,
          phone: p.phone || "N/A",
          createdAt: p.createdAt,
        })));
      }
    } catch (err) {
      console.error("Failed to load dashboard data:", err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          title="Total Patients"
          value={stats.totalPatients}
          icon={<Users className="w-6 h-6" />}
          accent="from-blue-500 to-cyan-500"
        />
        <KpiCard
          title="Today's Appointments"
          value={stats.todayAppointments}
          icon={<Calendar className="w-6 h-6" />}
          accent="from-purple-500 to-pink-500"
        />
        <KpiCard
          title="Waiting"
          value={stats.waitingPatients}
          icon={<Clock className="w-6 h-6" />}
          accent="from-amber-500 to-orange-500"
        />
        <KpiCard
          title="Completed Today"
          value={stats.completedToday}
          icon={<CheckCircle2 className="w-6 h-6" />}
          accent="from-emerald-500 to-green-500"
        />
      </div>

      {/* Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Patients */}
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-white">Recent Patients</h3>
            <Users className="w-5 h-5 text-gray-500" />
          </div>
          {recentPatients.length === 0 ? (
            <p className="text-gray-500 text-center py-6">No recent patients</p>
          ) : (
            <div className="space-y-3">
              {recentPatients.map((patient) => (
                <div key={patient.id} className="flex items-center justify-between py-2 border-b border-gray-800 last:border-0">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-full bg-purple-600/20 flex items-center justify-center text-purple-400 text-sm font-medium">
                      {patient.firstName[0]}{patient.lastName[0]}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-white">{patient.firstName} {patient.lastName}</p>
                      <p className="text-xs text-gray-500">{patient.phone}</p>
                    </div>
                  </div>
                  <p className="text-xs text-gray-500">
                    {new Date(patient.createdAt).toLocaleDateString()}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Recent Appointments */}
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-white">Today&apos;s Appointments</h3>
            <Calendar className="w-5 h-5 text-gray-500" />
          </div>
          {recentAppointments.length === 0 ? (
            <p className="text-gray-500 text-center py-6">No appointments today</p>
          ) : (
            <div className="space-y-3">
              {recentAppointments.map((appt) => (
                <div key={appt.id} className="flex items-center justify-between py-2 border-b border-gray-800 last:border-0">
                  <div>
                    <p className="text-sm font-medium text-white">{appt.patientName}</p>
                    <p className="text-xs text-gray-500">Dr. {appt.doctorName} • {new Date(appt.appointmentTime).toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" })}</p>
                  </div>
                  <StatusBadge status={appt.status} />
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const config: Record<string, { bg: string; text: string }> = {
    Scheduled: { bg: "bg-blue-500/10", text: "text-blue-400" },
    CheckedIn: { bg: "bg-amber-500/10", text: "text-amber-400" },
    InProgress: { bg: "bg-purple-500/10", text: "text-purple-400" },
    Completed: { bg: "bg-green-500/10", text: "text-green-400" },
    Cancelled: { bg: "bg-red-500/10", text: "text-red-400" },
    NoShow: { bg: "bg-gray-500/10", text: "text-gray-400" },
  };
  const style = config[status] || { bg: "bg-gray-500/10", text: "text-gray-400" };
  return (
    <span className={`text-xs px-2 py-1 rounded-full ${style.bg} ${style.text}`}>
      {status}
    </span>
  );
}