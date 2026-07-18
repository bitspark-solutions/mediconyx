"use client";
import { useState, useEffect } from "react";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import { apiClient } from "../../../lib/apiClient";
import {
  Activity, Users, ClipboardList, Loader2, Save, Calendar, Search, Plus
} from "lucide-react";

interface Patient {
  id: number;
  patientNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  phone: string;
  bloodGroup?: string;
}

export default function NursePortal() {
  const [activeTab, setActiveTab] = useState("patients");
  const menuItems = [
    { id: "patients", label: "Patient List", icon: Users },
    { id: "vitals", label: "Record Vitals", icon: Activity },
    { id: "appointments", label: "Today's Queue", icon: Calendar },
  ];

  return (
    <ProtectedRoute allowedRoles={["Nurse"]}>
      <ClinicalPortalLayout
        portalLabel="Nurse Portal"
        activeItem={activeTab}
        navItems={menuItems}
        onNavigate={setActiveTab}
        primaryActionLabel="Record Vitals"
        onPrimaryAction={() => setActiveTab("vitals")}
        searchPlaceholder="Search patients..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">
              {menuItems.find(m => m.id === activeTab)?.label}
            </h2>
            <p className="mt-1 text-sm text-slate-500">Assist doctors with patient care, vitals recording, and queue management.</p>
          </div>
        </div>

        {activeTab === "patients" && <PatientsTab />}
        {activeTab === "vitals" && <VitalsTab />}
        {activeTab === "appointments" && <QueueTab />}
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}

function PatientsTab() {
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/patient?pageSize=100");
        if (res.ok) {
          const data = await res.json();
          setPatients(data.patients || data.items || []);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = patients.filter(p =>
    `${p.firstName} ${p.lastName} ${p.patientNumber} ${p.phone}`.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
      <div className="p-4 border-b border-slate-200 flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name, ID, or phone..."
            className="w-full pl-9 pr-4 py-2 border border-slate-200 rounded text-sm focus:outline-none focus:border-blue-500"
          />
        </div>
      </div>
      {loading ? (
        <div className="flex justify-center py-12">
          <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-12 text-slate-500">
          <Users className="w-10 h-10 text-slate-300 mx-auto mb-3" />
          No patients found
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-slate-600 text-xs uppercase">
              <tr>
                <th className="px-4 py-3 text-left">Patient ID</th>
                <th className="px-4 py-3 text-left">Name</th>
                <th className="px-4 py-3 text-left">Gender</th>
                <th className="px-4 py-3 text-left">DOB</th>
                <th className="px-4 py-3 text-left">Phone</th>
                <th className="px-4 py-3 text-left">Blood</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((p) => (
                <tr key={p.id} className="border-t border-slate-200 hover:bg-slate-50">
                  <td className="px-4 py-3 text-slate-900 font-mono text-xs">{p.patientNumber}</td>
                  <td className="px-4 py-3 text-slate-900 font-medium">{p.firstName} {p.lastName}</td>
                  <td className="px-4 py-3 text-slate-700">{p.gender}</td>
                  <td className="px-4 py-3 text-slate-700 text-xs">{new Date(p.dateOfBirth).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-slate-700 text-xs">{p.phone}</td>
                  <td className="px-4 py-3 text-slate-700">{p.bloodGroup || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function VitalsTab() {
  const [patients, setPatients] = useState<Patient[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [vitals, setVitals] = useState({
    systolic: "",
    diastolic: "",
    heartRate: "",
    temperature: "",
    weight: "",
    height: "",
    spO2: "",
    bloodGlucose: "",
  });

  useEffect(() => {
    apiClient.get("/api/patient?pageSize=50").then(async (res) => {
      if (res.ok) {
        const data = await res.json();
        setPatients(data.patients || data.items || []);
      }
    });
  }, []);

  const handleSave = async () => {
    if (!selectedId) {
      setMessage("Please select a patient first");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      const payload: any = {
        patientId: selectedId,
        recordedAt: new Date().toISOString(),
      };
      if (vitals.systolic && vitals.diastolic) {
        payload.bloodPressureSystolic = parseInt(vitals.systolic);
        payload.bloodPressureDiastolic = parseInt(vitals.diastolic);
      }
      if (vitals.heartRate) payload.heartRate = parseInt(vitals.heartRate);
      if (vitals.temperature) payload.temperatureCelsius = parseFloat(vitals.temperature);
      if (vitals.weight) payload.weightKg = parseFloat(vitals.weight);
      if (vitals.height) payload.heightCm = parseFloat(vitals.height);
      if (vitals.spO2) payload.oxygenSaturation = parseFloat(vitals.spO2);
      if (vitals.bloodGlucose) payload.bloodGlucoseMgDl = parseFloat(vitals.bloodGlucose);

      const res = await apiClient.post("/api/vital-sign", payload);
      if (res.ok) {
        setMessage("Vitals recorded successfully");
        setVitals({ systolic: "", diastolic: "", heartRate: "", temperature: "", weight: "", height: "", spO2: "", bloodGlucose: "" });
        setSelectedId(null);
        setTimeout(() => setMessage(""), 3000);
      } else {
        setMessage("Failed to record vitals");
      }
    } catch (err) {
      console.error(err);
      setMessage("Failed to record vitals");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="bg-white border border-slate-200 rounded-xl p-6">
        <div className="flex items-center gap-3 mb-4">
          <Activity className="w-5 h-5 text-blue-600" />
          <h3 className="text-lg font-semibold text-slate-900">Record Vital Signs</h3>
        </div>

        {message && (
          <div className={`mb-4 p-3 rounded text-sm ${message.includes("success") ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"}`}>
            {message}
          </div>
        )}

        <div className="mb-4">
          <label htmlFor="vitals-patient" className="block text-sm font-medium text-slate-700 mb-1">Patient</label>
          <select
            id="vitals-patient"
            value={selectedId || ""}
            onChange={(e) => setSelectedId(e.target.value ? parseInt(e.target.value) : null)}
            className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
          >
            <option value="">— Select patient —</option>
            {patients.map((p) => (
              <option key={p.id} value={p.id}>
                {p.patientNumber} — {p.firstName} {p.lastName}
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
          <div>
            <label htmlFor="vitals-systolic" className="block text-xs font-medium text-slate-600 mb-1">BP Systolic (mmHg)</label>
            <input
              id="vitals-systolic"
              type="number"
              value={vitals.systolic}
              onChange={(e) => setVitals({ ...vitals, systolic: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="120"
            />
          </div>
          <div>
            <label htmlFor="vitals-diastolic" className="block text-xs font-medium text-slate-600 mb-1">BP Diastolic (mmHg)</label>
            <input
              id="vitals-diastolic"
              type="number"
              value={vitals.diastolic}
              onChange={(e) => setVitals({ ...vitals, diastolic: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="80"
            />
          </div>
          <div>
            <label htmlFor="vitals-heartRate" className="block text-xs font-medium text-slate-600 mb-1">Heart Rate (bpm)</label>
            <input
              id="vitals-heartRate"
              type="number"
              value={vitals.heartRate}
              onChange={(e) => setVitals({ ...vitals, heartRate: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="72"
            />
          </div>
          <div>
            <label htmlFor="vitals-temperature" className="block text-xs font-medium text-slate-600 mb-1">Temperature (°C)</label>
            <input
              id="vitals-temperature"
              type="number"
              step="0.1"
              value={vitals.temperature}
              onChange={(e) => setVitals({ ...vitals, temperature: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="36.6"
            />
          </div>
          <div>
            <label htmlFor="vitals-weight" className="block text-xs font-medium text-slate-600 mb-1">Weight (kg)</label>
            <input
              id="vitals-weight"
              type="number"
              step="0.1"
              value={vitals.weight}
              onChange={(e) => setVitals({ ...vitals, weight: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="70"
            />
          </div>
          <div>
            <label htmlFor="vitals-height" className="block text-xs font-medium text-slate-600 mb-1">Height (cm)</label>
            <input
              id="vitals-height"
              type="number"
              step="0.1"
              value={vitals.height}
              onChange={(e) => setVitals({ ...vitals, height: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="170"
            />
          </div>
          <div>
            <label htmlFor="vitals-spO2" className="block text-xs font-medium text-slate-600 mb-1">SpO2 (%)</label>
            <input
              id="vitals-spO2"
              type="number"
              value={vitals.spO2}
              onChange={(e) => setVitals({ ...vitals, spO2: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="98"
            />
          </div>
          <div>
            <label htmlFor="vitals-bloodGlucose" className="block text-xs font-medium text-slate-600 mb-1">Blood Glucose (mg/dL)</label>
            <input
              id="vitals-bloodGlucose"
              type="number"
              value={vitals.bloodGlucose}
              onChange={(e) => setVitals({ ...vitals, bloodGlucose: e.target.value })}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="100"
            />
          </div>
        </div>

        <button
          onClick={handleSave}
          disabled={saving}
          className="px-4 py-2 bg-blue-600 text-white rounded text-sm font-medium hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
        >
          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
          Record Vitals
        </button>
      </div>
    </div>
  );
}

function QueueTab() {
  const [appointments, setAppointments] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient.get("/api/appointment?pageSize=50").then(async (res) => {
      if (res.ok) {
        const data = await res.json();
        setAppointments(data.appointments || data.items || []);
      }
      setLoading(false);
    });
  }, []);

  const today = new Date().toDateString();
  const todayApts = appointments.filter(a => new Date(a.appointmentDate).toDateString() === today);

  if (loading) {
    return <div className="flex justify-center py-12"><Loader2 className="w-6 h-6 text-blue-500 animate-spin" /></div>;
  }

  return (
    <div className="bg-white border border-slate-200 rounded-xl p-6">
      <div className="flex items-center gap-3 mb-4">
        <Calendar className="w-5 h-5 text-blue-600" />
        <h3 className="text-lg font-semibold text-slate-900">Today's Queue</h3>
      </div>
      {todayApts.length === 0 ? (
        <div className="text-center py-12 text-slate-500">
          <ClipboardList className="w-10 h-10 text-slate-300 mx-auto mb-3" />
          No appointments for today
        </div>
      ) : (
        <div className="space-y-2">
          {todayApts.map((apt) => {
            const dt = new Date(apt.appointmentDate);
            return (
              <div key={apt.id} className="p-3 border border-slate-200 rounded flex items-center justify-between">
                <div>
                  <p className="font-medium text-slate-900">Patient #{apt.patientId}</p>
                  <p className="text-xs text-slate-500">
                    {dt.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })} • {apt.type} • {apt.durationMinutes}min
                  </p>
                </div>
                <span className="px-2 py-1 bg-blue-100 text-blue-700 text-xs rounded-full font-medium">
                  {apt.status}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
