"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import NewConsultationWorkflow from "../../../components/clinical/NewConsultationWorkflow";
import { apiClient } from "../../../lib/apiClient";
import {
  Bot,
  Users,
  Calendar,
  Stethoscope,
  Activity,
  Pill,
  AlertCircle,
  ChevronRight,
  Search,
  Loader2,
  RefreshCw,
} from "lucide-react";

interface Patient {
  id: number;
  firstName: string;
  lastName: string;
  phone: string;
  dateOfBirth: string;
  gender: string;
}

interface PatientSummary {
  patientId: number;
  summaryText: string;
  activeConditions: string[];
  currentMedications: string[];
  recentLabResults: string[];
  upcomingAppointments: string[];
  generatedAt: string;
  confidence: number;
}

interface Appointment {
  id: number;
  patientId: number;
  patientName?: string;
  doctorId: number;
  doctorName?: string;
  serviceId?: number;
  serviceName?: string;
  appointmentDate: string;
  durationMinutes: number;
  type: string;
  status: string;
  notes?: string;
}

export default function DoctorPortal() {
  const [activeTab, setActiveTab] = useState("patients");
  const [patients, setPatients] = useState<Patient[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [patientSummary, setPatientSummary] = useState<PatientSummary | null>(null);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loadingAppointments, setLoadingAppointments] = useState(false);
  const [loadingPatients, setLoadingPatients] = useState(false);
  const [loadingSummary, setLoadingSummary] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    loadPatients();
    loadAppointments();
  }, []);

  const loadAppointments = async () => {
    setLoadingAppointments(true);
    try {
      const res = await apiClient.get("/api/appointment?pageSize=50");
      if (res.ok) {
        const data = await res.json();
        setAppointments(data.appointments || data.items || []);
      }
    } catch (err) {
      console.error("Failed to load appointments:", err);
    } finally {
      setLoadingAppointments(false);
    }
  };

  const handleCancelAppointment = async (id: number) => {
    const reason = window.prompt("Reason for cancellation?");
    if (!reason) return;
    try {
      const res = await apiClient.patch(`/api/appointment/${id}/cancel`, { reason });
      if (res.ok) {
        await loadAppointments();
      } else {
        alert("Failed to cancel appointment");
      }
    } catch (err) {
      console.error(err);
      alert("Failed to cancel appointment");
    }
  };

  const loadPatients = async () => {
    setLoadingPatients(true);
    setError("");
    try {
      const res = await apiClient.get("/api/patient?pageSize=50");
      if (res.ok) {
        const data = await res.json();
        setPatients(data.patients || data.items || []);
      } else {
        setError("Failed to load patients");
      }
    } catch (err) {
      console.error("Failed to load patients:", err);
      setError("Failed to load patients");
    } finally {
      setLoadingPatients(false);
    }
  };

  const loadPatientSummary = async (patientId: number) => {
    setLoadingSummary(true);
    setError("");
    try {
      const res = await apiClient.get(`/api/rag/patient/${patientId}/summary?depth=1`);
      if (res.ok) {
        const data = await res.json();
        setPatientSummary(data);
      } else {
        throw new Error("Failed to load summary");
      }
    } catch (err) {
      console.error("Failed to load patient summary:", err);
      setError("AI summary not available. Make sure LLM services are configured.");
      setPatientSummary(null);
    } finally {
      setLoadingSummary(false);
    }
  };

  const handlePatientSelect = (patient: Patient) => {
    setSelectedPatient(patient);
    setPatientSummary(null);
    loadPatientSummary(patient.id);
  };

  const menuItems = [
    { id: "patients", label: "Patients", icon: Users },
    { id: "consultation", label: "New Consultation", icon: Stethoscope },
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "ai-assist", label: "AI Assist", icon: Bot },
  ];

  const filteredPatients = patients.filter(p =>
    `${p.firstName} ${p.lastName} ${p.phone}`.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <ProtectedRoute allowedRoles={["Doctor"]}>
      <ClinicalPortalLayout
        portalLabel="Doctor Portal"
        activeItem={activeTab}
        navItems={menuItems}
        onNavigate={setActiveTab}
        primaryActionLabel="New Consultation"
        onPrimaryAction={() => setActiveTab("consultation")}
        searchPlaceholder="Search patients, records..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">
              {menuItems.find(m => m.id === activeTab)?.label || "Patients"}
            </h2>
            <p className="mt-1 text-sm text-slate-500">Review patient context, appointments, and AI-assisted clinical insights.</p>
          </div>
        </div>

        <div>
          {activeTab === "patients" && (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* Patient List */}
              <div className="lg:col-span-1 space-y-4">
                <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
                  <div className="p-4 border-b border-gray-800">
                    <div className="relative">
                      <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
                      <input
                        type="text"
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        placeholder="Search patients..."
                        className="w-full pl-9 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-blue-500 text-sm"
                      />
                    </div>
                  </div>
                  <div className="max-h-[calc(100vh-300px)] overflow-y-auto">
                    {loadingPatients ? (
                      <div className="flex justify-center py-12">
                        <Loader2 className="w-6 h-6 text-blue-400 animate-spin" />
                      </div>
                    ) : filteredPatients.length === 0 ? (
                      <div className="text-center py-12">
                        <Users className="w-10 h-10 text-gray-600 mx-auto mb-3" />
                        <p className="text-gray-500">No patients found</p>
                      </div>
                    ) : (
                      filteredPatients.map(patient => (
                        <button
                          key={patient.id}
                          onClick={() => handlePatientSelect(patient)}
                          className={`w-full text-left p-4 border-b border-gray-800 hover:bg-gray-800/50 transition-colors ${
                            selectedPatient?.id === patient.id ? "bg-blue-600/10 border-l-2 border-l-blue-500" : ""
                          }`}
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-blue-600/20 flex items-center justify-center text-blue-400 text-sm font-medium">
                              {patient.firstName[0]}{patient.lastName[0]}
                            </div>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium text-white truncate">
                                {patient.firstName} {patient.lastName}
                              </p>
                              <p className="text-xs text-gray-500">{patient.phone}</p>
                            </div>
                            <ChevronRight className="w-4 h-4 text-gray-600" />
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                </div>
              </div>

              {/* Patient Details */}
              <div className="lg:col-span-2">
                {selectedPatient ? (
                  <div className="space-y-6">
                    {/* Patient Info Card */}
                    <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex items-center gap-4">
                          <div className="w-16 h-16 rounded-full bg-blue-600/20 flex items-center justify-center text-blue-400 text-xl font-medium">
                            {selectedPatient.firstName[0]}{selectedPatient.lastName[0]}
                          </div>
                          <div>
                            <h3 className="text-xl font-semibold text-white">
                              {selectedPatient.firstName} {selectedPatient.lastName}
                            </h3>
                            <p className="text-gray-500 text-sm">
                              {new Date(selectedPatient.dateOfBirth).toLocaleDateString()} • {selectedPatient.gender}
                            </p>
                          </div>
                        </div>
                        <button
                          onClick={() => loadPatientSummary(selectedPatient.id)}
                          className="p-2 text-gray-400 hover:text-white bg-gray-800 rounded-lg transition-colors"
                          title="Refresh AI Summary"
                        >
                          <RefreshCw className={`w-4 h-4 ${loadingSummary ? "animate-spin" : ""}`} />
                        </button>
                      </div>
                    </div>

                    {/* AI Summary */}
                    <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden clinical-card-ai">
                      <div className="flex items-center gap-3 p-4 border-b border-gray-800 bg-gradient-to-r from-blue-600/10 to-purple-600/10">
                        <Bot className="w-5 h-5 text-blue-400" />
                        <h3 className="text-lg font-semibold text-white">AI Health Summary</h3>
                        {patientSummary && (
                          <span className="text-xs text-gray-500 ml-auto">
                            Generated {new Date(patientSummary.generatedAt).toLocaleTimeString()}
                          </span>
                        )}
                      </div>

                      {loadingSummary ? (
                        <div className="flex flex-col items-center justify-center py-12">
                          <Loader2 className="w-8 h-8 text-blue-400 animate-spin mb-4" />
                          <p className="text-gray-400">Analyzing patient records...</p>
                        </div>
                      ) : patientSummary ? (
                        <div className="p-6 space-y-6">
                          {/* Summary Text */}
                          <div>
                            <h4 className="text-sm font-medium text-gray-400 mb-2">Clinical Summary</h4>
                            <p className="text-gray-300 whitespace-pre-wrap">{patientSummary.summaryText}</p>
                          </div>

                          {/* Active Conditions */}
                          {patientSummary.activeConditions.length > 0 && (
                            <div>
                              <h4 className="text-sm font-medium text-gray-400 mb-2 flex items-center gap-2">
                                <AlertCircle className="w-4 h-4" /> Active Conditions
                              </h4>
                              <div className="flex flex-wrap gap-2">
                                {patientSummary.activeConditions.map((condition, idx) => (
                                  <span
                                    key={idx}
                                    className="px-3 py-1 bg-amber-500/10 text-amber-400 text-sm rounded-full"
                                  >
                                    {condition}
                                  </span>
                                ))}
                              </div>
                            </div>
                          )}

                          {/* Current Medications */}
                          {patientSummary.currentMedications.length > 0 && (
                            <div>
                              <h4 className="text-sm font-medium text-gray-400 mb-2 flex items-center gap-2">
                                <Pill className="w-4 h-4" /> Current Medications
                              </h4>
                              <div className="space-y-2">
                                {patientSummary.currentMedications.map((med, idx) => (
                                  <div key={idx} className="p-3 bg-gray-800/50 rounded-lg">
                                    <p className="text-sm text-white">{med}</p>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}

                          {/* Recent Lab Results */}
                          {patientSummary.recentLabResults.length > 0 && (
                            <div>
                              <h4 className="text-sm font-medium text-gray-400 mb-2 flex items-center gap-2">
                                <Activity className="w-4 h-4" /> Recent Lab Results
                              </h4>
                              <div className="space-y-2">
                                {patientSummary.recentLabResults.map((lab, idx) => (
                                  <div key={idx} className="p-3 bg-gray-800/50 rounded-lg">
                                    <p className="text-sm text-white">{lab}</p>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}

                          {/* Confidence Indicator */}
                          <div className="flex items-center gap-2 text-xs text-gray-500">
                            <span>AI Confidence:</span>
                            <div className="w-24 h-2 bg-gray-800 rounded-full overflow-hidden">
                              <div
                                className="h-full bg-gradient-to-r from-blue-500 to-purple-500"
                                style={{ width: `${patientSummary.confidence * 100}%` }}
                              />
                            </div>
                            <span>{Math.round(patientSummary.confidence * 100)}%</span>
                          </div>
                        </div>
                      ) : error ? (
                        <div className="p-6">
                          <div className="flex items-center gap-2 p-4 bg-red-500/10 border border-red-500/30 rounded-lg text-red-400">
                            <AlertCircle className="w-5 h-5" />
                            <p className="text-sm">{error}</p>
                          </div>
                          <p className="text-xs text-gray-500 mt-3">
                            AI summary requires OpenAI API keys to be configured in appsettings.
                          </p>
                        </div>
                      ) : null}
                    </div>
                  </div>
                ) : (
                  <div className="bg-gray-900 border border-gray-800 rounded-xl p-12 text-center">
                    <Users className="w-16 h-16 text-gray-600 mx-auto mb-4" />
                    <h3 className="text-lg font-medium text-white mb-2">Select a Patient</h3>
                    <p className="text-gray-500">
                      Choose a patient from the list to view their AI-generated health summary.
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === "consultation" && <NewConsultationWorkflow role="doctor" />}

          {activeTab === "appointments" && (
            <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
              <div className="p-4 border-b border-slate-200 flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-slate-900">All Appointments</h3>
                  <p className="text-sm text-slate-500">{appointments.length} total</p>
                </div>
                <button
                  onClick={loadAppointments}
                  className="px-3 py-1.5 text-sm bg-blue-50 text-blue-700 rounded hover:bg-blue-100"
                >
                  Refresh
                </button>
              </div>
              {loadingAppointments ? (
                <div className="flex justify-center py-12">
                  <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
                </div>
              ) : appointments.length === 0 ? (
                <div className="text-center py-12">
                  <Calendar className="w-10 h-10 text-slate-300 mx-auto mb-3" />
                  <p className="text-slate-500">No appointments scheduled</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-slate-50 text-slate-600 text-xs uppercase">
                      <tr>
                        <th className="px-4 py-3 text-left">Date & Time</th>
                        <th className="px-4 py-3 text-left">Patient</th>
                        <th className="px-4 py-3 text-left">Type</th>
                        <th className="px-4 py-3 text-left">Status</th>
                        <th className="px-4 py-3 text-left">Duration</th>
                        <th className="px-4 py-3 text-left">Notes</th>
                        <th className="px-4 py-3 text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appointments.map((apt) => {
                        const dt = new Date(apt.appointmentDate);
                        const statusColor: Record<string, string> = {
                          Scheduled: "bg-blue-100 text-blue-700",
                          Confirmed: "bg-emerald-100 text-emerald-700",
                          "In Progress": "bg-amber-100 text-amber-700",
                          Completed: "bg-slate-100 text-slate-700",
                          Cancelled: "bg-red-100 text-red-700",
                        };
                        return (
                          <tr key={apt.id} className="border-t border-slate-200 hover:bg-slate-50">
                            <td className="px-4 py-3 text-slate-900">
                              <div className="font-medium">{dt.toLocaleDateString()}</div>
                              <div className="text-xs text-slate-500">{dt.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</div>
                            </td>
                            <td className="px-4 py-3 text-slate-700">#{apt.patientId}</td>
                            <td className="px-4 py-3 text-slate-700">{apt.type || "—"}</td>
                            <td className="px-4 py-3">
                              <span className={`px-2 py-1 rounded-full text-xs font-medium ${statusColor[apt.status] || "bg-slate-100 text-slate-700"}`}>
                                {apt.status}
                              </span>
                            </td>
                            <td className="px-4 py-3 text-slate-700">{apt.durationMinutes} min</td>
                            <td className="px-4 py-3 text-slate-500 max-w-xs truncate">{apt.notes || "—"}</td>
                            <td className="px-4 py-3 text-right">
                              {apt.status !== "Cancelled" && apt.status !== "Completed" && (
                                <button
                                  onClick={() => handleCancelAppointment(apt.id)}
                                  className="text-red-600 hover:text-red-800 text-xs font-medium"
                                >
                                  Cancel
                                </button>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {activeTab === "ai-assist" && (
            <div className="space-y-4">
              <div className="bg-gradient-to-r from-blue-50 to-purple-50 border border-blue-200 rounded-xl p-6">
                <div className="flex items-start gap-3">
                  <Bot className="w-6 h-6 text-blue-600 mt-1" />
                  <div>
                    <h3 className="text-lg font-semibold text-slate-900">AI Clinical Assistant</h3>
                    <p className="text-sm text-slate-600 mt-1">
                      Select a patient from the Patients tab to get an AI-generated summary of their medical history, active conditions, and current medications. The AI analyzes past notes, prescriptions, vitals, and uploaded reports to give you instant context before a consultation.
                    </p>
                  </div>
                </div>
              </div>

              {selectedPatient ? (
                <div className="bg-white border border-slate-200 rounded-xl p-6">
                  <div className="flex items-center gap-3 mb-4">
                    <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold">
                      {selectedPatient.firstName[0]}{selectedPatient.lastName[0]}
                    </div>
                    <div>
                      <h4 className="font-semibold text-slate-900">{selectedPatient.firstName} {selectedPatient.lastName}</h4>
                      <p className="text-xs text-slate-500">Quick AI context for your next consultation</p>
                    </div>
                  </div>
                  {patientSummary ? (
                    <div className="prose prose-sm max-w-none">
                      <p className="text-slate-700 whitespace-pre-wrap">{patientSummary.summaryText}</p>
                    </div>
                  ) : (
                    <p className="text-sm text-slate-500">No AI summary available. Configure LLM provider in backend appsettings.</p>
                  )}
                </div>
              ) : (
                <div className="bg-white border border-slate-200 rounded-xl p-8 text-center">
                  <Bot className="w-12 h-12 text-slate-300 mx-auto mb-3" />
                  <p className="text-slate-500">Select a patient from the Patients tab to start.</p>
                </div>
              )}
            </div>
          )}
        </div>
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}
