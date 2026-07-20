"use client";
import { useEffect, useState } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import { apiClient } from "../../../lib/apiClient";
import { Video, Plus, X, Play, CheckCircle2, XCircle, Clock, ExternalLink, Calendar, User, Stethoscope } from "lucide-react";

interface TelehealthSession {
  id: number;
  patientId: number;
  patientName: string;
  patientNumber: string;
  doctorId: number;
  doctorName: string;
  scheduledAt: string;
  startedAt?: string;
  endedAt?: string;
  status: string;
  joinUrl?: string;
  joinToken?: string;
  notes?: string;
  durationMinutes: number;
  createdAt: string;
}

interface Patient { id: number; patientNumber: string; firstName: string; lastName: string; }
interface Doctor { id: number; user: { firstName: string; lastName: string }; }

const STATUS_COLORS: Record<string, string> = {
  Scheduled: "bg-blue-100 text-blue-800",
  Active:    "bg-emerald-100 text-emerald-800",
  Completed: "bg-slate-100 text-slate-700",
  Cancelled: "bg-red-100 text-red-700",
  NoShow:    "bg-amber-100 text-amber-800",
};

export default function TelemedicinePage() {
  const [sessions, setSessions] = useState<TelehealthSession[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [joinFor, setJoinFor] = useState<TelehealthSession | null>(null);
  const [error, setError] = useState("");

  const load = async () => {
    setLoading(true); setError("");
    try {
      const [s, p, d] = await Promise.all([
        apiClient.get("/api/telemedicine/sessions"),
        apiClient.get("/api/patient?pageSize=100"),
        apiClient.get("/api/doctor?pageSize=100"),
      ]);
      if (s.ok) setSessions(await s.json());
      if (p.ok) { const pp = await p.json(); setPatients(pp.patients || []); }
      if (d.ok) { const dd = await d.json(); setDoctors(dd.items || dd.doctors || []); }
    } catch (e: any) { setError(e.message); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const start = async (id: number) => {
    const r = await apiClient.post(`/api/telemedicine/sessions/${id}/start`, {});
    if (r.ok) { const s = await r.json(); setJoinFor(s); load(); }
  };
  const end = async (id: number) => {
    const r = await apiClient.post(`/api/telemedicine/sessions/${id}/end`, "");
    if (r.ok) load();
  };
  const cancel = async (id: number) => {
    if (!confirm("Cancel this session?")) return;
    const r = await apiClient.post(`/api/telemedicine/sessions/${id}/cancel`, "Cancelled by user");
    if (r.ok) load();
  };

  return (
    <ProtectedRoute allowedRoles={["Admin", "Doctor", "Receptionist", "Patient"]}>
      <div className="min-h-screen bg-slate-50">
        <div className="bg-white border-b border-slate-200 px-6 py-4 flex items-center justify-between sticky top-0 z-20">
          <div className="flex items-center gap-3">
            <Video className="w-6 h-6 text-blue-600" />
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Telemedicine</h1>
              <p className="text-sm text-slate-500">Video consultations (MVP — video provider not yet wired)</p>
            </div>
          </div>
          <button onClick={() => setShowCreate(true)} className="px-3 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 flex items-center gap-1">
            <Plus className="w-4 h-4" /> New session
          </button>
        </div>

        {error && <div className="m-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded">{error}</div>}

        <div className="p-6">
          {loading ? (
            <div className="text-center py-12 text-slate-500">Loading sessions…</div>
          ) : sessions.length === 0 ? (
            <div className="text-center py-12 bg-white rounded-xl border border-slate-200">
              <Video className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <p className="text-slate-500">No telemedicine sessions yet.</p>
              <button onClick={() => setShowCreate(true)} className="mt-4 px-4 py-2 bg-blue-600 text-white rounded">Schedule first session</button>
            </div>
          ) : (
            <div className="space-y-2">
              {sessions.map(s => (
                <div key={s.id} className="bg-white rounded-lg border border-slate-200 p-4 flex items-center gap-4">
                  <div className="text-center min-w-[80px]">
                    <div className="text-xs text-slate-500">{new Date(s.scheduledAt).toLocaleDateString()}</div>
                    <div className="text-lg font-bold text-slate-900">{new Date(s.scheduledAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[s.status] || "bg-slate-100"}`}>{s.status}</span>
                      {s.durationMinutes > 0 && <span className="text-xs text-slate-500">{s.durationMinutes} min</span>}
                    </div>
                    <div className="font-semibold text-slate-900 flex items-center gap-1.5"><User className="w-3.5 h-3.5" /> {s.patientName}</div>
                    <div className="text-sm text-slate-600 flex items-center gap-1.5"><Stethoscope className="w-3.5 h-3.5" /> {s.doctorName}</div>
                    {s.notes && <div className="text-xs text-slate-500 truncate mt-1">"{s.notes}"</div>}
                  </div>
                  <div className="flex gap-1">
                    {s.status === "Scheduled" && (
                      <button onClick={() => start(s.id)} className="px-3 py-1.5 text-sm bg-emerald-600 text-white rounded hover:bg-emerald-700 flex items-center gap-1">
                        <Play className="w-3.5 h-3.5" /> Start
                      </button>
                    )}
                    {s.status === "Active" && (
                      <>
                        <button onClick={() => setJoinFor(s)} className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 flex items-center gap-1">
                          <ExternalLink className="w-3.5 h-3.5" /> Join
                        </button>
                        <button onClick={() => end(s.id)} className="px-3 py-1.5 text-sm bg-slate-100 hover:bg-slate-200 rounded flex items-center gap-1">
                          <CheckCircle2 className="w-3.5 h-3.5" /> End
                        </button>
                      </>
                    )}
                    {s.status === "Scheduled" && (
                      <button onClick={() => cancel(s.id)} className="px-3 py-1.5 text-sm bg-red-50 text-red-700 hover:bg-red-100 rounded flex items-center gap-1">
                        <XCircle className="w-3.5 h-3.5" /> Cancel
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {showCreate && (
          <CreateSessionModal
            patients={patients}
            doctors={doctors}
            onClose={() => setShowCreate(false)}
            onCreated={() => { setShowCreate(false); load(); }}
          />
        )}

        {joinFor && <VideoCallModal session={joinFor} onClose={() => { setJoinFor(null); load(); }} onEnd={end} />}
      </div>
    </ProtectedRoute>
  );
}

function CreateSessionModal({ patients, doctors, onClose, onCreated }: any) {
  const [patientId, setPatientId] = useState("");
  const [doctorId, setDoctorId] = useState("");
  const [scheduledAt, setScheduledAt] = useState(() => {
    const d = new Date(Date.now() + 60 * 60 * 1000);
    return d.toISOString().slice(0, 16);
  });
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post("/api/telemedicine/sessions", {
        patientId: parseInt(patientId),
        doctorId: parseInt(doctorId),
        scheduledAt: new Date(scheduledAt).toISOString(),
        notes: notes || null,
      });
      if (r.ok) onCreated();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Schedule Telemedicine Session" onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Patient</label>
          <select value={patientId} onChange={e => setPatientId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— Select —</option>
            {patients.map((p: Patient) => <option key={p.id} value={p.id}>{p.patientNumber} {p.firstName} {p.lastName}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Doctor</label>
          <select value={doctorId} onChange={e => setDoctorId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— Select —</option>
            {doctors.map((d: Doctor) => <option key={d.id} value={d.id}>Dr. {d.user.firstName} {d.user.lastName}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Scheduled at</label>
          <input type="datetime-local" value={scheduledAt} onChange={e => setScheduledAt(e.target.value)}
            className="w-full px-3 py-2 border border-slate-300 rounded text-sm" />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Notes (optional)</label>
          <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={2} className="w-full px-3 py-2 border border-slate-300 rounded text-sm" />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded">Cancel</button>
          <button onClick={submit} disabled={submitting} className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
            {submitting ? "Scheduling…" : "Schedule"}
          </button>
        </div>
      </div>
    </Modal>
  );
}

function VideoCallModal({ session, onClose, onEnd }: { session: TelehealthSession; onClose: () => void; onEnd: (id: number) => void }) {
  return (
    <Modal title="Video Call" onClose={onClose}>
      <div className="space-y-3">
        <div className="bg-slate-900 rounded-lg aspect-video flex items-center justify-center text-white">
          <div className="text-center">
            <Video className="w-16 h-16 mx-auto mb-3 opacity-50" />
            <p className="text-sm opacity-75">Video provider not yet integrated.</p>
            <p className="text-xs opacity-50 mt-1">Room URL: {session.joinUrl}</p>
          </div>
        </div>
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 text-sm text-blue-900">
          <p className="font-semibold mb-1">MVP placeholder</p>
          <p>This is a stub for the video transport. When a real provider is wired in (Twilio, Daily.co, Jitsi), the room URL above will be opened in an iframe. Session lifecycle (Start/End) is fully functional.</p>
        </div>
        <div className="flex justify-between items-center">
          <div className="text-sm text-slate-600">
            <div><strong>{session.patientName}</strong> with <strong>{session.doctorName}</strong></div>
            <div className="text-xs text-slate-500">Token: {session.joinToken?.slice(0, 12)}…</div>
          </div>
          <button onClick={() => { onEnd(session.id); onClose(); }}
            className="px-4 py-2 text-sm bg-red-600 text-white rounded hover:bg-red-700">
            End Call
          </button>
        </div>
      </div>
    </Modal>
  );
}

function Modal({ title, onClose, children }: any) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-xl shadow-xl w-full max-w-lg" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between p-4 border-b border-slate-200">
          <h3 className="text-lg font-bold text-slate-900">{title}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
        </div>
        <div className="p-4">{children}</div>
      </div>
    </div>
  );
}
