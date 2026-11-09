"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { apiClient } from "../../lib/apiClient";
import { useAuthStore } from "../../store/useAuthStore";
import {
  AlertCircle,
  ArrowRight,
  Bot,
  CheckCircle2,
  ChevronRight,
  Clock,
  FileText,
  Lightbulb,
  Phone,
  PlusCircle,
  RefreshCw,
  Search,
  Sparkles,
  Stethoscope,
  UserPlus,
  Zap,
} from "lucide-react";

type WorkflowRole = "receptionist" | "doctor";

type NewConsultationWorkflowProps = {
  role: WorkflowRole;
};

type PatientDraft = {
  fullName: string;
  dateOfBirth: string;
  gender: string;
  nationalId: string;
  phone: string;
  email: string;
};

type ConsultationDraft = {
  departmentId: string;
  doctorId: string;
  serviceType: ServiceTypeOption;
  priority: "normal" | "urgent" | "emergency";
};

type Priority = ConsultationDraft["priority"];

type ServiceTypeOption = "Initial Consultation" | "Follow-up Visit" | "Lab Review" | "Diagnostic Scan" | "Telemedicine";

// Maps the UI service options to the API's AppointmentType enum (string form is
// accepted globally by JsonStringEnumConverter).
const SERVICE_TYPE_TO_ENUM: Record<ServiceTypeOption, string> = {
  "Initial Consultation": "Consultation",
  "Follow-up Visit": "FollowUp",
  "Lab Review": "Checkup",
  "Diagnostic Scan": "Procedure",
  Telemedicine: "Telemedicine",
};

const SERVICE_TYPES = Object.keys(SERVICE_TYPE_TO_ENUM) as ServiceTypeOption[];

type PatientListItem = {
  id: number;
  patientNumber: string;
  firstName: string;
  lastName: string;
  phone: string;
  gender: string;
};

type DoctorOption = {
  profileId: number;
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  specialization: string;
  departmentId: number | null;
  isAcceptingPatients: boolean;
};

type DepartmentOption = {
  id: number;
  name: string;
};

type SlotOption = {
  startTime: string;
  endTime: string;
};

type AppointmentListItem = {
  id: number;
  patientId: number;
  patientName: string;
  patientNumber: string;
  doctorName: string;
  departmentName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: string;
  type: string;
  reason?: string | null;
};

const todayIso = () => new Date().toISOString().slice(0, 10);

const priorityLabel: Record<Priority, string> = {
  normal: "Normal",
  urgent: "Urgent",
  emergency: "Emergency",
};

export default function NewConsultationWorkflow({ role }: NewConsultationWorkflowProps) {
  const isDoctor = role === "doctor";
  const { user } = useAuthStore();

  // Reference data
  const [doctors, setDoctors] = useState<DoctorOption[]>([]);
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [recentPatients, setRecentPatients] = useState<PatientListItem[]>([]);
  const [todayAppointments, setTodayAppointments] = useState<AppointmentListItem[]>([]);

  // Step 1: patient search / selection
  const [patientSearch, setPatientSearch] = useState("");
  const [patientResults, setPatientResults] = useState<PatientListItem[]>([]);
  const [searchingPatients, setSearchingPatients] = useState(false);
  const [selectedPatient, setSelectedPatient] = useState<PatientListItem | null>(null);

  // Step 2: registration draft (used when no existing patient is selected)
  const [patientDraft, setPatientDraft] = useState<PatientDraft>({
    fullName: "",
    dateOfBirth: "",
    gender: "",
    nationalId: "",
    phone: "",
    email: "",
  });

  // AI helper note (sent as appointment notes)
  const [aiNote, setAiNote] = useState("");

  // Step 3: consultation details
  const [consultationDraft, setConsultationDraft] = useState<ConsultationDraft>({
    departmentId: "",
    doctorId: "",
    serviceType: "Initial Consultation",
    priority: "normal",
  });
  const [appointmentDate, setAppointmentDate] = useState(todayIso());
  const [startTime, setStartTime] = useState("10:00");
  const [endTime, setEndTime] = useState("10:30");
  const [slots, setSlots] = useState<SlotOption[]>([]);
  const [loadingSlots, setLoadingSlots] = useState(false);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  // ── Data loading ─────────────────────────────────────────────────────────

  const loadTodayAppointments = useCallback(async () => {
    try {
      const res = await apiClient.get(
        `/api/appointment?dateFrom=${todayIso()}&dateTo=${todayIso()}&pageSize=100`
      );
      if (res.ok) {
        const data = await res.json();
        setTodayAppointments(data.appointments || []);
      }
    } catch (err) {
      console.error("Failed to load today's appointments:", err);
    }
  }, []);

  const loadRecentPatients = useCallback(async () => {
    try {
      const res = await apiClient.get("/api/patient?pageSize=5");
      if (res.ok) {
        const data = await res.json();
        setRecentPatients(data.patients || []);
      }
    } catch (err) {
      console.error("Failed to load recent patients:", err);
    }
  }, []);

  useEffect(() => {
    const loadReferenceData = async () => {
      const [doctorsRes, departmentsRes] = await Promise.all([
        apiClient.get("/api/doctor?pageSize=100"),
        apiClient.get("/api/department?pageSize=100"),
      ]);
      try {
        if (doctorsRes.ok) {
          const data = await doctorsRes.json();
          const options: DoctorOption[] = (data.doctors || [])
            .filter((d: { isActive: boolean }) => d.isActive)
            .map((d: {
              id: number;
              userId: number;
              firstName: string;
              lastName: string;
              email: string;
              specialization: string;
              departmentId: number | null;
              isAcceptingPatients: boolean;
            }) => ({
              profileId: d.id,
              userId: d.userId,
              firstName: d.firstName,
              lastName: d.lastName,
              email: d.email,
              specialization: d.specialization,
              departmentId: d.departmentId,
              isAcceptingPatients: d.isAcceptingPatients,
            }));
          setDoctors(options);

          // Doctors consult in their own portal — preselect the logged-in doctor.
          if (isDoctor && user?.email) {
            const own = options.find(d => d.email.toLowerCase() === user.email.toLowerCase());
            if (own) {
              setConsultationDraft(prev => ({
                ...prev,
                doctorId: String(own.userId),
                departmentId: own.departmentId ? String(own.departmentId) : prev.departmentId,
              }));
            }
          }
        }
      } catch (err) {
        console.error("Failed to load doctors:", err);
      }
      try {
        if (departmentsRes.ok) {
          const data = await departmentsRes.json();
          setDepartments((data.departments || []).map((d: { id: number; name: string }) => ({ id: d.id, name: d.name })));
        }
      } catch (err) {
        console.error("Failed to load departments:", err);
      }
    };

    loadReferenceData();
    loadRecentPatients();
    loadTodayAppointments();
  }, [isDoctor, user?.email, loadRecentPatients, loadTodayAppointments]);

  // Load available slots whenever doctor + date change (requires doctor schedules;
  // falls back to manual time entry when the doctor has none).
  useEffect(() => {
    if (!consultationDraft.doctorId || !appointmentDate) {
      setSlots([]);
      return;
    }
    let cancelled = false;
    const loadSlots = async () => {
      setLoadingSlots(true);
      try {
        const res = await apiClient.get(
          `/api/appointment/slots?doctorId=${consultationDraft.doctorId}&date=${appointmentDate}`
        );
        if (!cancelled && res.ok) {
          const data = await res.json();
          setSlots(Array.isArray(data) ? data : []);
        } else if (!cancelled) {
          setSlots([]);
        }
      } catch {
        if (!cancelled) setSlots([]);
      } finally {
        if (!cancelled) setLoadingSlots(false);
      }
    };
    loadSlots();
    return () => {
      cancelled = true;
    };
  }, [consultationDraft.doctorId, appointmentDate]);

  // ── Actions ──────────────────────────────────────────────────────────────

  const searchPatients = async () => {
    const term = patientSearch.trim();
    if (term.length < 2) {
      setError("Enter at least 2 characters to search patients.");
      return;
    }
    setSearchingPatients(true);
    setError("");
    try {
      const res = await apiClient.get(`/api/patient?searchTerm=${encodeURIComponent(term)}&pageSize=10`);
      if (res.ok) {
        const data = await res.json();
        setPatientResults(data.patients || []);
      } else {
        setPatientResults([]);
        setError("Patient search failed.");
      }
    } catch (err) {
      console.error("Patient search failed:", err);
      setPatientResults([]);
      setError("Patient search failed.");
    } finally {
      setSearchingPatients(false);
    }
  };

  const selectPatient = (patient: PatientListItem) => {
    setSelectedPatient(patient);
    setError("");
    setSuccess("");
  };

  const clearSelectedPatient = () => {
    setSelectedPatient(null);
    setPatientDraft({ fullName: "", dateOfBirth: "", gender: "", nationalId: "", phone: "", email: "" });
  };

  const clearForm = () => {
    clearSelectedPatient();
    setPatientSearch("");
    setPatientResults([]);
    setAiNote("");
    setConsultationDraft({
      departmentId: "",
      doctorId: isDoctor ? consultationDraft.doctorId : "",
      serviceType: "Initial Consultation",
      priority: "normal",
    });
    setAppointmentDate(todayIso());
    setStartTime("10:00");
    setEndTime("10:30");
    setSlots([]);
    setError("");
    setSuccess("");
  };

  const createPatient = async (): Promise<PatientListItem> => {
    const nameParts = patientDraft.fullName.trim().split(" ");
    const firstName = nameParts[0] || "";
    const lastName = nameParts.slice(1).join(" ") || "";

    if (!firstName || !lastName || !patientDraft.phone.trim()) {
      throw new Error("Full name and phone are required to create a patient.");
    }

    const response = await apiClient.post("/api/patient", {
      firstName,
      lastName,
      phone: patientDraft.phone.trim(),
      gender: patientDraft.gender || "Other",
      dateOfBirth: patientDraft.dateOfBirth || new Date().toISOString(),
      email: patientDraft.email || undefined,
      nationalId: patientDraft.nationalId || undefined,
    });

    if (!response.ok) {
      const responseError = await response.json().catch(() => null);
      throw new Error(responseError?.message || "Failed to save patient.");
    }

    return await response.json();
  };

  const saveConsultation = async () => {
    setError("");
    setSuccess("");

    // 1. Resolve the patient: the selected existing one, or register a new one.
    let patient = selectedPatient;
    if (!patient) {
      try {
        patient = await createPatient();
      } catch (patientError) {
        setError(patientError instanceof Error ? patientError.message : "Failed to save patient.");
        return;
      }
    }

    // 2. Validate the consultation details.
    const doctorUserId = Number(consultationDraft.doctorId);
    const departmentId = Number(consultationDraft.departmentId);
    if (!doctorUserId) {
      setError("Select a doctor for the consultation.");
      return;
    }
    if (!departmentId) {
      setError("Select a department for the consultation.");
      return;
    }
    if (!appointmentDate) {
      setError("Pick a date for the consultation.");
      return;
    }
    if (!startTime || !endTime || endTime <= startTime) {
      setError("End time must be after start time.");
      return;
    }

    // 3. Build type/reason/notes from service type + priority + AI note.
    let type = SERVICE_TYPE_TO_ENUM[consultationDraft.serviceType];
    if (consultationDraft.priority === "emergency") type = "Emergency";
    const reason = isDoctor
      ? consultationDraft.serviceType
      : `${consultationDraft.serviceType} (${priorityLabel[consultationDraft.priority]} priority)`;
    const notes = consultationDraft.priority === "urgent"
      ? `[URGENT] ${aiNote.trim()}`.trim()
      : aiNote.trim() || undefined;

    setSaving(true);
    try {
      const response = await apiClient.post("/api/appointment", {
        patientId: patient.id,
        doctorId: doctorUserId,
        departmentId,
        appointmentDate,
        startTime,
        endTime,
        type,
        reason,
        notes,
      });

      if (!response.ok) {
        const responseError = await response.json().catch(() => null);
        throw new Error(responseError?.message || "Failed to create appointment.");
      }

      const appointment = await response.json();

      // clearForm resets the banners — set success only after it
      clearForm();
      setSuccess(
        `Consultation created — appointment #${appointment.id} for ${appointment.patientName || `${patient.firstName} ${patient.lastName}`} (${appointment.patientNumber || patient.patientNumber}) on ${appointmentDate} ${startTime}-${endTime}.`
      );
      await Promise.all([loadTodayAppointments(), loadRecentPatients()]);
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Failed to create appointment.");
    } finally {
      setSaving(false);
    }
  };

  const initials = (name: string) =>
    name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .slice(0, 2)
      .toUpperCase();

  // ── Derived data ─────────────────────────────────────────────────────────

  const statusCounts = useMemo(() => {
    const counts = { Pending: 0, "Checked In": 0, Consulting: 0, Completed: 0 };
    for (const apt of todayAppointments) {
      if (apt.status === "Scheduled") counts.Pending += 1;
      else if (apt.status === "CheckedIn") counts["Checked In"] += 1;
      else if (apt.status === "InProgress") counts.Consulting += 1;
      else if (apt.status === "Completed") counts.Completed += 1;
    }
    return counts;
  }, [todayAppointments]);

  const updatePatient = (field: keyof PatientDraft, value: string) => {
    setPatientDraft((current) => ({ ...current, [field]: value }));
  };

  const updateConsultation = (field: keyof ConsultationDraft, value: string) => {
    setConsultationDraft((current) => ({ ...current, [field]: value }) as ConsultationDraft);
  };

  return (
    <div className="space-y-6">
      {/* Page metrics */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-4">
        <MetricCard label="Pending" value={statusCounts.Pending} icon={<Clock className="h-5 w-5 text-amber-500" />} />
        <MetricCard label="Checked In" value={statusCounts["Checked In"]} icon={<CheckCircle2 className="h-5 w-5 text-blue-600" />} />
        <MetricCard label="Consulting" value={statusCounts.Consulting} icon={<Stethoscope className="h-5 w-5 text-purple-600" />} />
        <MetricCard label="Completed" value={statusCounts.Completed} icon={<CheckCircle2 className="h-5 w-5 text-emerald-600" />} />
      </div>

      {success && (
        <div role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800">{success}</div>
      )}
      {error && (
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">{error}</div>
      )}

      {/* Page header with step progress */}
      <div className="flex items-end justify-between">
        <div>
          <nav className="mb-2 flex gap-2 text-xs text-slate-400">
            <span>Main Clinic</span>
            <span>/</span>
            <span className="font-medium text-blue-800">New Consultation</span>
          </nav>
          <h1 className="text-2xl font-semibold text-slate-900">New Patient Consultation</h1>
          <p className="mt-1 text-sm text-slate-500">Identify or register a patient, then book the consultation with a doctor.</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex h-1.5 w-48 overflow-hidden rounded-full bg-slate-200">
            <div className={selectedPatient ? "w-full rounded-full bg-blue-800" : "w-2/3 rounded-full bg-blue-800"} />
          </div>
          <span className="text-xs font-bold text-slate-500">{selectedPatient ? "PATIENT SELECTED" : "STEP 2 OF 3"}</span>
        </div>
      </div>

      {/* Workflow grid: design-matched layout */}
      <div className="grid grid-cols-1 gap-6 xl:grid-cols-12">
        {/* LEFT COLUMN: Search, AI, Recent Lookups (4 cols) */}
        <div className="space-y-6 xl:col-span-4">
          {/* Step 1: Patient Search */}
          <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="mb-4 text-sm font-semibold text-slate-900">Step 1: Patient Search</h3>
            <div className="relative mb-4">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                value={patientSearch}
                onChange={(event) => setPatientSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    searchPatients();
                  }
                }}
                className="w-full rounded-lg border border-slate-200 bg-slate-50 py-2.5 pl-10 pr-3 text-sm text-slate-700 outline-none transition placeholder:text-slate-400 focus:border-blue-800 focus:bg-white focus:ring-1 focus:ring-blue-800"
                placeholder="Search Name, ID, or Phone..."
                type="search"
                aria-label="Patient search"
              />
            </div>
            <button
              type="button"
              onClick={searchPatients}
              disabled={searchingPatients}
              className="mb-3 flex w-full items-center justify-center gap-2 rounded-lg bg-slate-800 px-4 py-2 text-xs font-bold uppercase tracking-wider text-white transition-opacity hover:opacity-90 disabled:opacity-60"
            >
              <Search className="h-4 w-4" />
              {searchingPatients ? "Searching..." : "Search Patients"}
            </button>
            {patientResults.length > 0 && (
              <div className="mb-4 divide-y divide-slate-100 overflow-hidden rounded-lg border border-slate-200">
                {patientResults.map((patient) => (
                  <button
                    key={patient.id}
                    type="button"
                    onClick={() => selectPatient(patient)}
                    className={`flex w-full items-center justify-between p-3 text-left transition-colors hover:bg-slate-50 ${
                      selectedPatient?.id === patient.id ? "bg-blue-50" : ""
                    }`}
                  >
                    <div>
                      <p className="text-[13px] font-semibold text-slate-900">
                        {patient.firstName} {patient.lastName}
                      </p>
                      <p className="text-[11px] text-slate-400">
                        {patient.patientNumber} · {patient.phone}
                      </p>
                    </div>
                    <CheckCircle2 className={`h-4 w-4 ${selectedPatient?.id === patient.id ? "text-blue-800" : "text-slate-300"}`} />
                  </button>
                ))}
              </div>
            )}
            <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/50 p-3 text-center">
              <p className="mb-2 text-[13px] text-slate-600">Not found? Proceed to registration.</p>
              <button
                type="button"
                onClick={() => {
                  const el = document.getElementById("registration-card");
                  el?.scrollIntoView({ behavior: "smooth" });
                }}
                className="mx-auto flex items-center justify-center gap-1 text-xs font-bold uppercase tracking-wider text-blue-700 hover:underline"
              >
                <PlusCircle className="h-4 w-4" />
                Register New Patient
              </button>
            </div>
          </div>

          {/* AI Assistant Card */}
          <div
            className="rounded-xl border border-purple-200 bg-white p-5 shadow-sm"
            style={{
              boxShadow: "inset 0 0 0 1px rgba(138, 76, 252, 0.3), 0 0 4px rgba(138, 76, 252, 0.1)",
              borderLeftWidth: "4px",
              borderLeftColor: "#8a4cfc",
            }}
          >
            <div className="mb-3 flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-purple-600" />
              <span className="text-xs font-bold uppercase tracking-wider text-purple-700">Clinical AI Helper</span>
            </div>
            <p className="mb-4 text-sm text-slate-600">
              Paste or dictate a short note. It is saved with the appointment so the doctor sees it in the patient&apos;s AI summary.
            </p>
            <textarea
              value={aiNote}
              onChange={(event) => setAiNote(event.target.value)}
              className="min-h-[80px] w-full rounded-lg border border-purple-100 bg-purple-50/40 px-3 py-2 text-sm text-slate-700 outline-none placeholder:text-slate-400 focus:border-purple-500 focus:ring-1 focus:ring-purple-500"
              placeholder="Example: New walk-in, fever since yesterday, history of hypertension."
              aria-label="Clinical note"
            />
          </div>

          {/* Recent Lookups */}
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <h4 className="text-xs font-bold uppercase tracking-widest text-slate-500">Recent Patients</h4>
              <button type="button" onClick={loadRecentPatients} className="text-[11px] text-blue-700 hover:underline">
                Refresh
              </button>
            </div>
            <div className="divide-y divide-slate-50">
              {recentPatients.length === 0 ? (
                <p className="p-4 text-[13px] text-slate-400">No patients registered yet.</p>
              ) : (
                recentPatients.map((patient) => (
                  <button
                    key={patient.id}
                    type="button"
                    onClick={() => selectPatient(patient)}
                    className="flex w-full cursor-pointer items-center justify-between p-4 text-left hover:bg-slate-50"
                  >
                    <div className="flex items-center gap-3">
                      <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-500">
                        {initials(`${patient.firstName} ${patient.lastName}`)}
                      </div>
                      <div>
                        <p className="text-[13px] font-semibold text-slate-900">
                          {patient.firstName} {patient.lastName}
                        </p>
                        <p className="text-[11px] text-slate-400">
                          {patient.patientNumber} · {patient.phone}
                        </p>
                      </div>
                    </div>
                    <ChevronRight className="h-4 w-4 text-slate-300" />
                  </button>
                ))
              )}
            </div>
          </div>
        </div>

        {/* RIGHT COLUMN: Registration + Consultation unified card + AI Summary (8 cols) */}
        <div className="space-y-6 xl:col-span-8">
          {/* Unified Registration + Consultation Card */}
          <div id="registration-card" className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            {/* Step 2 Header */}
            <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/50 px-6 py-4">
              <div className="flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-800 text-sm font-bold text-white">2</div>
                <h3 className="text-base font-semibold text-slate-900">
                  {selectedPatient ? "Selected Patient" : "New Patient Registration"}
                </h3>
              </div>
              {selectedPatient ? (
                <button
                  type="button"
                  onClick={clearSelectedPatient}
                  className="text-xs font-bold uppercase tracking-wider text-blue-700 hover:underline"
                >
                  <UserPlus className="mr-1 inline h-3 w-3" />
                  Change patient
                </button>
              ) : (
                <span className="text-xs italic text-slate-400">
                  {isDoctor ? "Walk-in registration is done at the front desk — search for the patient instead" : "Fields marked * are mandatory"}
                </span>
              )}
            </div>

            {/* Selected patient banner */}
            {selectedPatient ? (
              <div className="flex items-center gap-4 bg-blue-50/60 p-6">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 text-sm font-bold text-blue-800">
                  {initials(`${selectedPatient.firstName} ${selectedPatient.lastName}`)}
                </div>
                <div>
                  <p className="text-base font-semibold text-slate-900">
                    {selectedPatient.firstName} {selectedPatient.lastName}
                  </p>
                  <p className="text-sm text-slate-500">
                    {selectedPatient.patientNumber} · {selectedPatient.phone} · {selectedPatient.gender}
                  </p>
                </div>
              </div>
            ) : (
              /* Registration Fields */
              <div className="p-6">
                <div className="grid grid-cols-2 gap-x-6 gap-y-4">
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-full-name" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Full Name *
                    </label>
                    <input
                      id="nc-full-name"
                      type="text"
                      value={patientDraft.fullName}
                      onChange={(event) => updatePatient("fullName", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                      placeholder="Patient full name"
                    />
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-dob" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Date of Birth
                    </label>
                    <input
                      id="nc-dob"
                      type="date"
                      value={patientDraft.dateOfBirth}
                      onChange={(event) => updatePatient("dateOfBirth", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                    />
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-gender" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Gender
                    </label>
                    <select
                      id="nc-gender"
                      value={patientDraft.gender}
                      onChange={(event) => updatePatient("gender", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                    >
                      <option value="">Select Gender</option>
                      <option value="Female">Female</option>
                      <option value="Male">Male</option>
                      <option value="Other">Other</option>
                    </select>
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-national-id" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      National ID
                    </label>
                    <input
                      id="nc-national-id"
                      type="text"
                      value={patientDraft.nationalId}
                      onChange={(event) => updatePatient("nationalId", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                      placeholder="National ID"
                    />
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-phone" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Phone Number *
                    </label>
                    <div className="relative">
                      <Phone className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                      <input
                        id="nc-phone"
                        type="tel"
                        value={patientDraft.phone}
                        onChange={(event) => updatePatient("phone", event.target.value)}
                        className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                        placeholder="+880 1XXX XXXXXX"
                      />
                    </div>
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <label htmlFor="nc-email" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                      Email Address
                    </label>
                    <input
                      id="nc-email"
                      type="email"
                      value={patientDraft.email}
                      onChange={(event) => updatePatient("email", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                      placeholder="patient@email.com"
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Step 3 Header */}
            <div className="mt-4 flex items-center justify-between border-t border-slate-100 bg-slate-50/50 px-6 py-4">
              <div className="flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-800 text-sm font-bold text-white">3</div>
                <h3 className="text-base font-semibold text-slate-900">Consultation Details</h3>
              </div>
              <span className="text-xs italic text-slate-400">Saved as an appointment</span>
            </div>

            {/* Consultation Details */}
            <div className="bg-white p-6">
              <div className="grid grid-cols-12 gap-x-6 gap-y-4">
                <div className="col-span-12 sm:col-span-6">
                  <label htmlFor="nc-department" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Department *
                  </label>
                  <select
                    id="nc-department"
                    value={consultationDraft.departmentId}
                    onChange={(event) => updateConsultation("departmentId", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  >
                    <option value="">{departments.length ? "Select Department" : "No departments configured"}</option>
                    {departments.map((department) => (
                      <option key={department.id} value={String(department.id)}>
                        {department.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-span-12 sm:col-span-6">
                  <label htmlFor="nc-doctor" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Doctor *
                  </label>
                  <select
                    id="nc-doctor"
                    value={consultationDraft.doctorId}
                    onChange={(event) => updateConsultation("doctorId", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  >
                    <option value="">{doctors.length ? "Select Doctor" : "No doctors configured"}</option>
                    {doctors.map((doctor) => (
                      <option key={doctor.profileId} value={String(doctor.userId)}>
                        Dr. {doctor.firstName} {doctor.lastName} — {doctor.specialization}
                        {doctor.isAcceptingPatients ? "" : " (not accepting)"}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-span-12 sm:col-span-6">
                  <label htmlFor="nc-date" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Date *
                  </label>
                  <input
                    id="nc-date"
                    type="date"
                    value={appointmentDate}
                    onChange={(event) => setAppointmentDate(event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  />
                </div>
                <div className="col-span-12 sm:col-span-6">
                  <label htmlFor="nc-service-type" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Service Type
                  </label>
                  <select
                    id="nc-service-type"
                    value={consultationDraft.serviceType}
                    onChange={(event) => updateConsultation("serviceType", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  >
                    {SERVICE_TYPES.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-span-12">
                  <label className="mb-2 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Available Slots {loadingSlots && <span className="normal-case text-slate-400">(checking doctor&apos;s schedule…)</span>}
                  </label>
                  {slots.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {slots.map((slot) => {
                        const isSelected = startTime === slot.startTime && endTime === slot.endTime;
                        return (
                          <button
                            key={slot.startTime}
                            type="button"
                            onClick={() => {
                              setStartTime(slot.startTime);
                              setEndTime(slot.endTime);
                            }}
                            className={`rounded-lg border px-3 py-1.5 text-sm font-semibold transition-colors ${
                              isSelected
                                ? "border-blue-800 bg-blue-800 text-white"
                                : "border-slate-200 text-slate-600 hover:bg-slate-50"
                            }`}
                          >
                            {slot.startTime}
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <p className="rounded-lg border border-dashed border-slate-300 bg-slate-50/50 p-3 text-[13px] text-slate-500">
                      No published schedule slots for this doctor and date — enter the time manually below.
                    </p>
                  )}
                </div>
                <div className="col-span-6 sm:col-span-3">
                  <label htmlFor="nc-start-time" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Start Time *
                  </label>
                  <input
                    id="nc-start-time"
                    type="time"
                    value={startTime}
                    onChange={(event) => setStartTime(event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  />
                </div>
                <div className="col-span-6 sm:col-span-3">
                  <label htmlFor="nc-end-time" className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    End Time *
                  </label>
                  <input
                    id="nc-end-time"
                    type="time"
                    value={endTime}
                    onChange={(event) => setEndTime(event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  />
                </div>
                <div className="col-span-12 sm:col-span-6">
                  <label className="mb-3 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Priority Level
                  </label>
                  <div className="grid grid-cols-3 gap-4">
                    {([
                      { value: "normal", label: "Normal", Icon: CheckCircle2 },
                      { value: "urgent", label: "Urgent", Icon: AlertCircle },
                      { value: "emergency", label: "Emergency", Icon: Zap },
                    ] as const).map((option) => {
                      const isSelected = consultationDraft.priority === option.value;
                      return (
                        <label
                          key={option.value}
                          className="relative flex cursor-pointer items-center justify-center rounded-lg border border-slate-200 p-3 transition-colors hover:bg-slate-50"
                        >
                          <input
                            type="radio"
                            name="priority"
                            value={option.value}
                            checked={isSelected}
                            onChange={(event) => updateConsultation("priority", event.target.value as Priority)}
                            className="peer hidden"
                          />
                          <div className="flex flex-col items-center gap-1">
                            <option.Icon
                              className={`h-5 w-5 ${isSelected ? "text-blue-800" : "text-slate-300"}`}
                            />
                            <span className={`text-sm font-semibold ${isSelected ? "text-blue-800" : "text-slate-600"}`}>
                              {option.label}
                            </span>
                          </div>
                          <div
                            className={`absolute inset-0 rounded-lg border-2 ${isSelected ? "border-blue-800" : "border-transparent"}`}
                          />
                        </label>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>

            {/* Footer Buttons */}
            <div className="flex justify-end gap-3 border-t border-slate-100 px-6 py-5">
              <button
                type="button"
                onClick={clearForm}
                className="rounded-lg border border-slate-200 px-6 py-2.5 text-sm font-bold text-slate-600 transition-colors hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={saveConsultation}
                disabled={saving}
                className="flex items-center gap-2 rounded-lg bg-blue-800 px-8 py-2.5 text-sm font-bold text-white shadow-sm transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {saving ? "Saving..." : "Create Consultation"}
                <ArrowRight className="h-5 w-5" />
              </button>
            </div>
          </div>

          {/* AI Context Summary */}
          <div className="flex items-start gap-4 rounded-xl border border-blue-100 bg-blue-50/50 p-6">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-blue-100 text-blue-800">
              <Lightbulb className="h-5 w-5" />
            </div>
            <div>
              <h4 className="mb-1 text-sm font-bold text-blue-900">AI Smart Context</h4>
              <p className="text-[13px] leading-relaxed text-slate-600">
                Created consultations appear in Today&apos;s Queue below and on the doctor&apos;s appointments list. Clinical notes
                added here feed the doctor&apos;s AI patient summary.
              </p>
            </div>
          </div>

          {/* Todays Queue */}
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <h3 className="font-semibold text-slate-950">Today&apos;s Queue</h3>
              <button type="button" onClick={loadTodayAppointments} aria-label="Refresh queue">
                <RefreshCw className="h-5 w-5 text-slate-400 hover:text-blue-800" />
              </button>
            </div>
            <div className="divide-y divide-slate-100">
              {todayAppointments.length === 0 ? (
                <p className="p-4 text-[13px] text-slate-400">No consultations booked for today yet.</p>
              ) : (
                todayAppointments.map((appointment) => (
                  <div key={appointment.id} className="p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-semibold text-slate-900">{appointment.patientName || `Patient #${appointment.patientId}`}</p>
                        <p className="mt-0.5 text-xs text-slate-500">
                          #{appointment.patientNumber} &nbsp; {appointment.startTime}-{appointment.endTime} &nbsp; Dr. {appointment.doctorName}
                        </p>
                        <p className="mt-2 text-sm text-slate-600">{appointment.reason || appointment.type}</p>
                      </div>
                      <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-semibold text-slate-600">
                        {appointment.status}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function MetricCard({ label, value, icon }: { label: string; value: number; icon: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p>
      <div className="mt-3 flex items-end justify-between">
        <span className="text-3xl font-semibold text-slate-950">{value}</span>
        {icon}
      </div>
    </div>
  );
}
