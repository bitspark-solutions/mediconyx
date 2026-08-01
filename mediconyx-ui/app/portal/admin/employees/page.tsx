"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../../components/ProtectedRoute";
import { apiClient } from "../../../../lib/apiClient";
import { useAuthStore } from "../../../../store/useAuthStore";
import { Users, RefreshCw, Plus, X, Pencil, Stethoscope, UserX, UserCheck, Mail } from "lucide-react";

interface Employee {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: string;
  designation?: string | null;
  primaryDepartmentId?: number | null;
  primaryDepartmentName?: string | null;
  additionalRoles: string[];
  isActive: boolean;
}

interface Department {
  id: number;
  name: string;
  code: string;
}

// UserRole enum values (API has no string enum converter — send numbers)
const STAFF_ROLES = [
  { value: 1, label: "Admin" },
  { value: 2, label: "Doctor" },
  { value: 3, label: "Nurse" },
  { value: 4, label: "Receptionist" },
  { value: 5, label: "Accountant" },
];

const SPECIALIZATIONS = [
  "General", "Cardiology", "Orthopedics", "Neurology", "Pediatrics", "Dermatology",
  "Internal Medicine", "Surgery", "Radiology", "Pathology", "Psychiatry",
  "Ophthalmology", "ENT", "Nephrology", "Gastroenterology", "Pulmonology",
  "Endocrinology", "Oncology", "Other",
];

export default function EmployeesPage() {
  const { user } = useAuthStore();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [showInvite, setShowInvite] = useState(false);
  const [editFor, setEditFor] = useState<Employee | null>(null);
  const [doctorFor, setDoctorFor] = useState<Employee | null>(null);
  const [error, setError] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const [u, d] = await Promise.all([
        apiClient.get("/api/UserManagement?pageSize=100"),
        apiClient.get("/api/department?pageSize=100"),
      ]);
      if (u.ok) {
        const uu = await u.json();
        setEmployees(uu.users || []);
      }
      if (d.ok) {
        const dd = await d.json();
        setDepartments(dd.departments || []);
      }
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const toggleActive = async (emp: Employee) => {
    try {
      const r = await apiClient.patch(`/api/UserManagement/${emp.id}/toggle-active`, {});
      if (r.ok) load();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
  };

  const filtered = employees.filter(e =>
    `${e.firstName} ${e.lastName} ${e.email} ${e.role} ${e.designation ?? ""}`
      .toLowerCase().includes(search.toLowerCase())
  );

  return (
    <ProtectedRoute allowedRoles={["Admin"]}>
      <div className="min-h-screen bg-slate-50">
        <div className="bg-white border-b border-slate-200 px-6 py-4 flex items-center justify-between sticky top-0 z-20">
          <div className="flex items-center gap-3">
            <Users className="w-6 h-6 text-blue-600" />
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Employees</h1>
              <p className="text-sm text-slate-500">Staff accounts, roles & designations</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={load} className="px-3 py-2 text-sm bg-slate-100 hover:bg-slate-200 rounded flex items-center gap-1">
              <RefreshCw className="w-4 h-4" /> Refresh
            </button>
            <button onClick={() => setShowInvite(true)} className="px-3 py-2 text-sm bg-emerald-600 text-white rounded hover:bg-emerald-700 flex items-center gap-1">
              <Mail className="w-4 h-4" /> Invite by Email
            </button>
            <button onClick={() => setShowCreate(true)} className="px-3 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 flex items-center gap-1">
              <Plus className="w-4 h-4" /> New Employee
            </button>
          </div>
        </div>

        {error && <div className="m-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded">{error}</div>}

        <div className="p-6">
          <div className="mb-4 flex items-center justify-between">
            <input
              type="text"
              placeholder="Search employees..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="px-3 py-2 border border-slate-300 rounded text-sm w-72 focus:outline-none focus:border-blue-500"
            />
            <p className="text-xs text-slate-400">
              Role changes take effect after the employee logs out and back in.
            </p>
          </div>

          {loading ? (
            <div className="text-center py-12 text-slate-500">Loading employees…</div>
          ) : filtered.length === 0 ? (
            <div className="text-center py-12 bg-white rounded-xl border border-slate-200">
              <Users className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <p className="text-slate-500">No employees found.</p>
              <button onClick={() => setShowCreate(true)} className="mt-4 px-4 py-2 bg-blue-600 text-white rounded">
                Add first employee
              </button>
            </div>
          ) : (
            <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-slate-600 text-xs uppercase">
                  <tr>
                    <th className="px-4 py-3 text-left">Name</th>
                    <th className="px-4 py-3 text-left">Email</th>
                    <th className="px-4 py-3 text-left">Role</th>
                    <th className="px-4 py-3 text-left">Designation</th>
                    <th className="px-4 py-3 text-left">Department</th>
                    <th className="px-4 py-3 text-left">Status</th>
                    <th className="px-4 py-3 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(emp => (
                    <tr key={emp.id} className="border-t border-slate-100 hover:bg-slate-50">
                      <td className="px-4 py-3 font-medium text-slate-900">{emp.firstName} {emp.lastName}</td>
                      <td className="px-4 py-3 text-slate-600">{emp.email}</td>
                      <td className="px-4 py-3">
                        <span className="px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                          {emp.role}
                        </span>
                        {emp.additionalRoles.map(r => (
                          <span key={r} className="ml-1 px-2 py-1 rounded-full text-xs font-medium bg-violet-100 text-violet-700">
                            +{r}
                          </span>
                        ))}
                      </td>
                      <td className="px-4 py-3 text-slate-600">{emp.designation || "—"}</td>
                      <td className="px-4 py-3 text-slate-600">{emp.primaryDepartmentName || "—"}</td>
                      <td className="px-4 py-3">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                          emp.isActive ? "bg-emerald-100 text-emerald-700" : "bg-slate-200 text-slate-500"
                        }`}>
                          {emp.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => setEditFor(emp)}
                            title="Edit employee"
                            className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 rounded"
                          >
                            <Pencil className="w-4 h-4" />
                          </button>
                          {emp.isActive && (
                            <button
                              onClick={() => setDoctorFor(emp)}
                              title="Make practicing doctor"
                              className="p-1.5 text-slate-500 hover:text-emerald-600 hover:bg-emerald-50 rounded"
                            >
                              <Stethoscope className="w-4 h-4" />
                            </button>
                          )}
                          {emp.id !== user?.id && (
                            <button
                              onClick={() => toggleActive(emp)}
                              title={emp.isActive ? "Deactivate" : "Reactivate"}
                              className={`p-1.5 rounded ${
                                emp.isActive
                                  ? "text-slate-500 hover:text-red-600 hover:bg-red-50"
                                  : "text-slate-500 hover:text-emerald-600 hover:bg-emerald-50"
                              }`}
                            >
                              {emp.isActive ? <UserX className="w-4 h-4" /> : <UserCheck className="w-4 h-4" />}
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {showCreate && (
          <EmployeeFormModal
            departments={departments}
            tenantId={user?.tenantId ?? 0}
            onClose={() => setShowCreate(false)}
            onSaved={() => { setShowCreate(false); load(); }}
          />
        )}

        {showInvite && (
          <InviteEmployeeModal onClose={() => setShowInvite(false)} />
        )}

        {editFor && (
          <EmployeeFormModal
            departments={departments}
            tenantId={user?.tenantId ?? 0}
            employee={editFor}
            onClose={() => setEditFor(null)}
            onSaved={() => { setEditFor(null); load(); }}
          />
        )}

        {doctorFor && (
          <AttachDoctorModal
            employee={doctorFor}
            departments={departments}
            onClose={() => setDoctorFor(null)}
            onSaved={() => { setDoctorFor(null); load(); }}
          />
        )}
      </div>
    </ProtectedRoute>
  );
}

function EmployeeFormModal({ departments, tenantId, employee, onClose, onSaved }: {
  departments: Department[];
  tenantId: number;
  employee?: Employee;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = !!employee;
  const [firstName, setFirstName] = useState(employee?.firstName ?? "");
  const [lastName, setLastName] = useState(employee?.lastName ?? "");
  const [email, setEmail] = useState(employee?.email ?? "");
  const [password, setPassword] = useState("");
  const [phone, setPhone] = useState(employee?.phoneNumber ?? "");
  const [role, setRole] = useState(
    STAFF_ROLES.find(r => r.label === employee?.role)?.value ?? 4
  );
  const [designation, setDesignation] = useState(employee?.designation ?? "");
  const [departmentId, setDepartmentId] = useState(employee?.primaryDepartmentId?.toString() ?? "");
  const [additionalRoles, setAdditionalRoles] = useState<number[]>(
    (employee?.additionalRoles ?? [])
      .map(r => STAFF_ROLES.find(x => x.label === r)?.value)
      .filter((v): v is number => v !== undefined)
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const toggleRole = (v: number) =>
    setAdditionalRoles(prev => prev.includes(v) ? prev.filter(x => x !== v) : [...prev, v]);

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const payload: any = {
        firstName, lastName,
        phoneNumber: phone || null,
        role,
        designation: designation || null,
        primaryDepartmentId: departmentId ? parseInt(departmentId) : null,
        additionalRoles,
      };
      const r = isEdit
        ? await apiClient.put(`/api/UserManagement/${employee!.id}`, payload)
        : await apiClient.post("/api/UserManagement", { ...payload, email, password, tenantId });
      if (r.ok) onSaved();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title={isEdit ? `Edit — ${employee!.firstName} ${employee!.lastName}` : "New Employee"} onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <div className="grid grid-cols-2 gap-3">
          <Field label="First name" value={firstName} onChange={setFirstName} placeholder="Ayesha" />
          <Field label="Last name" value={lastName} onChange={setLastName} placeholder="Rahman" />
        </div>
        {!isEdit && (
          <>
            <Field label="Email" value={email} onChange={setEmail} type="email" placeholder="ayesha@hospital.com" />
            <Field label="Temporary password" value={password} onChange={setPassword} type="password" placeholder="Min. 6 characters" />
          </>
        )}
        <Field label="Phone" value={phone} onChange={setPhone} placeholder="+880…" />
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Role</label>
            <select value={role} onChange={e => setRole(parseInt(e.target.value))} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
              {STAFF_ROLES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Department</label>
            <select value={departmentId} onChange={e => setDepartmentId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
              <option value="">— None —</option>
              {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
        </div>
        <Field label="Designation (free text)" value={designation} onChange={setDesignation} placeholder="e.g. Director of Cardiology, Head Nurse" />
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Additional roles</label>
          <div className="flex flex-wrap gap-1.5">
            {STAFF_ROLES.filter(r => r.value !== role).map(r => (
              <button
                key={r.value}
                onClick={() => toggleRole(r.value)}
                className={`text-xs px-2 py-1 rounded border ${
                  additionalRoles.includes(r.value)
                    ? "bg-violet-600 text-white border-violet-600"
                    : "bg-white text-slate-600 border-slate-300"
                }`}
              >
                {r.label}
              </button>
            ))}
          </div>
          <p className="text-xs text-slate-400 mt-2">
            Role changes take effect after the employee logs out and back in.
          </p>
        </div>
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText={isEdit ? "Save" : "Create"} />
      </div>
    </Modal>
  );
}

function InviteEmployeeModal({ onClose }: { onClose: () => void }) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState(4);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [sent, setSent] = useState("");

  const submit = async () => {
    if (!email.trim()) { setError("Email is required."); return; }
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post("/api/UserManagement/invite", { email, role });
      const b = await r.json().catch(() => ({}));
      if (r.ok) setSent(b.message || `Invitation sent to ${email}.`);
      else setError(b.message || "Failed to send invitation");
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title="Invite employee by email" onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        {sent ? (
          <>
            <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 rounded text-sm">
              {sent} They'll set their own password from the link in the email.
            </div>
            <div className="flex justify-end pt-1">
              <button onClick={onClose} className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700">
                Done
              </button>
            </div>
          </>
        ) : (
          <>
            <p className="text-xs text-slate-500">
              Sends an invitation email. The employee sets their own password and can log in immediately after accepting.
            </p>
            <Field label="Email" value={email} onChange={setEmail} type="email" placeholder="ayesha@hospital.com" />
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Role</label>
              <select value={role} onChange={e => setRole(parseInt(e.target.value))} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
                {STAFF_ROLES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
              </select>
            </div>
            <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Send invite" />
          </>
        )}
      </div>
    </Modal>
  );
}

function AttachDoctorModal({ employee, departments, onClose, onSaved }: {
  employee: Employee;
  departments: Department[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [license, setLicense] = useState("");
  const [specialization, setSpecialization] = useState(SPECIALIZATIONS[0]);
  const [departmentId, setDepartmentId] = useState(employee.primaryDepartmentId?.toString() ?? "");
  const [duration, setDuration] = useState("30");
  const [fee, setFee] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    setSubmitting(true); setError("");
    try {
      const r = await apiClient.post("/api/doctor/attach-profile", {
        userId: employee.id,
        medicalLicenseNumber: license || null,
        specialization,
        departmentId: departmentId ? parseInt(departmentId) : null,
        consultationDurationMinutes: parseInt(duration) || 30,
        consultationFee: fee ? parseFloat(fee) : null,
      });
      if (r.ok) onSaved();
      else { const b = await r.json().catch(() => ({})); setError(b.message || "Failed"); }
    } catch (e: any) { setError(e.message); }
    finally { setSubmitting(false); }
  };

  return (
    <Modal title={`Make practicing doctor — ${employee.firstName} ${employee.lastName}`} onClose={onClose}>
      <div className="space-y-3">
        {error && <div className="p-2 bg-red-50 text-red-700 rounded text-sm">{error}</div>}
        <p className="text-xs text-slate-500">
          Creates a doctor profile for this employee and grants the Doctor role
          (active after their next login).
        </p>
        <Field label="Medical license number" value={license} onChange={setLicense} placeholder="BMA-12345" />
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Specialization</label>
          <select value={specialization} onChange={e => setSpecialization(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            {SPECIALIZATIONS.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Department</label>
          <select value={departmentId} onChange={e => setDepartmentId(e.target.value)} className="w-full px-3 py-2 border border-slate-300 rounded text-sm">
            <option value="">— None —</option>
            {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Consultation duration (min)" value={duration} onChange={setDuration} type="number" />
          <Field label="Consultation fee (BDT)" value={fee} onChange={setFee} type="number" placeholder="e.g. 1500" />
        </div>
        <ModalActions onClose={onClose} onSubmit={submit} submitting={submitting} submitText="Create doctor profile" />
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
