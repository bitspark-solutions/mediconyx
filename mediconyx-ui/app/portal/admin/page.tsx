"use client";

import { useState, useEffect } from "react";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";
import AdminReviewQueue from "../../../components/admin/AdminReviewQueue";
import { apiClient } from "../../../lib/apiClient";
import { useAuthStore } from "../../../store/useAuthStore";
import { ListChecks, Users, Settings, Loader2, Save, Upload, Hospital, Stethoscope, Building2, Bed } from "lucide-react";

const MODULE_LABELS: Record<string, string> = {
  moduleOpdEnabled: "OPD Management",
  moduleIpdEnabled: "IPD Management",
  moduleEmergencyEnabled: "Emergency Services",
  modulePharmacyEnabled: "Pharmacy",
  moduleLaboratoryEnabled: "Laboratory",
  moduleRadiologyEnabled: "Radiology / Imaging",
  moduleBillingEnabled: "Billing / Invoicing",
  moduleInventoryEnabled: "Inventory",
  moduleHrPayrollEnabled: "HR / Payroll",
  moduleAppointmentEnabled: "Appointment Scheduling",
  moduleTelemedicineEnabled: "Telemedicine",
  moduleAnalyticsEnabled: "Analytics & Reporting",
};

interface TenantSettings {
  id: number;
  name: string;
  address?: string;
  contactEmail?: string;
  contactPhone?: string;
  logoUrl?: string;
  description?: string;
  operatingHours?: string;
  timeZone?: string;
  businessRegistrationNumber?: string;
  subscriptionPlan?: string;
  userLicenses?: number;
  [key: string]: any;
}

export default function AdminPortal() {
  const [activeTab, setActiveTab] = useState("review");
  const { user } = useAuthStore();
  const isMaster = user?.role?.toLowerCase() === "master";

  return (
    <ProtectedRoute allowedRoles={["Admin", "Master"]}>
      <main className="min-h-screen bg-black text-white">
        <Header />
        <div className="max-w-7xl mx-auto p-6">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-pink-400">
              Admin Portal
            </h1>
          </div>

          {/* KPI Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">Pending Reviews</h3>
              <div className="text-3xl font-bold text-white">--</div>
            </div>
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">Active Tenants</h3>
              <div className="text-3xl font-bold text-white">--</div>
            </div>
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">System Health</h3>
              <div className="text-3xl font-bold text-green-400">Good</div>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex items-center gap-1 border-b border-white/10 mb-6">
            <button
              onClick={() => setActiveTab("review")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "review"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <ListChecks className="w-4 h-4" /> Review Queue
            </button>
            <button
              onClick={() => setActiveTab("tenants")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "tenants"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <Building2 className="w-4 h-4" /> Tenants
            </button>
            <a
              href="/portal/admin/employees"
              className="px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 border-transparent text-gray-400 hover:text-white transition-colors"
            >
              <Users className="w-4 h-4" /> Employees
            </a>
            <a
              href="/portal/admin/bed-board"
              className="px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 border-transparent text-gray-400 hover:text-white transition-colors"
            >
              <Bed className="w-4 h-4" /> Bed Board (IPD)
            </a>
            <button
              onClick={() => setActiveTab("settings")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "settings"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <Settings className="w-4 h-4" /> Hospital Settings
            </button>
            {isMaster && (
              <button
                onClick={() => setActiveTab("users")}
                className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "users"
                    ? "border-purple-500 text-purple-400"
                    : "border-transparent text-gray-400 hover:text-white"
                  }`}
              >
                <Users className="w-4 h-4" /> Users
              </button>
            )}
          </div>

          {/* Content */}
          <div className="min-h-[400px]">
            {activeTab === "review" && <AdminReviewQueue />}

            {activeTab === "tenants" && <TenantsTab />}

            {activeTab === "settings" && <HospitalSettingsTab />}

            {activeTab === "users" && isMaster && <UsersTab />}
          </div>
        </div>
      </main>
    </ProtectedRoute>
  );
}

function TenantsTab() {
  const [tenants, setTenants] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/sales/requests?pageSize=50");
        if (res.ok) {
          const data = await res.json();
          setTenants(data.items || data.requests || []);
        }
      } catch (err) {
        console.error("Failed to load tenants:", err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="w-6 h-6 text-purple-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="bg-white/5 border border-white/10 rounded-xl overflow-hidden">
      <div className="p-4 border-b border-white/10">
        <h2 className="text-lg font-semibold text-white">Onboarded Tenants</h2>
        <p className="text-sm text-gray-400">{tenants.length} tenants in the system</p>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-white/5 text-gray-400 text-xs uppercase">
            <tr>
              <th className="px-4 py-3 text-left">Tenant</th>
              <th className="px-4 py-3 text-left">Plan</th>
              <th className="px-4 py-3 text-left">Status</th>
              <th className="px-4 py-3 text-left">Payment</th>
              <th className="px-4 py-3 text-left">Created</th>
              <th className="px-4 py-3 text-left">Region</th>
            </tr>
          </thead>
          <tbody>
            {tenants.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-12 text-gray-500">
                  <Building2 className="w-10 h-10 mx-auto mb-2 opacity-30" />
                  No tenants yet
                </td>
              </tr>
            ) : (
              tenants.map((t) => (
                <tr key={t.id} className="border-t border-white/5 hover:bg-white/5">
                  <td className="px-4 py-3">
                    <p className="text-white font-medium">{t.hospitalName}</p>
                    <p className="text-xs text-gray-500">{t.email}</p>
                  </td>
                  <td className="px-4 py-3 text-gray-300">{t.subscriptionPlan || "—"}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                      t.status === "Active" ? "bg-emerald-500/20 text-emerald-300" :
                      t.status === "Suspended" ? "bg-red-500/20 text-red-300" :
                      "bg-amber-500/20 text-amber-300"
                    }`}>
                      {t.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-300">{t.paymentStatus || "—"}</td>
                  <td className="px-4 py-3 text-gray-400 text-xs">{new Date(t.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-400 text-xs">{t.region || "—"}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function HospitalSettingsTab() {
  const [settings, setSettings] = useState<TenantSettings | null>(null);
  const [edited, setEdited] = useState<Partial<TenantSettings>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [uploadingLogo, setUploadingLogo] = useState(false);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/TenantSettings");
        if (res.ok) {
          const data = await res.json();
          setSettings(data);
          setEdited(data);
        }
      } catch (err) {
        console.error("Failed to load settings:", err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    if (!settings) return;
    setSaving(true);
    setMessage("");
    try {
      const res = await apiClient.put("/api/TenantSettings", edited);
      if (res.ok) {
        const updated = await res.json();
        setSettings(updated);
        setEdited(updated);
        setMessage("Settings saved successfully");
        setTimeout(() => setMessage(""), 3000);
      } else {
        setMessage("Failed to save settings");
      }
    } catch (err) {
      console.error(err);
      setMessage("Failed to save settings");
    } finally {
      setSaving(false);
    }
  };

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploadingLogo(true);
    setMessage("");
    try {
      const form = new FormData();
      form.append("logo", file);
      const { accessToken } = (await import("../../../store/useAuthStore")).useAuthStore.getState();
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/TenantSettings/logo`, {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
        body: form,
      });
      if (res.ok) {
        const data = await res.json();
        if (settings) {
          setSettings({ ...settings, logoUrl: data.logoUrl });
        }
        setEdited({ ...edited, logoUrl: data.logoUrl });
        setMessage("Logo uploaded successfully");
        setTimeout(() => setMessage(""), 3000);
      } else {
        setMessage("Failed to upload logo");
      }
    } catch (err) {
      console.error(err);
      setMessage("Failed to upload logo");
    } finally {
      setUploadingLogo(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="w-6 h-6 text-purple-400 animate-spin" />
      </div>
    );
  }

  if (!settings) {
    return (
      <div className="bg-white/5 border border-white/10 rounded-xl p-8 text-center">
        <Settings className="w-12 h-12 text-gray-500 mx-auto mb-3" />
        <p className="text-gray-400">No settings available for this tenant</p>
      </div>
    );
  }

  const textFields: Array<{ key: keyof TenantSettings; label: string; type?: string }> = [
    { key: "name", label: "Hospital Name" },
    { key: "contactEmail", label: "Contact Email", type: "email" },
    { key: "contactPhone", label: "Contact Phone" },
    { key: "address", label: "Address" },
    { key: "description", label: "Description" },
    { key: "operatingHours", label: "Operating Hours (e.g. Mon-Fri 9am-5pm)" },
    { key: "timeZone", label: "Time Zone" },
    { key: "businessRegistrationNumber", label: "Business Registration Number" },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-white/5 border border-white/10 rounded-xl p-6">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3">
            <Hospital className="w-6 h-6 text-purple-400" />
            <h2 className="text-lg font-semibold text-white">Hospital Settings</h2>
          </div>
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2 bg-purple-600 text-white rounded text-sm font-medium hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
          >
            {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
            Save Changes
          </button>
        </div>

        {message && (
          <div className={`mb-4 p-3 rounded text-sm ${message.includes("success") ? "bg-emerald-500/20 text-emerald-300" : "bg-red-500/20 text-red-300"}`}>
            {message}
          </div>
        )}

        {/* Logo Upload */}
        <div className="mb-6 pb-6 border-b border-white/10 flex items-center gap-6">
          <div className="w-24 h-24 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center overflow-hidden">
            {settings.logoUrl ? (
              <img src={settings.logoUrl} alt="Logo" className="w-full h-full object-contain" />
            ) : (
              <Hospital className="w-10 h-10 text-gray-600" />
            )}
          </div>
          <div>
            <h3 className="text-sm font-medium text-white mb-1">Hospital Logo</h3>
            <p className="text-xs text-gray-400 mb-2">PNG, JPG up to 5MB. Recommended 256x256.</p>
            <label className="inline-flex items-center gap-2 px-3 py-1.5 bg-white/5 border border-white/10 text-white text-sm rounded cursor-pointer hover:bg-white/10">
              <Upload className="w-4 h-4" />
              {uploadingLogo ? "Uploading..." : "Upload Logo"}
              <input type="file" accept="image/*" onChange={handleLogoUpload} disabled={uploadingLogo} className="hidden" />
            </label>
          </div>
        </div>

        {/* Text fields */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {textFields.map(({ key, label, type }) => {
            const value = edited[key] as string | undefined;
            return (
              <div key={key}>
                <label htmlFor={`settings-${key}`} className="block text-xs font-medium text-gray-400 mb-1">{label}</label>
                <input
                  id={`settings-${key}`}
                  type={type || "text"}
                  value={value || ""}
                  onChange={(e) => setEdited({ ...edited, [key]: e.target.value })}
                  className="w-full px-3 py-2 bg-white/5 border border-white/10 text-white rounded text-sm focus:outline-none focus:border-purple-500"
                />
              </div>
            );
          })}
        </div>
      </div>

      {/* Modules */}
      <div className="bg-white/5 border border-white/10 rounded-xl p-6">
        <div className="flex items-center gap-3 mb-4">
          <Stethoscope className="w-5 h-5 text-purple-400" />
          <h2 className="text-lg font-semibold text-white">Active Modules</h2>
        </div>
        <p className="text-sm text-gray-400 mb-4">
          Modules are configured during onboarding. Contact sales to enable additional modules.
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {Object.entries(MODULE_LABELS).map(([key, label]) => {
            const enabled = settings[key] === true;
            return (
              <div
                key={key}
                className={`p-3 rounded border ${
                  enabled
                    ? "bg-emerald-500/10 border-emerald-500/30"
                    : "bg-white/5 border-white/10 opacity-60"
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className="text-sm text-white">{label}</span>
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                    enabled ? "bg-emerald-500/20 text-emerald-300" : "bg-slate-700 text-slate-400"
                  }`}>
                    {enabled ? "Enabled" : "Disabled"}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Subscription Info */}
      {(settings.subscriptionPlan || settings.userLicenses) && (
        <div className="bg-white/5 border border-white/10 rounded-xl p-6">
          <h2 className="text-lg font-semibold text-white mb-4">Subscription</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1">Plan</label>
              <p className="text-white">{settings.subscriptionPlan || "—"}</p>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1">User Licenses</label>
              <p className="text-white">{settings.userLicenses || "—"}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function UsersTab() {
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await apiClient.get("/api/UserManagement?pageSize=100");
        if (res.ok) {
          const data = await res.json();
          setUsers(data.users || data.items || []);
        }
      } catch (err) {
        console.error("Failed to load users:", err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = users.filter(u =>
    `${u.email} ${u.firstName} ${u.lastName} ${u.role}`.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="w-6 h-6 text-purple-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="bg-white/5 border border-white/10 rounded-xl overflow-hidden">
      <div className="p-4 border-b border-white/10 flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-white">All Users (Master View)</h2>
          <p className="text-sm text-gray-400">{filtered.length} of {users.length} users</p>
        </div>
        <input
          type="text"
          placeholder="Search users..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="px-3 py-1.5 bg-white/5 border border-white/10 text-white text-sm rounded focus:outline-none focus:border-purple-500"
        />
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-white/5 text-gray-400 text-xs uppercase">
            <tr>
              <th className="px-4 py-3 text-left">User</th>
              <th className="px-4 py-3 text-left">Email</th>
              <th className="px-4 py-3 text-left">Role</th>
              <th className="px-4 py-3 text-left">Tenant</th>
              <th className="px-4 py-3 text-left">Status</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-center py-12 text-gray-500">No users found</td>
              </tr>
            ) : (
              filtered.map((u) => (
                <tr key={u.id} className="border-t border-white/5 hover:bg-white/5">
                  <td className="px-4 py-3 text-white">{u.firstName} {u.lastName}</td>
                  <td className="px-4 py-3 text-gray-300">{u.email}</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-1 rounded-full text-xs font-medium bg-purple-500/20 text-purple-300">
                      {u.role}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-400 text-xs">#{u.tenantId}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                      u.isActive ? "bg-emerald-500/20 text-emerald-300" : "bg-slate-700 text-slate-400"
                    }`}>
                      {u.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
