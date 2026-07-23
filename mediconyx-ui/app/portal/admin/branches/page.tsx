"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../../components/ProtectedRoute";
import { apiClient } from "../../../../lib/apiClient";
import { Building2, MapPin, Plus, RefreshCw, Star, X, Pencil, Ban, CheckCircle } from "lucide-react";

interface Branch {
  id: number;
  tenantId: number;
  name: string;
  code: string;
  address?: string;
  city?: string;
  phone?: string;
  email?: string;
  isMain: boolean;
  isActive: boolean;
  createdAt: string;
}

export default function BranchesPage() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [editFor, setEditFor] = useState<Branch | null>(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const r = await apiClient.get("/api/branch?includeInactive=true");
      if (r.ok) setBranches(await r.json());
      else setError(`Failed to load branches (${r.status})`);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const promote = async (b: Branch) => {
    setNotice(""); setError("");
    const r = await apiClient.put(`/api/branch/${b.id}`, { isMain: true });
    if (r.ok) { setNotice(`"${b.name}" is now the main branch.`); load(); }
    else { const d = await r.json().catch(() => ({})); setError(d.message || "Failed to promote"); }
  };

  const toggleActive = async (b: Branch) => {
    setNotice(""); setError("");
    const r = await apiClient.put(`/api/branch/${b.id}`, { isActive: !b.isActive });
    if (r.ok) load();
    else { const d = await r.json().catch(() => ({})); setError(d.message || "Failed to update"); }
  };

  return (
    <ProtectedRoute allowedRoles={["Admin"]}>
      <div className="min-h-screen bg-slate-50">
        <div className="bg-white border-b border-slate-200 px-6 py-4 flex items-center justify-between sticky top-0 z-20">
          <div className="flex items-center gap-3">
            <Building2 className="w-6 h-6 text-indigo-600" />
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Branches</h1>
              <p className="text-sm text-slate-500">Hospital locations — wards, beds and billing are scoped per branch</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={load} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded flex items-center gap-1">
              <RefreshCw className="w-4 h-4" /> Refresh
            </button>
            <button onClick={() => setShowCreate(true)} className="px-3 py-2 text-sm bg-indigo-600 text-white rounded hover:bg-indigo-700 flex items-center gap-1">
              <Plus className="w-4 h-4" /> New Branch
            </button>
          </div>
        </div>

        {error && <div className="m-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded text-sm">{error}</div>}
        {notice && <div className="m-4 p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 rounded text-sm">{notice}</div>}

        <div className="p-6">
          {loading ? (
            <div className="text-center py-12 text-slate-500">Loading branches…</div>
          ) : branches.length === 0 ? (
            <div className="text-center py-12 bg-white rounded-xl border border-slate-200">
              <Building2 className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <p className="text-slate-500">No branches yet.</p>
            </div>
          ) : (
            <div className="grid gap-3">
              {branches.map(b => (
                <div key={b.id} className={`bg-white rounded-xl border-2 p-4 flex items-center justify-between ${b.isMain ? "border-indigo-300" : "border-slate-200"} ${!b.isActive ? "opacity-60" : ""}`}>
                  <div className="flex items-center gap-4">
                    <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${b.isMain ? "bg-indigo-100 text-indigo-700" : "bg-slate-100 text-slate-500"}`}>
                      <Building2 className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-slate-900">{b.name}</span>
                        <span className="text-xs px-1.5 py-0.5 bg-slate-100 rounded text-slate-600 font-mono">{b.code}</span>
                        {b.isMain && <span className="text-xs px-1.5 py-0.5 bg-indigo-600 text-white rounded font-semibold">MAIN</span>}
                        {!b.isActive && <span className="text-xs px-1.5 py-0.5 bg-slate-200 text-slate-600 rounded">Inactive</span>}
                      </div>
                      <div className="text-sm text-slate-500 flex items-center gap-1 mt-0.5">
                        <MapPin className="w-3.5 h-3.5" />
                        {[b.address, b.city].filter(Boolean).join(", ") || "No address"}
                        {b.phone && <span className="ml-2">· {b.phone}</span>}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {!b.isMain && b.isActive && (
                      <button onClick={() => promote(b)} title="Promote to main branch"
                        className="px-2.5 py-1.5 text-xs bg-white border border-slate-300 hover:bg-slate-50 rounded flex items-center gap-1">
                        <Star className="w-3.5 h-3.5" /> Make main
                      </button>
                    )}
                    <button onClick={() => setEditFor(b)} title="Edit"
                      className="px-2.5 py-1.5 text-xs bg-white border border-slate-300 hover:bg-slate-50 rounded flex items-center gap-1">
                      <Pencil className="w-3.5 h-3.5" /> Edit
                    </button>
                    {!b.isMain && (
                      <button onClick={() => toggleActive(b)} title={b.isActive ? "Deactivate" : "Reactivate"}
                        className={`px-2.5 py-1.5 text-xs rounded flex items-center gap-1 border ${b.isActive ? "bg-white border-slate-300 hover:bg-red-50 text-slate-600 hover:text-red-600" : "bg-emerald-50 border-emerald-300 text-emerald-700"}`}>
                        {b.isActive ? <><Ban className="w-3.5 h-3.5" /> Deactivate</> : <><CheckCircle className="w-3.5 h-3.5" /> Reactivate</>}
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {showCreate && <BranchModal onClose={() => setShowCreate(false)} onSaved={() => { setShowCreate(false); load(); }} />}
        {editFor && <BranchModal branch={editFor} onClose={() => setEditFor(null)} onSaved={() => { setEditFor(null); load(); }} />}
      </div>
    </ProtectedRoute>
  );
}

function BranchModal({ branch, onClose, onSaved }: { branch?: Branch; onClose: () => void; onSaved: () => void }) {
  const isEdit = !!branch;
  const [name, setName] = useState(branch?.name ?? "");
  const [code, setCode] = useState(branch?.code ?? "");
  const [address, setAddress] = useState(branch?.address ?? "");
  const [city, setCity] = useState(branch?.city ?? "");
  const [phone, setPhone] = useState(branch?.phone ?? "");
  const [email, setEmail] = useState(branch?.email ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = isEdit
        ? await apiClient.put(`/api/branch/${branch!.id}`, { name, address: address || null, city: city || null, phone: phone || null, email: email || null })
        : await apiClient.post("/api/branch", { name, code, address: address || null, city: city || null, phone: phone || null, email: email || null });
      if (r.ok) onSaved();
      else { const d = await r.json().catch(() => ({})); setError(d.message || "Failed to save"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between p-4 border-b border-slate-200">
          <h3 className="text-lg font-bold text-slate-900">{isEdit ? `Edit — ${branch!.name}` : "New Branch"}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
        </div>
        <div className="p-4 space-y-3">
          {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
          <ModalField label="Name" value={name} onChange={setName} placeholder="City General Uttara" />
          {!isEdit && <ModalField label="Code" value={code} onChange={setCode} placeholder="UTTARA (auto-uppercased, unique per hospital)" />}
          <ModalField label="Address" value={address} onChange={setAddress} placeholder="Sector 7, Uttara" />
          <div className="grid grid-cols-2 gap-3">
            <ModalField label="City" value={city} onChange={setCity} placeholder="Dhaka" />
            <ModalField label="Phone" value={phone} onChange={setPhone} placeholder="+880-1XXX" />
          </div>
          <ModalField label="Email" value={email} onChange={setEmail} placeholder="uttara@hospital.com" />
          <div className="flex justify-end gap-2 pt-3">
            <button onClick={onClose} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded">Cancel</button>
            <button onClick={submit} disabled={submitting || !name.trim() || (!isEdit && !code.trim())}
              className="px-4 py-2 text-sm bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50">
              {submitting ? "Saving…" : isEdit ? "Save" : "Create"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function ModalField({ label, value, onChange, placeholder = "" }: any) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>
      <input type="text" value={value} onChange={e => onChange(e.target.value)} placeholder={placeholder}
        className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-indigo-500" />
    </div>
  );
}
