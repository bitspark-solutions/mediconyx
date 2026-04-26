"use client";

import { useState } from "react";
import { apiClient } from "../../lib/apiClient";
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
  department: string;
  serviceType: string;
  priority: "normal" | "urgent" | "emergency";
};

const recentLookups = [
  { id: "#882910-A", name: "Jane Doe" },
  { id: "#119283-B", name: "Mark Smith" },
];

const queueItems = [
  { id: "P-1024", name: "Karim Hassan", time: "10:30 AM", status: "Pending", reason: "Fever and cough" },
  { id: "P-1025", name: "Rania Begum", time: "10:45 AM", status: "Checked In", reason: "BP follow-up" },
  { id: "Walk-in", name: "Unregistered Patient", time: "Now", status: "Direct", reason: "Quick consult" },
];

export default function NewConsultationWorkflow({ role }: NewConsultationWorkflowProps) {
  const [patientSearch, setPatientSearch] = useState("");
  const [aiNote, setAiNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [patientDraft, setPatientDraft] = useState<PatientDraft>({
    fullName: "",
    dateOfBirth: "",
    gender: "",
    nationalId: "",
    phone: "",
    email: "",
  });
  const [consultationDraft, setConsultationDraft] = useState<ConsultationDraft>({
    department: "",
    serviceType: "Initial Consultation",
    priority: "normal",
  });

  const isDoctor = role === "doctor";

  const updatePatient = (field: keyof PatientDraft, value: string) => {
    setPatientDraft((current) => ({ ...current, [field]: value }));
  };

  const updateConsultation = (field: keyof ConsultationDraft, value: string) => {
    setConsultationDraft((current) => ({ ...current, [field]: value }));
  };

  const clearForm = () => {
    setPatientSearch("");
    setAiNote("");
    setPatientDraft({ fullName: "", dateOfBirth: "", gender: "", nationalId: "", phone: "", email: "" });
    setConsultationDraft({ department: "", serviceType: "Initial Consultation", priority: "normal" });
    setError("");
    setSuccess("");
  };

  const savePatient = async () => {
    setError("");
    setSuccess("");

    const nameParts = patientDraft.fullName.trim().split(" ");
    const firstName = nameParts[0] || "";
    const lastName = nameParts.slice(1).join(" ") || "";

    if (!firstName.trim() || !lastName.trim() || !patientDraft.phone.trim()) {
      setError("Full name and phone are required to create a patient.");
      return;
    }

    setSaving(true);
    try {
      const response = await apiClient.post("/api/patient", {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
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

      const savedPatient = await response.json();
      setSuccess(
        isDoctor
          ? `Patient ${savedPatient.patientNumber} saved. You can start consultation now.`
          : `Patient ${savedPatient.patientNumber} saved.`
      );
      clearForm();
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Failed to save patient.");
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

  return (
    <div className="space-y-6">
      {/* Page metrics */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-4">
        <MetricCard label="Pending" value="12" icon={<Clock className="h-5 w-5 text-amber-500" />} />
        <MetricCard label="Checked In" value="7" icon={<CheckCircle2 className="h-5 w-5 text-blue-600" />} />
        <MetricCard label="Consulting" value="3" icon={<Stethoscope className="h-5 w-5 text-purple-600" />} />
        <MetricCard label="Completed" value="18" icon={<CheckCircle2 className="h-5 w-5 text-emerald-600" />} />
      </div>

      {success && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800">{success}</div>
      )}
      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">{error}</div>
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
          <p className="mt-1 text-sm text-slate-500">Initiate a clinical encounter by identifying or registering a patient.</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex h-1.5 w-48 overflow-hidden rounded-full bg-slate-200">
            <div className="w-2/3 rounded-full bg-blue-800" />
          </div>
          <span className="text-xs font-bold text-slate-500">STEP 2 OF 3</span>
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
                className="w-full rounded-lg border border-slate-200 bg-slate-50 py-2.5 pl-10 pr-3 text-sm text-slate-700 outline-none transition placeholder:text-slate-400 focus:border-blue-800 focus:bg-white focus:ring-1 focus:ring-blue-800"
                placeholder="Search Name, ID, or Phone..."
                type="search"
              />
            </div>
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
              Paste or dictate a short note. This area should later auto-fill the minimal patient fields.
            </p>
            <textarea
              value={aiNote}
              onChange={(event) => setAiNote(event.target.value)}
              className="min-h-[80px] w-full rounded-lg border border-purple-100 bg-purple-50/40 px-3 py-2 text-sm text-slate-700 outline-none placeholder:text-slate-400 focus:border-purple-500 focus:ring-1 focus:ring-purple-500"
              placeholder="Example: New walk-in, Ahmed Rahman, phone 017..., fever since yesterday."
            />
            <button
              type="button"
              className="mt-3 flex w-full items-center justify-center gap-2 rounded-lg bg-purple-600 px-4 py-2 text-xs font-bold text-white transition-opacity hover:opacity-90"
            >
              <RefreshCw className="h-4 w-4" />
              Auto-fill from National ID
            </button>
          </div>

          {/* Recent Lookups */}
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <h4 className="text-xs font-bold uppercase tracking-widest text-slate-500">Recent Lookups</h4>
              <span className="cursor-pointer text-[11px] text-blue-700 hover:underline">Clear all</span>
            </div>
            <div className="divide-y divide-slate-50">
              {recentLookups.map((item) => (
                <div key={item.id} className="flex cursor-pointer items-center justify-between p-4 hover:bg-slate-50">
                  <div className="flex items-center gap-3">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-500">
                      {initials(item.name)}
                    </div>
                    <div>
                      <p className="text-[13px] font-semibold text-slate-900">{item.name}</p>
                      <p className="text-[11px] text-slate-400">ID: {item.id}</p>
                    </div>
                  </div>
                  <ChevronRight className="h-4 w-4 text-slate-300" />
                </div>
              ))}
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
                <h3 className="text-base font-semibold text-slate-900">New Patient Registration</h3>
              </div>
              <span className="text-xs italic text-slate-400">Fields marked * are mandatory</span>
            </div>

            {/* Registration Fields */}
            <div className="p-6">
              <div className="grid grid-cols-2 gap-x-6 gap-y-4">
                <div className="col-span-2 sm:col-span-1">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Full Name *
                  </label>
                  <input
                    type="text"
                    value={patientDraft.fullName}
                    onChange={(event) => updatePatient("fullName", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                    placeholder="Patient full name"
                  />
                </div>
                <div className="col-span-2 sm:col-span-1">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Date of Birth
                  </label>
                  <input
                    type="date"
                    value={patientDraft.dateOfBirth}
                    onChange={(event) => updatePatient("dateOfBirth", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  />
                </div>
                <div className="col-span-2 sm:col-span-1">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Gender
                  </label>
                  <select
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
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    National ID
                  </label>
                  <input
                    type="text"
                    value={patientDraft.nationalId}
                    onChange={(event) => updatePatient("nationalId", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                    placeholder="National ID"
                  />
                </div>
                <div className="col-span-2 sm:col-span-1">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Phone Number *
                  </label>
                  <div className="relative">
                    <Phone className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <input
                      type="tel"
                      value={patientDraft.phone}
                      onChange={(event) => updatePatient("phone", event.target.value)}
                      className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                      placeholder="+880 1XXX XXXXXX"
                    />
                  </div>
                </div>
                <div className="col-span-2 sm:col-span-1">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Email Address
                  </label>
                  <input
                    type="email"
                    value={patientDraft.email}
                    onChange={(event) => updatePatient("email", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                    placeholder="patient@email.com"
                  />
                </div>
              </div>
            </div>

            {/* Step 3 Header */}
            <div className="mt-4 flex items-center justify-between border-t border-slate-100 bg-slate-50/50 px-6 py-4">
              <div className="flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-200 text-sm font-bold text-slate-500">3</div>
                <h3 className="text-base font-semibold text-slate-500">Consultation Details</h3>
              </div>
            </div>

            {/* Consultation Details */}
            <div className="bg-white p-6">
              <div className="grid grid-cols-12 gap-x-6 gap-y-4">
                <div className="col-span-12 sm:col-span-6">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Department
                  </label>
                  <select
                    value={consultationDraft.department}
                    onChange={(event) => updateConsultation("department", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  >
                    <option value="">Select Department</option>
                    <option value="Cardiology">Cardiology</option>
                    <option value="General Medicine">General Medicine</option>
                    <option value="Neurology">Neurology</option>
                    <option value="Pediatrics">Pediatrics</option>
                  </select>
                </div>
                <div className="col-span-12 sm:col-span-6">
                  <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Service Type
                  </label>
                  <select
                    value={consultationDraft.serviceType}
                    onChange={(event) => updateConsultation("serviceType", event.target.value)}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
                  >
                    <option>Initial Consultation</option>
                    <option>Follow-up Visit</option>
                    <option>Lab Review</option>
                    <option>Diagnostic Scan</option>
                  </select>
                </div>
                <div className="col-span-12">
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
                            onChange={(event) => updateConsultation("priority", event.target.value as ConsultationDraft["priority"])}
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
                onClick={savePatient}
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
                Based on the patient&apos;s age and selected Department, I recommend checking the Vital Statistics module first. The AI assistant can also extract details from pasted clinical notes.
              </p>
            </div>
          </div>

          {/* Todays Queue */}
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
              <h3 className="font-semibold text-slate-950">Today&apos;s Queue</h3>
              <FileText className="h-5 w-5 text-slate-400" />
            </div>
            <div className="divide-y divide-slate-100">
              {queueItems.map((item) => (
                <div key={item.id} className="p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-slate-900">{item.name}</p>
                      <p className="mt-0.5 text-xs text-slate-500">
                        {item.id} &nbsp; {item.time}
                      </p>
                      <p className="mt-2 text-sm text-slate-600">{item.reason}</p>
                    </div>
                    <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-semibold text-slate-600">
                      {item.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function MetricCard({ label, value, icon }: { label: string; value: string; icon: React.ReactNode }) {
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
