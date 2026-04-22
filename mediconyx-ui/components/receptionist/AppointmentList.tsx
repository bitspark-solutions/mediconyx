"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";
import { Calendar, Clock, User, Search, ChevronLeft, ChevronRight, CheckCircle2, XCircle, X } from "lucide-react";

interface Appointment {
  id: number;
  patientId: number;
  patientName: string;
  doctorId: number;
  doctorName: string;
  appointmentTime: string;
  reason: string;
  status: string;
  checkedInAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
}

interface AppointmentQueryParams {
  page?: number;
  pageSize?: number;
  status?: string;
  date?: string;
  search?: string;
  sortBy?: string;
  sortOrder?: string;
}

const STATUS_CONFIG: Record<string, { label: string; bg: string; text: string; icon: string }> = {
  Scheduled: { label: "Scheduled", bg: "bg-blue-500/10", text: "text-blue-400", icon: "clock" },
  CheckedIn: { label: "Checked In", bg: "bg-amber-500/10", text: "text-amber-400", icon: "user" },
  InProgress: { label: "In Progress", bg: "bg-purple-500/10", text: "text-purple-400", icon: "activity" },
  Completed: { label: "Completed", bg: "bg-green-500/10", text: "text-green-400", icon: "check" },
  Cancelled: { label: "Cancelled", bg: "bg-red-500/10", text: "text-red-400", icon: "x" },
  NoShow: { label: "No Show", bg: "bg-gray-500/10", text: "text-gray-400", icon: "xcircle" },
};

export default function AppointmentList() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [filterStatus, setFilterStatus] = useState("");
  const [filterDate, setFilterDate] = useState(new Date().toISOString().split("T")[0]);
  const [search, setSearch] = useState("");
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState<Appointment | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    loadAppointments();
  }, [page, filterStatus, filterDate]);

  const loadAppointments = async () => {
    setLoading(true);
    try {
      const params: AppointmentQueryParams = {
        page,
        pageSize: 10,
        sortBy: "appointmentTime",
        sortOrder: "asc",
      };
      if (filterStatus) params.status = filterStatus;
      if (filterDate) params.date = filterDate;
      if (search) params.search = search;

      const queryString = new URLSearchParams(
        Object.entries(params).filter(([, v]) => v !== undefined).map(([k, v]) => [k, String(v)])
      ).toString();

      const res = await apiClient.get(`/api/appointment?${queryString}`);
      if (res.ok) {
        const data = await res.json();
        setAppointments(data.items || []);
        setTotalCount(data.totalCount || 0);
        setTotalPages(data.totalPages || 1);
      }
    } catch (err) {
      console.error("Failed to load appointments:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    loadAppointments();
  };

  const handleCheckIn = async (appointment: Appointment) => {
    try {
      const res = await apiClient.patch(`/api/appointment/${appointment.id}/status`, { status: "CheckedIn" });
      if (res.ok) {
        loadAppointments();
      }
    } catch (err) {
      console.error("Check-in failed:", err);
    }
  };

  const handleCheckInBatch = async (ids: number[]) => {
    for (const id of ids) {
      await apiClient.patch(`/api/appointment/${id}/status`, { status: "CheckedIn" });
    }
    loadAppointments();
  };

  const handleCancel = async () => {
    if (!selectedAppointment || !cancelReason.trim()) return;
    setCancelling(true);
    try {
      const res = await apiClient.post(`/api/appointment/${selectedAppointment.id}/cancel`, {
        cancelReason,
      });
      if (res.ok) {
        setShowCancelModal(false);
        setSelectedAppointment(null);
        setCancelReason("");
        loadAppointments();
      }
    } catch (err) {
      console.error("Cancel failed:", err);
    } finally {
      setCancelling(false);
    }
  };

  const getStatusConfig = (status: string) => STATUS_CONFIG[status] || STATUS_CONFIG.Scheduled;

  const scheduledCount = appointments.filter((a) => a.status === "Scheduled").length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-white">Appointment Management</h2>
          <p className="text-sm text-gray-500 mt-1">View and manage patient appointments</p>
        </div>
        {scheduledCount > 0 && (
          <button
            onClick={() => {
              const ids = appointments.filter((a) => a.status === "Scheduled").map((a) => a.id);
              handleCheckInBatch(ids);
            }}
            className="flex items-center gap-2 px-4 py-2 bg-green-600 hover:bg-green-500 text-white rounded-xl text-sm font-medium transition-colors"
          >
            <CheckCircle2 className="w-4 h-4" /> Batch Check-In ({scheduledCount})
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <form onSubmit={handleSearch} className="flex gap-2 flex-1 min-w-[300px]">
          <div className="relative flex-1 max-w-xs">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search patient name..."
              className="w-full pl-9 pr-4 py-2 bg-gray-900 border border-gray-800 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-purple-500 text-sm"
            />
          </div>
          <button
            type="submit"
            className="px-3 py-2 bg-gray-800 border border-gray-700 rounded-xl text-gray-300 text-sm hover:bg-gray-700 transition-colors"
          >
            Search
          </button>
        </form>

        <select
          value={filterStatus}
          onChange={(e) => { setFilterStatus(e.target.value); setPage(1); loadAppointments(); }}
          className="px-3 py-2 bg-gray-900 border border-gray-800 rounded-xl text-white text-sm focus:outline-none focus:border-purple-500"
        >
          <option value="">All Status</option>
          <option value="Scheduled">Scheduled</option>
          <option value="CheckedIn">Checked In</option>
          <option value="InProgress">In Progress</option>
          <option value="Completed">Completed</option>
          <option value="Cancelled">Cancelled</option>
          <option value="NoShow">No Show</option>
        </select>

        <input
          type="date"
          value={filterDate}
          onChange={(e) => { setFilterDate(e.target.value); setPage(1); loadAppointments(); }}
          className="px-3 py-2 bg-gray-900 border border-gray-800 rounded-xl text-white text-sm focus:outline-none focus:border-purple-500"
        />
      </div>

      {/* Stats Bar */}
      <div className="flex items-center gap-6 text-sm">
        <span className="text-gray-500">Total: {totalCount} appointments</span>
        <span className="text-gray-500">•</span>
        <span className="text-gray-500">Page {page} of {totalPages}</span>
      </div>

      {/* Appointments Table */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        {loading ? (
          <div className="flex justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
          </div>
        ) : appointments.length === 0 ? (
          <div className="text-center py-12">
            <Calendar className="w-12 h-12 text-gray-600 mx-auto mb-3" />
            <p className="text-gray-500">No appointments found</p>
          </div>
        ) : (
          <table className="w-full text-left">
            <thead>
              <tr className="border-b border-gray-800 bg-gray-800/50">
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase">Patient</th>
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase">Doctor</th>
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase">Time</th>
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase">Status</th>
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase">Reason</th>
                <th className="px-4 py-3 text-xs font-medium text-gray-400 uppercase text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-800">
              {appointments.map((appt) => {
                const statusCfg = getStatusConfig(appt.status);
                return (
                  <tr key={appt.id} className="hover:bg-gray-800/50 transition-colors">
                    <td className="px-4 py-3">
                      <p className="text-sm font-medium text-white">{appt.patientName || "Unknown"}</p>
                      <p className="text-xs text-gray-500">ID: {appt.patientId}</p>
                    </td>
                    <td className="px-4 py-3">
                      <p className="text-sm text-gray-300">Dr. {appt.doctorName || "TBD"}</p>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1.5 text-sm text-gray-300">
                        <Clock className="w-3.5 h-3.5 text-gray-500" />
                        {new Date(appt.appointmentTime).toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" })}
                      </div>
                      <p className="text-xs text-gray-500 mt-0.5">
                        {new Date(appt.appointmentTime).toLocaleDateString()}
                      </p>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-1 rounded-full ${statusCfg.bg} ${statusCfg.text}`}>
                        {statusCfg.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <p className="text-sm text-gray-400 truncate max-w-[150px]">{appt.reason || "—"}</p>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        {appt.status === "Scheduled" && (
                          <>
                            <button
                              onClick={() => handleCheckIn(appt)}
                              className="px-2.5 py-1 bg-green-500/10 text-green-400 rounded-lg text-xs font-medium hover:bg-green-500/20 transition-colors"
                              title="Check In"
                            >
                              Check In
                            </button>
                            <button
                              onClick={() => { setSelectedAppointment(appt); setShowCancelModal(true); }}
                              className="p-1.5 rounded-lg text-gray-400 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                              title="Cancel"
                            >
                              <XCircle className="w-4 h-4" />
                            </button>
                          </>
                        )}
                        {appt.status === "CheckedIn" && (
                          <button
                            onClick={() => handleCheckIn(appt)}
                            className="px-2.5 py-1 bg-purple-500/10 text-purple-400 rounded-lg text-xs font-medium hover:bg-purple-500/20 transition-colors"
                          >
                            Start Visit
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <button
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page === 1}
            className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-white text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-700 transition-colors"
          >
            Previous
          </button>
          <span className="px-3 py-1.5 text-gray-400 text-sm">Page {page} of {totalPages}</span>
          <button
            onClick={() => setPage(Math.min(totalPages, page + 1))}
            disabled={page === totalPages}
            className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-white text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-700 transition-colors"
          >
            Next
          </button>
        </div>
      )}

      {/* Cancel Modal */}
      {showCancelModal && selectedAppointment && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-gray-900 border border-gray-800 rounded-2xl w-full max-w-md">
            <div className="flex items-center justify-between p-6 border-b border-gray-800">
              <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                <XCircle className="w-5 h-5 text-red-400" /> Cancel Appointment
              </h3>
              <button
                onClick={() => { setShowCancelModal(false); setSelectedAppointment(null); setCancelReason(""); }}
                className="text-gray-400 hover:text-white"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div className="bg-gray-800 rounded-xl p-4">
                <p className="text-sm text-gray-400">Cancelling appointment for</p>
                <p className="text-white font-medium mt-1">{selectedAppointment.patientName}</p>
                <p className="text-gray-500 text-sm mt-1">
                  {new Date(selectedAppointment.appointmentTime).toLocaleString()}
                </p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">Reason for Cancellation *</label>
                <textarea
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  rows={3}
                  placeholder="Please provide a reason for cancellation..."
                  className="w-full px-4 py-2.5 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-red-500 resize-none"
                />
              </div>
              <div className="flex items-center justify-end gap-3">
                <button
                  onClick={() => { setShowCancelModal(false); setSelectedAppointment(null); setCancelReason(""); }}
                  className="px-4 py-2 bg-gray-800 border border-gray-700 rounded-xl text-gray-300 text-sm hover:bg-gray-700 transition-colors"
                >
                  Keep Appointment
                </button>
                <button
                  onClick={handleCancel}
                  disabled={!cancelReason.trim() || cancelling}
                  className="px-4 py-2 bg-red-600 hover:bg-red-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl text-sm font-medium transition-colors"
                >
                  {cancelling ? "Cancelling..." : "Confirm Cancel"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}