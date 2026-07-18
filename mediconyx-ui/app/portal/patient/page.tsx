"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import PatientAIChat from "../../../components/patient/PatientAIChat";
import PatientReportUpload from "../../../components/patient/PatientReportUpload";
import { apiClient } from "../../../lib/apiClient";
import { useAuthStore } from "../../../store/useAuthStore";
import { Bot, FileUp, Calendar, User, Loader2, Save } from "lucide-react";

interface Appointment {
  id: number;
  patientId: number;
  doctorId: number;
  doctorName?: string;
  appointmentDate: string;
  durationMinutes: number;
  type: string;
  status: string;
  notes?: string;
}

interface PatientProfile {
  id: number;
  patientNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  bloodGroup: string;
  phone: string;
  email: string;
  address?: string;
  city?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export default function PatientPortal() {
  const [activeTab, setActiveTab] = useState("chat");
  const { user } = useAuthStore();

  const menuItems = [
    { id: "chat", label: "AI Assistant", icon: Bot, isAi: true },
    { id: "reports", label: "My Reports", icon: FileUp },
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "profile", label: "My Profile", icon: User },
  ];

  return (
    <ProtectedRoute allowedRoles={["Patient"]}>
      <ClinicalPortalLayout
        portalLabel="Patient Portal"
        activeItem={activeTab}
        navItems={menuItems}
        onNavigate={setActiveTab}
        primaryActionLabel="Upload Report"
        onPrimaryAction={() => setActiveTab("reports")}
        searchPlaceholder="Search reports, appointments..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">
              {menuItems.find(m => m.id === activeTab)?.label || "AI Assistant"}
            </h2>
            <p className="mt-1 text-sm text-slate-500">Access your health assistant, uploaded reports, appointments, and profile information.</p>
          </div>
        </div>

        {activeTab === "chat" && (
          <div className="h-[calc(100vh-176px)]">
            <PatientAIChat />
          </div>
        )}

        {activeTab === "reports" && <PatientReportUpload />}

        {activeTab === "appointments" && <AppointmentsTab userId={user?.id} />}

        {activeTab === "profile" && <ProfileTab />}
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}

function AppointmentsTab({ userId }: { userId?: number }) {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/appointment?pageSize=50");
        if (res.ok) {
          const data = await res.json();
          setAppointments(data.appointments || data.items || []);
        }
      } catch (err) {
        console.error("Failed to load appointments:", err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [userId]);

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
      </div>
    );
  }

  const now = new Date();
  const upcoming = appointments.filter(a => new Date(a.appointmentDate) >= now && a.status !== "Cancelled");
  const past = appointments.filter(a => new Date(a.appointmentDate) < now || a.status === "Completed" || a.status === "Cancelled");

  return (
    <div className="space-y-6">
      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div className="p-4 border-b border-slate-200">
          <h3 className="text-lg font-semibold text-slate-900">Upcoming Appointments</h3>
          <p className="text-sm text-slate-500">{upcoming.length} scheduled</p>
        </div>
        {upcoming.length === 0 ? (
          <div className="text-center py-12">
            <Calendar className="w-10 h-10 text-slate-300 mx-auto mb-3" />
            <p className="text-slate-500">No upcoming appointments</p>
            <p className="text-xs text-slate-400 mt-1">Book a new appointment through your hospital receptionist.</p>
          </div>
        ) : (
          <div className="divide-y divide-slate-200">
            {upcoming.map((apt) => {
              const dt = new Date(apt.appointmentDate);
              return (
                <div key={apt.id} className="p-4 flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-14 h-14 bg-blue-50 rounded-lg flex flex-col items-center justify-center text-blue-700">
                      <span className="text-xs font-medium uppercase">{dt.toLocaleDateString("en", { month: "short" })}</span>
                      <span className="text-xl font-bold leading-none">{dt.getDate()}</span>
                    </div>
                    <div>
                      <p className="font-medium text-slate-900">{apt.type || "Appointment"}</p>
                      <p className="text-sm text-slate-500">
                        {dt.toLocaleDateString("en", { weekday: "long" })} at {dt.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })} • {apt.durationMinutes} min
                      </p>
                      {apt.notes && <p className="text-xs text-slate-400 mt-1">{apt.notes}</p>}
                    </div>
                  </div>
                  <span className="px-2 py-1 bg-emerald-100 text-emerald-700 text-xs rounded-full font-medium">
                    {apt.status}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {past.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="p-4 border-b border-slate-200">
            <h3 className="text-lg font-semibold text-slate-900">Past Appointments</h3>
            <p className="text-sm text-slate-500">{past.length} total</p>
          </div>
          <div className="divide-y divide-slate-200 max-h-96 overflow-y-auto">
            {past.map((apt) => {
              const dt = new Date(apt.appointmentDate);
              return (
                <div key={apt.id} className="p-4 flex items-center justify-between text-sm">
                  <div>
                    <p className="font-medium text-slate-700">{apt.type || "Appointment"}</p>
                    <p className="text-xs text-slate-500">{dt.toLocaleDateString()} • {apt.durationMinutes} min</p>
                  </div>
                  <span className="px-2 py-1 bg-slate-100 text-slate-600 text-xs rounded-full">
                    {apt.status}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

function ProfileTab() {
  const [profile, setProfile] = useState<PatientProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editing, setEditing] = useState(false);
  const [edited, setEdited] = useState<Partial<PatientProfile>>({});
  const [message, setMessage] = useState("");

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/patient?pageSize=1");
        if (res.ok) {
          const data = await res.json();
          const list = data.patients || data.items || [];
          if (list.length > 0) {
            setProfile(list[0]);
            setEdited(list[0]);
          }
        }
      } catch (err) {
        console.error("Failed to load profile:", err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    if (!profile) return;
    setSaving(true);
    setMessage("");
    try {
      const res = await apiClient.put(`/api/patient/${profile.id}`, edited);
      if (res.ok) {
        const updated = await res.json();
        setProfile(updated);
        setEdited(updated);
        setEditing(false);
        setMessage("Profile updated successfully");
        setTimeout(() => setMessage(""), 3000);
      } else {
        setMessage("Failed to update profile");
      }
    } catch (err) {
      console.error(err);
      setMessage("Failed to update profile");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="bg-white border border-slate-200 rounded-xl p-8 text-center">
        <User className="w-12 h-12 text-slate-300 mx-auto mb-3" />
        <p className="text-slate-500">No patient profile found for your account.</p>
        <p className="text-xs text-slate-400 mt-2">Please contact your hospital to register as a patient.</p>
      </div>
    );
  }

  const fields: Array<{ key: keyof PatientProfile; label: string; type?: string }> = [
    { key: "firstName", label: "First Name" },
    { key: "lastName", label: "Last Name" },
    { key: "dateOfBirth", label: "Date of Birth", type: "date" },
    { key: "gender", label: "Gender" },
    { key: "bloodGroup", label: "Blood Group" },
    { key: "phone", label: "Phone" },
    { key: "email", label: "Email", type: "email" },
    { key: "address", label: "Address" },
    { key: "city", label: "City" },
    { key: "emergencyContactName", label: "Emergency Contact Name" },
    { key: "emergencyContactPhone", label: "Emergency Contact Phone" },
  ];

  return (
    <div className="space-y-4">
      <div className="bg-white border border-slate-200 rounded-xl p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h3 className="text-lg font-semibold text-slate-900">Patient Profile</h3>
            <p className="text-sm text-slate-500">Patient ID: {profile.patientNumber}</p>
          </div>
          {!editing ? (
            <button
              onClick={() => setEditing(true)}
              className="px-4 py-2 bg-blue-600 text-white rounded text-sm font-medium hover:bg-blue-700"
            >
              Edit
            </button>
          ) : (
            <div className="flex gap-2">
              <button
                onClick={() => { setEditing(false); setEdited(profile); }}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded text-sm font-medium hover:bg-slate-200"
                disabled={saving}
              >
                Cancel
              </button>
              <button
                onClick={handleSave}
                disabled={saving}
                className="px-4 py-2 bg-blue-600 text-white rounded text-sm font-medium hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                Save
              </button>
            </div>
          )}
        </div>

        {message && (
          <div className={`mb-4 p-3 rounded text-sm ${message.includes("success") ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"}`}>
            {message}
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {fields.map(({ key, label, type }) => {
            const value = (editing ? edited[key] : profile[key]) as string | undefined;
            return (
              <div key={key}>
                <label className="block text-xs font-medium text-slate-600 mb-1">{label}</label>
                {editing ? (
                  <input
                    type={type || "text"}
                    value={value || ""}
                    onChange={(e) => setEdited({ ...edited, [key]: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
                  />
                ) : (
                  <p className="text-sm text-slate-900 py-2">{value || "—"}</p>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
