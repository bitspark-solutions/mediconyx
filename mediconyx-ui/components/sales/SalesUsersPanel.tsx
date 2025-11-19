"use client";
import { useEffect, useState } from "react";
import { apiClient } from "../../lib/apiClient";
import { useAuthStore } from "../../store/useAuthStore";

interface SalesUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  lastLogin: string | null;
}

export default function SalesUsersPanel() {
  const [users, setUsers] = useState<SalesUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({
    email: "",
    firstName: "",
    lastName: "",
    password: "",
  });
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState("");
  const { user } = useAuthStore();

  useEffect(() => {
    // Only load if user is Master role
    if (user?.role?.toLowerCase() === 'master') {
      loadUsers();
    } else {
      setLoading(false);
      setError("Unauthorized: This feature is only available to Master users");
    }
  }, [user]);

  const loadUsers = async () => {
    try {
      const res = await apiClient.get("/api/admin/sales-users");
      if (res.ok) {
        const data = await res.json();
        setUsers(data);
      }
    } catch (err) {
      setError("Failed to load sales users");
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    if (user?.role?.toLowerCase() !== 'master') {
      setError("Unauthorized: Only Master users can create sales users");
      return;
    }

    if (!createForm.email || !createForm.firstName || !createForm.lastName || !createForm.password) {
      setError("All fields are required");
      return;
    }

    setCreating(true);
    setError("");

    try {
      const res = await apiClient.post("/api/admin/sales-users", {
        ...createForm,
        role: "Sales",
      });

      if (res.ok) {
        setShowCreateModal(false);
        setCreateForm({ email: "", firstName: "", lastName: "", password: "" });
        await loadUsers();
      } else {
        const data = await res.json();
        setError(data.message || "Failed to create user");
      }
    } catch (err) {
      setError("Failed to create user");
    } finally {
      setCreating(false);
    }
  };

  const handleToggleActive = async (userId: number, currentStatus: boolean) => {
    if (user?.role?.toLowerCase() !== 'master') {
      setError("Unauthorized: Only Master users can manage sales users");
      return;
    }

    try {
      const res = await apiClient.patch(`/api/admin/sales-users/${userId}`, {
        isActive: !currentStatus,
      });

      if (res.ok) {
        await loadUsers();
      }
    } catch (err) {
      setError("Failed to update user");
    }
  };

  const exportCSV = () => {
    const headers = ["Name", "Email", "Status", "Last Login"];
    const rows = users.map(u => [
      `${u.firstName} ${u.lastName}`,
      u.email,
      u.isActive ? "Active" : "Inactive",
      u.lastLogin ? new Date(u.lastLogin).toLocaleString() : "Never",
    ]);

    const csv = [headers, ...rows].map(row => row.join(",")).join("\n");
    const blob = new Blob([csv], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `sales-users-${new Date().toISOString().split("T")[0]}.csv`;
    a.click();
  };

  if (loading) {
    return (
      <div className="surface rounded-xl p-6">
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="surface rounded-xl p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-white">Sales Users</h2>
        <div className="flex gap-2">
          <button
            onClick={exportCSV}
            className="px-4 py-2 bg-gray-700 text-white rounded-lg text-sm hover:bg-gray-600"
          >
            Export CSV
          </button>
          <button
            onClick={() => setShowCreateModal(true)}
            className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700"
          >
            + Create User
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200">
          {error}
        </div>
      )}

      <div className="overflow-x-auto">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-white/5">
            <tr>
              <th className="px-4 py-3 font-medium text-gray-300">Name</th>
              <th className="px-4 py-3 font-medium text-gray-300">Email</th>
              <th className="px-4 py-3 font-medium text-gray-300">Status</th>
              <th className="px-4 py-3 font-medium text-gray-300">Last Login</th>
              <th className="px-4 py-3 font-medium text-gray-300">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {users.map((user) => (
              <tr key={user.id} className="hover:bg-white/5">
                <td className="px-4 py-3 text-white">
                  {user.firstName} {user.lastName}
                </td>
                <td className="px-4 py-3 text-gray-300">{user.email}</td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${
                      user.isActive
                        ? "bg-emerald-500/20 text-emerald-300"
                        : "bg-slate-500/20 text-slate-300"
                    }`}
                  >
                    {user.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-400 text-sm">
                  {user.lastLogin ? new Date(user.lastLogin).toLocaleString() : "Never"}
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => handleToggleActive(user.id, user.isActive)}
                    className={`px-3 py-1 rounded text-xs ${
                      user.isActive
                        ? "bg-red-600/20 text-red-300 hover:bg-red-600/30"
                        : "bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600/30"
                    }`}
                  >
                    {user.isActive ? "Deactivate" : "Activate"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create User Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-gray-900 rounded-xl p-6 max-w-md w-full mx-4 border border-white/10">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-white">Create Sales User</h3>
              <button
                onClick={() => setShowCreateModal(false)}
                className="text-gray-400 hover:text-white"
              >
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-400 mb-1">First Name</label>
                <input
                  type="text"
                  value={createForm.firstName}
                  onChange={(e) => setCreateForm({ ...createForm, firstName: e.target.value })}
                  className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white"
                />
              </div>

              <div>
                <label className="block text-sm text-gray-400 mb-1">Last Name</label>
                <input
                  type="text"
                  value={createForm.lastName}
                  onChange={(e) => setCreateForm({ ...createForm, lastName: e.target.value })}
                  className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white"
                />
              </div>

              <div>
                <label className="block text-sm text-gray-400 mb-1">Email</label>
                <input
                  type="email"
                  value={createForm.email}
                  onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                  className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white"
                />
              </div>

              <div>
                <label className="block text-sm text-gray-400 mb-1">Password</label>
                <input
                  type="password"
                  value={createForm.password}
                  onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                  className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white"
                />
                <p className="mt-1 text-xs text-gray-500">Min 8 characters, 1 uppercase, 1 number</p>
              </div>

              {error && (
                <div className="rounded-lg bg-red-500/20 border border-red-500/30 px-3 py-2 text-sm text-red-200">
                  {error}
                </div>
              )}

              <div className="flex gap-2">
                <button
                  onClick={() => setShowCreateModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600"
                >
                  Cancel
                </button>
                <button
                  onClick={handleCreate}
                  disabled={creating}
                  className="flex-1 px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
                >
                  {creating ? "Creating..." : "Create User"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
