"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import { useAuthStore } from "../../../store/useAuthStore";
import { apiClient } from "../../../lib/apiClient";
import {
  Bot,
  Users,
  Calendar,
  FileText,
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

export default function DoctorPortal() {
  const [activeTab, setActiveTab] = useState("patients");
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [patientSummary, setPatientSummary] = useState<PatientSummary | null>(null);
  const [loadingPatients, setLoadingPatients] = useState(false);
  const [loadingSummary, setLoadingSummary] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    loadPatients();
  }, []);

  const loadPatients = async () => {
    setLoadingPatients(true);
    try {
      const res = await apiClient.get("/api/patient?pageSize=50");
      if (res.ok) {
        const data = await res.json();
        setPatients(data.items || []);
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
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "ai-assist", label: "AI Assist", icon: Bot },
  ];

  const filteredPatients = patients.filter(p =>
    `${p.firstName} ${p.lastName} ${p.phone}`.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <ProtectedRoute allowedRoles={["Doctor"]}>
      <main className="min-h-screen bg-black flex">
        {/* Sidebar */}
        <aside
          className={`${
            sidebarOpen ? "w-64" : "w-20"
          } bg-gray-900 border-r border-gray-800 flex flex-col fixed h-full z-40 transition-all duration-300`}
        >
          <div className="p-4 border-b border-gray-800">
            <div className="flex items-center justify-between">
              {sidebarOpen && (
                <div>
                  <h1 className="text-xl font-bold text-blue-400">Mediconyx</h1>
                  <p className="text-xs text-gray-500 mt-0.5">Doctor Portal</p>
                </div>
              )}
              <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className="p-1.5 rounded-lg text-gray-400 hover:text-white hover:bg-gray-800 transition-colors"
              >
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>
            </div>
          </div>

          <nav className="flex-1 overflow-y-auto py-4 px-3">
            <div className="space-y-1">
              {menuItems.map((item) => {
                const Icon = item.icon;
                return (
                  <button
                    key={item.id}
                    onClick={() => setActiveTab(item.id)}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors ${
                      activeTab === item.id
                        ? "bg-blue-600 text-white"
                        : "text-gray-400 hover:bg-gray-800 hover:text-white"
                    }`}
                  >
                    <Icon className="w-5 h-5 flex-shrink-0" />
                    {sidebarOpen && <span className="text-sm font-medium">{item.label}</span>}
                  </button>
                );
              })}
            </div>
          </nav>
        </aside>

        {/* Main Content */}
        <div className={`flex-1 transition-all duration-300 ${sidebarOpen ? "ml-64" : "ml-20"}`}>
          <header className="sticky top-0 bg-black/80 backdrop-blur-md border-b border-gray-800 px-6 py-4 z-30">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-white">
                {menuItems.find(m => m.id === activeTab)?.label || "Dashboard"}
              </h2>
            </div>
          </header>

          <div className="p-6">
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
                      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
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

            {activeTab === "appointments" && (
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-8 text-center">
                <Calendar className="w-12 h-12 text-gray-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-white mb-2">Appointments</h3>
                <p className="text-gray-500">Your schedule and appointments will appear here.</p>
              </div>
            )}

            {activeTab === "ai-assist" && (
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-8 text-center">
                <Bot className="w-12 h-12 text-gray-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-white mb-2">AI Assist</h3>
                <p className="text-gray-500">
                  Use AI to assist with clinical decisions, drug interactions, and more.
                </p>
              </div>
            )}
          </div>
        </div>
      </main>
    </ProtectedRoute>
  );
}
