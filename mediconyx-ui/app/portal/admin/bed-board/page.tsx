"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../../components/ProtectedRoute";
import { apiClient } from "../../../../lib/apiClient";
import { Bed, User, MapPin, RefreshCw, Plus, X, Activity, ArrowRightLeft, LogOut, Stethoscope, Receipt, Sparkles } from "lucide-react";

interface BedBoardWard {
  id: number;
  name: string;
  code: string;
  wardType: string;
  floor: number;
  beds: BedBoardBed[];
}

interface BedBoardBed {
  id: number;
  number: string;
  kind?: string;
  cabinType?: string;
  facilities?: string;
  status: string;
  isActive: boolean;
  admissionId?: number;
  patientId?: number;
  patientName?: string;
  patientNumber?: string;
  attendingDoctorName?: string;
  admissionDate?: string;
}

interface Ward {
  id: number;
  name: string;
  code: string;
  wardType: string;
  capacity: number;
  availableBedCount: number;
  occupiedBedCount: number;
}

interface Doctor {
  id: number;
  user: { firstName: string; lastName: string };
}

interface Patient {
  id: number;
  patientNumber: string;
  firstName: string;
  lastName: string;
}

const WARD_TYPE_COLORS: Record<string, { bg: string; border: string; text: string }> = {
  ICU:    { bg: "bg-red-50",    border: "border-red-300",    text: "text-red-700" },
  PICU:   { bg: "bg-pink-50",   border: "border-pink-300",   text: "text-pink-700" },
  NICU:   { bg: "bg-fuchsia-50",border: "border-fuchsia-300",text: "text-fuchsia-700" },
  DayCare:{ bg: "bg-amber-50",  border: "border-amber-300",  text: "text-amber-700" },
  Maternity:{bg:"bg-rose-50",  border: "border-rose-300",   text: "text-rose-700" },
  Pediatric:{bg:"bg-sky-50",   border: "border-sky-300",    text: "text-sky-700" },
  Geriatric:{bg:"bg-violet-50", border: "border-violet-300", text: "text-violet-700" },
  Surgery:{ bg: "bg-orange-50", border: "border-orange-300", text: "text-orange-700" },
  General:{ bg: "bg-emerald-50",border: "border-emerald-300",text: "text-emerald-700" },
  Cabin:  { bg: "bg-indigo-50", border: "border-indigo-300", text: "text-indigo-700" },
};

const BED_STATUS_STYLES: Record<string, string> = {
  Available:   "bg-emerald-100 border-emerald-400 text-emerald-800 hover:bg-emerald-200",
  Occupied:    "bg-red-100 border-red-400 text-red-800",
  Cleaning:    "bg-amber-100 border-amber-400 text-amber-800",
  Maintenance: "bg-slate-200 border-slate-400 text-slate-700",
  Reserved:    "bg-blue-100 border-blue-400 text-blue-800",
};

export default function BedBoardPage() {
  const [board, setBoard] = useState<BedBoardWard[]>([]);
  const [wards, setWards] = useState<Ward[]>([]);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateWard, setShowCreateWard] = useState(false);
  const [showCreateBed, setShowCreateBed] = useState<number | null>(null);
  const [admitFor, setAdmitFor] = useState<number | null>(null);
  const [transferFor, setTransferFor] = useState<number | null>(null);
  const [dischargeFor, setDischargeFor] = useState<number | null>(null);
  const [billFor, setBillFor] = useState<{ admissionId: number; patientName: string } | null>(null);
  const [selectedAdmission, setSelectedAdmission] = useState<any>(null);
  const [error, setError] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const [b, w, d, p] = await Promise.all([
        apiClient.get("/api/ipd/bed-board"),
        apiClient.get("/api/ipd/wards"),
        apiClient.get("/api/doctor?pageSize=100"),
        apiClient.get("/api/patient?pageSize=100"),
      ]);
      if (b.ok) setBoard(await b.json());
      if (w.ok) setWards(await w.json());
      if (d.ok) {
        const dd = await d.json();
        setDoctors(dd.items || dd.doctors || []);
      }
      if (p.ok) {
        const pp = await p.json();
        setPatients(pp.patients || []);
      }
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  return (
    <ProtectedRoute allowedRoles={["Admin", "Doctor", "Nurse", "Receptionist"]}>
      <div className="min-h-screen bg-slate-50">
        <div className="bg-white border-b border-slate-200 px-6 py-4 flex items-center justify-between sticky top-0 z-20">
          <div className="flex items-center gap-3">
            <Bed className="w-6 h-6 text-blue-600" />
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Bed Board</h1>
              <p className="text-sm text-slate-500">Real-time IPD ward & bed status</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={load} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded flex items-center gap-1">
              <RefreshCw className="w-4 h-4" /> Refresh
            </button>
            <button onClick={() => setShowCreateWard(true)} className="px-3 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 flex items-center gap-1">
              <Plus className="w-4 h-4" /> New Ward
            </button>
          </div>
        </div>

        {error && <div className="m-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded">{error}</div>}

        <div className="p-6">
          {loading ? (
            <div className="text-center py-12 text-slate-500">Loading bed board…</div>
          ) : board.length === 0 ? (
            <div className="text-center py-12 bg-white rounded-xl border border-slate-200">
              <Bed className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <p className="text-slate-500">No wards yet. Create one to get started.</p>
              <button onClick={() => setShowCreateWard(true)} className="mt-4 px-4 py-2 bg-blue-600 text-white rounded">
                Create first ward
              </button>
            </div>
          ) : (
            <div className="space-y-6">
              {board.map((ward) => {
                const colors = WARD_TYPE_COLORS[ward.wardType] || WARD_TYPE_COLORS.General;
                return (
                  <div key={ward.id} className={`rounded-xl border-2 ${colors.border} ${colors.bg} p-4`}>
                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center gap-3">
                        <div className={`px-3 py-1 rounded-full ${colors.text} bg-white border ${colors.border} text-sm font-semibold`}>
                          {ward.wardType}
                        </div>
                        <h2 className="text-xl font-bold text-slate-900">{ward.name}</h2>
                        <span className="text-sm text-slate-500">({ward.code}) · Floor {ward.floor}</span>
                      </div>
                      <div className="flex items-center gap-3">
                        <div className="text-sm">
                          <span className="text-emerald-700 font-semibold">{ward.beds.filter(b => b.status === "Available").length}</span>
                          <span className="text-slate-500"> available · </span>
                          <span className="text-red-700 font-semibold">{ward.beds.filter(b => b.status === "Occupied").length}</span>
                          <span className="text-slate-500"> occupied / {ward.beds.length}</span>
                        </div>
                        <button
                          onClick={() => setShowCreateBed(ward.id)}
                          className="px-3 py-1.5 text-sm bg-white border border-slate-300 hover:bg-slate-50 rounded flex items-center gap-1"
                        >
                          <Plus className="w-3.5 h-3.5" /> {ward.wardType === "Cabin" ? "Cabin" : "Bed"}
                        </button>
                      </div>
                    </div>
                    {ward.beds.length === 0 ? (
                      <div className="text-center py-6 text-slate-400 text-sm">No beds in this ward</div>
                    ) : (
                      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
                        {ward.beds.map((bed) => (
                          <BedCard
                            key={bed.id}
                            bed={bed}
                            onAdmit={() => setAdmitFor(bed.id)}
                            onTransfer={() => {
                              setTransferFor(bed.admissionId!);
                              setSelectedAdmission({ id: bed.admissionId, currentBedId: bed.id });
                            }}
                            onDischarge={() => {
                              setDischargeFor(bed.admissionId!);
                              setSelectedAdmission({ id: bed.admissionId, currentBedId: bed.id });
                            }}
                            onBill={() => {
                              setBillFor({ admissionId: bed.admissionId!, patientName: bed.patientName || "Patient" });
                            }}
                          />
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {showCreateWard && (
          <CreateWardModal
            onClose={() => setShowCreateWard(false)}
            onCreated={() => { setShowCreateWard(false); load(); }}
          />
        )}

        {showCreateBed !== null && (
          <CreateBedModal
            wardId={showCreateBed}
            defaultKind={board.find(w => w.id === showCreateBed)?.wardType === "Cabin" ? "Cabin" : "Bed"}
            onClose={() => setShowCreateBed(null)}
            onCreated={() => { setShowCreateBed(null); load(); }}
          />
        )}

        {admitFor !== null && (
          <AdmitModal
            bedId={admitFor}
            patients={patients}
            doctors={doctors}
            onClose={() => setAdmitFor(null)}
            onCreated={() => { setAdmitFor(null); load(); }}
          />
        )}

        {transferFor !== null && (
          <TransferModal
            admissionId={transferFor}
            wards={wards}
            onClose={() => setTransferFor(null)}
            onDone={() => { setTransferFor(null); load(); }}
          />
        )}

        {dischargeFor !== null && (
          <DischargeModal
            admissionId={dischargeFor}
            doctors={doctors}
            onClose={() => setDischargeFor(null)}
            onDone={() => { setDischargeFor(null); load(); }}
          />
        )}

        {billFor !== null && (
          <BillModal
            admissionId={billFor.admissionId}
            patientName={billFor.patientName}
            onClose={() => setBillFor(null)}
          />
        )}
      </div>
    </ProtectedRoute>
  );
}

function BedCard({ bed, onAdmit, onTransfer, onDischarge, onBill }: {
  bed: BedBoardBed;
  onAdmit: () => void;
  onTransfer: () => void;
  onDischarge: () => void;
  onBill: () => void;
}) {
  const style = BED_STATUS_STYLES[bed.status] || BED_STATUS_STYLES.Available;
  let facilities: string[] = [];
  try { if (bed.facilities) facilities = JSON.parse(bed.facilities); } catch { /* ignore malformed */ }
  const isCabin = bed.kind === "Cabin";
  return (
    <div className={`rounded-lg border-2 p-3 ${style} transition-all`} title={isCabin && facilities.length > 0 ? `Facilities: ${facilities.join(", ")}` : undefined}>
      <div className="flex items-center justify-between mb-2">
        <span className="font-bold text-sm">{bed.number}</span>
        <span className="text-xs font-medium uppercase">{bed.status}</span>
      </div>
      {isCabin && (
        <div className="mb-2 flex items-center gap-1 flex-wrap">
          <span className="text-xs px-1.5 py-0.5 bg-indigo-600 text-white rounded font-semibold">
            {bed.cabinType || "Cabin"}
          </span>
          {facilities.slice(0, 2).map(f => (
            <span key={f} className="text-[10px] px-1 py-0.5 bg-white/70 rounded border border-current/20">{f}</span>
          ))}
          {facilities.length > 2 && (
            <span className="text-[10px] opacity-70">+{facilities.length - 2} more</span>
          )}
        </div>
      )}
      {bed.status === "Occupied" && bed.patientName ? (
        <div className="space-y-1 mb-2">
          <div className="text-sm font-semibold truncate" title={bed.patientName}>
            {bed.patientName}
          </div>
          <div className="text-xs opacity-75">{bed.patientNumber}</div>
          {bed.attendingDoctorName && (
            <div className="text-xs flex items-center gap-1 opacity-75">
              <Stethoscope className="w-3 h-3" /> {bed.attendingDoctorName}
            </div>
          )}
          <div className="grid grid-cols-3 gap-1 mt-2">
            <button onClick={onTransfer} className="text-xs px-1.5 py-1 bg-white/80 hover:bg-white rounded border border-current/20">
              Transfer
            </button>
            <button onClick={onBill} className="text-xs px-1.5 py-1 bg-white/80 hover:bg-white rounded border border-current/20 flex items-center justify-center gap-1" title="View bill">
              <Receipt className="w-3 h-3" /> Bill
            </button>
            <button onClick={onDischarge} className="text-xs px-1.5 py-1 bg-white/80 hover:bg-white rounded border border-current/20">
              D/C
            </button>
          </div>
        </div>
      ) : bed.status === "Available" ? (
        <button onClick={onAdmit} className="w-full text-xs px-2 py-1.5 bg-emerald-600 text-white rounded hover:bg-emerald-700 font-medium">
          + Admit Patient
        </button>
      ) : (
        <div className="text-xs opacity-60 italic text-center py-1">
          {bed.status === "Cleaning" ? "Awaiting cleaning" : bed.status === "Maintenance" ? "Under maintenance" : "Reserved"}
        </div>
      )}
    </div>
  );
}

function CreateWardModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [wardType, setWardType] = useState("General");
  const [floor, setFloor] = useState("1");
  const [capacity, setCapacity] = useState("10");
  const [location, setLocation] = useState("");
  const [defaultDailyRate, setDefaultDailyRate] = useState("0");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true);
    setError("");
    try {
      const r = await apiClient.post("/api/ipd/wards", {
        name, code, wardType, floor: parseInt(floor), capacity: parseInt(capacity),
        location: location || null, defaultDailyRate: parseFloat(defaultDailyRate) || 0,
      });
      if (r.ok) onCreated();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Create Ward" onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <Field label="Name" value={name} onChange={setName} placeholder="ICU Main" />
        <Field label="Code" value={code} onChange={setCode} placeholder="ICU-1" />
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Type</label>
          <select value={wardType} onChange={e => setWardType(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            {["General","ICU","PICU","NICU","DayCare","Maternity","Pediatric","Geriatric","Surgery","Cabin"].map(t =>
              <option key={t} value={t}>{t}</option>
            )}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Floor" value={floor} onChange={setFloor} type="number" />
          <Field label="Capacity" value={capacity} onChange={setCapacity} type="number" />
        </div>
        <Field label="Default daily rate (BDT)" value={defaultDailyRate} onChange={setDefaultDailyRate} type="number" placeholder="e.g. 5000" />
        <Field label="Location (optional)" value={location} onChange={setLocation} placeholder="Block A Floor 2" />
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Create" />
      </div>
    </Modal>
  );
}

interface CabinTypeInfo {
  type: string;
  defaultFacilities: string[];
  typicalRateMinBdt: number;
  typicalRateMaxBdt: number;
}

function CreateBedModal({ wardId, defaultKind = "Bed", onClose, onCreated }: { wardId: number; defaultKind?: "Bed" | "Cabin"; onClose: () => void; onCreated: () => void }) {
  const [number, setNumber] = useState("");
  const [dailyRate, setDailyRate] = useState("");
  const [kind, setKind] = useState<"Bed" | "Cabin">(defaultKind);
  const [cabinTypes, setCabinTypes] = useState<CabinTypeInfo[]>([]);
  const [cabinType, setCabinType] = useState("");
  const [facilities, setFacilities] = useState<string[]>([]);
  const [newFacility, setNewFacility] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (kind !== "Cabin") return;
    apiClient.get("/api/ipd/cabin-types").then(async r => {
      if (r.ok) {
        const types: CabinTypeInfo[] = await r.json();
        setCabinTypes(types);
        if (!cabinType && types.length > 0) {
          setCabinType(types[1]?.type ?? types[0].type); // default to "Standard"
          setFacilities((types[1] ?? types[0]).defaultFacilities);
        }
      }
    });
  }, [kind]);

  const selectCabinType = (t: string) => {
    setCabinType(t);
    const info = cabinTypes.find(x => x.type === t);
    if (info) setFacilities(info.defaultFacilities);
  };

  const toggleFacility = (f: string) =>
    setFacilities(prev => prev.includes(f) ? prev.filter(x => x !== f) : [...prev, f]);

  const selectedType = cabinTypes.find(x => x.type === cabinType);

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post("/api/ipd/beds", {
        wardId, number, status: "Available",
        dailyRate: dailyRate ? parseFloat(dailyRate) : null,
        kind,
        cabinType: kind === "Cabin" ? cabinType : null,
        facilities: kind === "Cabin" ? facilities : null,
      });
      if (r.ok) onCreated();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title={kind === "Cabin" ? "Add Cabin" : "Add Bed"} onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}

        <div className="grid grid-cols-2 gap-2">
          {(["Bed", "Cabin"] as const).map(k => (
            <button
              key={k}
              onClick={() => setKind(k)}
              className={`px-3 py-2 text-sm rounded border-2 font-medium ${kind === k ? "border-blue-500 bg-blue-50 text-blue-700" : "border-slate-200 text-slate-500 hover:border-slate-300"}`}
            >
              {k === "Bed" ? "Ward Bed" : "Private Cabin"}
            </button>
          ))}
        </div>

        <Field label={kind === "Cabin" ? "Cabin Number" : "Bed Number"} value={number} onChange={setNumber} placeholder={kind === "Cabin" ? "CAB-201" : "ICU-1-A"} />

        {kind === "Cabin" && (
          <>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Cabin Type</label>
              <select value={cabinType} onChange={e => selectCabinType(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
                {cabinTypes.map(t => <option key={t.type} value={t.type}>{t.type} (typ. {t.typicalRateMinBdt}–{t.typicalRateMaxBdt} BDT/day)</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Facilities</label>
              <div className="flex flex-wrap gap-1.5">
                {(selectedType?.defaultFacilities ?? facilities).map(f => (
                  <button
                    key={f}
                    onClick={() => toggleFacility(f)}
                    className={`text-xs px-2 py-1 rounded border ${facilities.includes(f) ? "bg-indigo-600 text-white border-indigo-600" : "bg-white text-slate-600 border-slate-300"}`}
                  >
                    {f}
                  </button>
                ))}
                {facilities.filter(f => !(selectedType?.defaultFacilities ?? []).includes(f)).map(f => (
                  <button
                    key={f}
                    onClick={() => toggleFacility(f)}
                    className="text-xs px-2 py-1 rounded border bg-indigo-600 text-white border-indigo-600"
                  >
                    {f}
                  </button>
                ))}
              </div>
              <div className="flex gap-2 mt-2">
                <input
                  type="text"
                  value={newFacility}
                  onChange={e => setNewFacility(e.target.value)}
                  placeholder="Custom facility (e.g. Balcony)"
                  className="flex-1 px-3 py-1.5 border border-slate-300 rounded text-sm"
                />
                <button
                  onClick={() => { const f = newFacility.trim(); if (f && !facilities.includes(f)) setFacilities([...facilities, f]); setNewFacility(""); }}
                  className="px-3 py-1.5 text-sm bg-slate-100 hover:bg-slate-200 rounded"
                >
                  Add
                </button>
              </div>
            </div>
          </>
        )}

        <Field label="Daily rate override (BDT, blank = inherit ward)" value={dailyRate} onChange={setDailyRate} type="number" placeholder={selectedType ? `e.g. ${selectedType.typicalRateMinBdt}` : "e.g. 6000"} />
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Create" />
      </div>
    </Modal>
  );
}

function AdmitModal({ bedId, patients, doctors, onClose, onCreated }: any) {
  const [patientId, setPatientId] = useState("");
  const [attendingDoctorId, setAttendingDoctorId] = useState("");
  const [admissionType, setAdmissionType] = useState("Elective");
  const [reason, setReason] = useState("");
  const [diagnosis, setDiagnosis] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post("/api/ipd/admissions", {
        patientId: parseInt(patientId), bedId, attendingDoctorId: parseInt(attendingDoctorId),
        admissionType, reason: reason || null, diagnosisOnAdmission: diagnosis || null,
      });
      if (r.ok) onCreated();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Admit Patient" onClose={onClose}>
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
          <label className="block text-sm font-medium text-slate-700 mb-1">Attending Doctor</label>
          <select value={attendingDoctorId} onChange={e => setAttendingDoctorId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— Select —</option>
            {doctors.map((d: Doctor) => <option key={d.id} value={d.id}>Dr. {d.user.firstName} {d.user.lastName}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Type</label>
          <select value={admissionType} onChange={e => setAdmissionType(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            {["Elective","Emergency","DayCare","Transfer"].map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
        <Field label="Reason" value={reason} onChange={setReason} placeholder="Post-op recovery" />
        <Field label="Diagnosis on admission" value={diagnosis} onChange={setDiagnosis} placeholder="Optional" />
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Admit" />
      </div>
    </Modal>
  );
}

function TransferModal({ admissionId, wards, onClose, onDone }: any) {
  const [toBedId, setToBedId] = useState("");
  const [reason, setReason] = useState("");
  const [availableBeds, setAvailableBeds] = useState<any[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    apiClient.get("/api/ipd/beds?status=Available").then(async r => { if (r.ok) setAvailableBeds(await r.json()); });
  }, []);

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post(`/api/ipd/admissions/${admissionId}/transfer`, {
        toBedId: parseInt(toBedId), reason: reason || null,
      });
      if (r.ok) onDone();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Transfer Patient" onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Destination bed (Available only)</label>
          <select value={toBedId} onChange={e => setToBedId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— Select —</option>
            {availableBeds.map((b: any) => {
              const w = wards.find((x: Ward) => x.id === b.wardId);
              return <option key={b.id} value={b.id}>{w?.name} · {b.number} ({w?.wardType})</option>;
            })}
          </select>
        </div>
        <Field label="Reason" value={reason} onChange={setReason} placeholder="Stabilized, move to recovery" />
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Transfer" />
      </div>
    </Modal>
  );
}

function DischargeModal({ admissionId, doctors, onClose, onDone }: any) {
  const [dischargingDoctorId, setDischargingDoctorId] = useState("");
  const [conditionAtDischarge, setConditionAtDischarge] = useState("Improved");
  const [dischargeInstructions, setDischargeInstructions] = useState("");
  const [medications, setMedications] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post(`/api/ipd/admissions/${admissionId}/discharge`, {
        dischargingDoctorId: parseInt(dischargingDoctorId),
        conditionAtDischarge,
        dischargeInstructions: dischargeInstructions || null,
        medicationsOnDischarge: medications || null,
      });
      if (r.ok) onDone();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Discharge Patient" onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Discharging Doctor</label>
          <select value={dischargingDoctorId} onChange={e => setDischargingDoctorId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— Select —</option>
            {doctors.map((d: Doctor) => <option key={d.id} value={d.id}>Dr. {d.user.firstName} {d.user.lastName}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Condition at discharge</label>
          <select value={conditionAtDischarge} onChange={e => setConditionAtDischarge(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            {["Improved","Stable","Critical","LAMA","Deceased"].map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Discharge instructions</label>
          <textarea value={dischargeInstructions} onChange={e => setDischargeInstructions(e.target.value)} rows={3} className="w-full px-3 py-2 border border-slate-300 rounded text-sm" />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Medications on discharge</label>
          <textarea value={medications} onChange={e => setMedications(e.target.value)} rows={2} className="w-full px-3 py-2 border border-slate-300 rounded text-sm" />
        </div>
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Discharge" />
      </div>
    </Modal>
  );
}

function Modal({ title, onClose, children }: any) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between p-4 border-b border-slate-200">
          <h3 className="text-lg font-bold text-slate-900">{title}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
        </div>
        <div className="p-4">{children}</div>
      </div>
    </div>
  );
}

function ModalActions({ onClose, onSubmit, submitting, submitText }: any) {
  return (
    <div className="flex justify-end gap-2 pt-3">
      <button onClick={onClose} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded">Cancel</button>
      <button onClick={onSubmit} disabled={submitting} className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
        {submitting ? "Saving…" : submitText}
      </button>
    </div>
  );
}

function BillModal({ admissionId, patientName, onClose }: { admissionId: number; patientName: string; onClose: () => void }) {
  const [bill, setBill] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState("");
  const [lastResult, setLastResult] = useState<any>(null);

  const load = async () => {
    setLoading(true); setError("");
    try {
      const r = await apiClient.get(`/api/ipd/admissions/${admissionId}/bill`);
      if (r.ok) setBill(await r.json());
      else if (r.status === 404) setBill(null);
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed to load bill"); }
    } catch (e: any) { setError(e.message); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [admissionId]);

  const generate = async () => {
    setGenerating(true); setError("");
    try {
      const r = await apiClient.post(`/api/ipd/admissions/${admissionId}/generate-bill`, {});
      if (r.ok) {
        const result = await r.json();
        setLastResult(result);
        await load();
      } else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed to generate bill"); }
    } catch (e: any) { setError(e.message); }
    finally { setGenerating(false); }
  };

  return (
    <Modal title={`Bill — ${patientName}`} onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}

        {lastResult && (
          <div className="p-2 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded text-sm flex items-center gap-2">
            <Sparkles className="w-4 h-4" />
            Generated {lastResult.newChargesCreated} new charge(s) for {lastResult.totalChargeDays} day(s). Total: {lastResult.totalAmount} BDT.
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="text-sm text-slate-600">
            {bill ? `${bill.totalDays} day(s) billed · ${bill.currency}` : "No charges yet."}
          </div>
          <button
            onClick={generate}
            disabled={generating}
            className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-1"
          >
            <Receipt className="w-3.5 h-3.5" />
            {generating ? "Generating…" : (bill && bill.totalDays > 0 ? "Refresh bill" : "Generate bill")}
          </button>
        </div>

        {loading ? (
          <div className="text-center py-6 text-slate-500 text-sm">Loading bill…</div>
        ) : bill && bill.charges && bill.charges.length > 0 ? (
          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-slate-700">
                <tr>
                  <th className="text-left p-2 font-medium">Date</th>
                  <th className="text-left p-2 font-medium">Bed / Ward</th>
                  <th className="text-right p-2 font-medium">Rate</th>
                  <th className="text-right p-2 font-medium">Amount</th>
                </tr>
              </thead>
              <tbody>
                {bill.charges.map((c: any) => (
                  <tr key={c.id} className="border-t border-slate-100">
                    <td className="p-2 text-slate-700">{new Date(c.chargeDate).toLocaleDateString()}</td>
                    <td className="p-2">
                      <div className="text-slate-900">{c.bedNumber}</div>
                      <div className="text-xs text-slate-500">{c.wardName} ({c.wardType})</div>
                    </td>
                    <td className="p-2 text-right text-slate-700">{c.dailyRate}</td>
                    <td className="p-2 text-right font-semibold text-slate-900">{c.amount} {bill.currency}</td>
                  </tr>
                ))}
                <tr className="bg-slate-50 border-t border-slate-200 font-bold">
                  <td colSpan={3} className="p-2 text-right">Total</td>
                  <td className="p-2 text-right text-blue-700">{bill.totalAmount} {bill.currency}</td>
                </tr>
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-6 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-500">
            <Receipt className="w-8 h-8 text-slate-300 mx-auto mb-2" />
            No charges yet. Click "Generate bill" to bill this admission.
          </div>
        )}

        <div className="flex justify-end pt-2">
          <button onClick={onClose} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded">Close</button>
        </div>
      </div>
    </Modal>
  );
}

function Field({ label, value, onChange, type = "text", placeholder = "" }: any) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>
      <input
        type={type}
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
      />
    </div>
  );
}
