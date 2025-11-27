"use client";
import { useEffect, useState } from "react";
import { apiClient } from "../../lib/apiClient";
import { useAuthStore } from "../../store/useAuthStore";
import RequestDetail from "./RequestDetail";

interface SalesRequest {
  id: number;
  hospitalName: string;
  contactPerson: string;
  email: string;
  region: string | null;
  status: number;
  ownerSalesUserId: number | null;
  createdAt: string;
  quotedPrice: number | null;
  currency: string | null;
  paymentStatus: number;
}

interface PipelineTableProps {
  onRowClick?: (id: number) => void;
}

const statusLabels: Record<number, { label: string; color: string }> = {
  0: { label: "Pending", color: "bg-amber-500/20 text-amber-300" },
  1: { label: "In Progress", color: "bg-blue-500/20 text-blue-300" },
  2: { label: "Active", color: "bg-emerald-500/20 text-emerald-300" },
  3: { label: "Deactivated", color: "bg-slate-500/20 text-slate-300" },
};

export default function PipelineTable({ onRowClick }: PipelineTableProps) {
  const [requests, setRequests] = useState<SalesRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const { accessToken } = useAuthStore();

  // Filters
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [regionFilter, setRegionFilter] = useState("");

  // Pagination
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [total, setTotal] = useState(0);

  // Detail modal
  const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null);

  const totalPages = Math.ceil(total / pageSize);

  useEffect(() => {
    loadRequests();
  }, [accessToken, page, search, statusFilter, regionFilter]);

  const loadRequests = async () => {
    if (!accessToken) return;
    
    setLoading(true);
    setError("");

    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });

      if (search) params.append("q", search);
      if (statusFilter !== "") params.append("status", statusFilter.toString());
      if (regionFilter) params.append("region", regionFilter);

      const res = await apiClient.get(`/api/sales/requests?${params}`);
      
      if (!res.ok) {
        throw new Error("Failed to load requests");
      }

      const data = await res.json();
      setRequests(data.items || []);
      setTotal(data.total || 0);
    } catch (e: any) {
      setError(e.message || "Failed to load pipeline");
      setRequests([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (value: string) => {
    setSearch(value);
    setPage(1); // Reset to first page on search
  };

  const handleStatusFilter = (value: string) => {
    setStatusFilter(value === "" ? "" : parseInt(value));
    setPage(1);
  };

  const handleRegionFilter = (value: string) => {
    setRegionFilter(value);
    setPage(1);
  };

  const handleReset = () => {
    setSearch("");
    setStatusFilter("");
    setRegionFilter("");
    setPage(1);
  };

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Search hospital or email..."
          value={search}
          onChange={(e) => handleSearch(e.target.value)}
          className="flex-1 min-w-[200px] rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-sm text-white placeholder-gray-400 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
        />

        <select
          value={statusFilter}
          onChange={(e) => handleStatusFilter(e.target.value)}
          className="rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-sm text-white focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
        >
          <option value="">All Statuses</option>
          <option value="0">Pending</option>
          <option value="1">In Progress</option>
          <option value="2">Active</option>
          <option value="3">Deactivated</option>
        </select>

        <input
          type="text"
          placeholder="Region"
          value={regionFilter}
          onChange={(e) => handleRegionFilter(e.target.value)}
          className="w-40 rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-sm text-white placeholder-gray-400 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
        />

        <button
          onClick={handleReset}
          className="px-4 py-2 text-sm text-gray-400 hover:text-white transition-colors"
        >
          Reset
        </button>
      </div>

      {error && (
        <div className="rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200">
          {error}
        </div>
      )}

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border border-white/10">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-white/5">
            <tr>
              <th className="px-4 py-3 font-medium text-gray-300">Hospital</th>
              <th className="px-4 py-3 font-medium text-gray-300">Contact</th>
              <th className="px-4 py-3 font-medium text-gray-300">Region</th>
              <th className="px-4 py-3 font-medium text-gray-300">Status</th>
              <th className="px-4 py-3 font-medium text-gray-300 text-right">Quoted Price</th>
              <th className="px-4 py-3 font-medium text-gray-300">Created</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {loading ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                  <div className="flex items-center justify-center gap-2">
                    <div className="h-4 w-4 animate-spin rounded-full border-2 border-purple-500 border-t-transparent"></div>
                    Loading...
                  </div>
                </td>
              </tr>
            ) : requests.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-12 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <svg className="h-12 w-12 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    <p className="text-gray-400">No requests found</p>
                    {(search || statusFilter !== "" || regionFilter) && (
                      <button
                        onClick={handleReset}
                        className="mt-2 text-sm text-purple-400 hover:text-purple-300"
                      >
                        Clear filters
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ) : (
              requests.map((request) => (
                <tr
                  key={request.id}
                  onClick={() => setSelectedRequestId(request.id)}
                  className="cursor-pointer hover:bg-white/5 transition-colors"
                >
                  <td className="px-4 py-3 font-medium text-white">
                    {request.hospitalName}
                  </td>
                  <td className="px-4 py-3 text-gray-300">
                    <div className="flex flex-col">
                      <span className="text-sm">{request.contactPerson}</span>
                      <span className="text-xs text-gray-500">{request.email}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-300">
                    {request.region || "-"}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${statusLabels[request.status]?.color || "bg-gray-500/20 text-gray-300"}`}>
                      {statusLabels[request.status]?.label || "Unknown"}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right text-gray-300">
                    {request.quotedPrice !== null ? (
                      <span className="font-medium">
                        {request.currency || ""} {request.quotedPrice.toLocaleString()}
                      </span>
                    ) : (
                      <span className="text-gray-500">-</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-gray-400 text-sm">
                    {new Date(request.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {!loading && totalPages > 1 && (
        <div className="flex items-center justify-between pt-2">
          <div className="text-sm text-gray-400">
            Showing {requests.length > 0 ? (page - 1) * pageSize + 1 : 0} to{" "}
            {Math.min(page * pageSize, total)} of {total} requests
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage(Math.max(1, page - 1))}
              disabled={page === 1}
              className="rounded-lg bg-white/5 px-3 py-1.5 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white/10 transition-colors"
            >
              Previous
            </button>

            <div className="flex items-center gap-1">
              {Array.from({ length: totalPages }, (_, i) => i + 1)
                .filter(p => {
                  // Show first, last, current, and adjacent pages
                  return p === 1 || p === totalPages || Math.abs(p - page) <= 1;
                })
                .map((p, idx, arr) => {
                  // Add ellipsis if there's a gap
                  const prevPage = arr[idx - 1];
                  const showEllipsis = prevPage && p - prevPage > 1;
                  
                  return (
                    <div key={p} className="flex items-center gap-1">
                      {showEllipsis && <span className="text-gray-500 px-1">...</span>}
                      <button
                        onClick={() => setPage(p)}
                        className={`min-w-[32px] rounded-lg px-2 py-1.5 text-sm transition-colors ${
                          page === p
                            ? "bg-purple-500 text-white"
                            : "bg-white/5 text-gray-300 hover:bg-white/10"
                        }`}
                      >
                        {p}
                      </button>
                    </div>
                  );
                })}
            </div>

            <button
              onClick={() => setPage(Math.min(totalPages, page + 1))}
              disabled={page === totalPages}
              className="rounded-lg bg-white/5 px-3 py-1.5 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white/10 transition-colors"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {/* Detail Modal */}
      {selectedRequestId && (
        <RequestDetail
          requestId={selectedRequestId}
          onClose={() => setSelectedRequestId(null)}
        />
      )}
    </div>
  );
}
