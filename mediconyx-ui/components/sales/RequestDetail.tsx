"use client";
import { useEffect, useState } from "react";
import { apiClient } from "../../lib/apiClient";

interface RequestDetailProps {
  requestId: number;
  onClose: () => void;
}

interface TimelineEvent {
  id: number;
  type: string;
  description: string;
  happenedAt: string;
}

interface Note {
  id: number;
  note: string;
  authorUserId: number;
  createdAt: string;
}

interface Attachment {
  id: number;
  fileName: string;
  sizeBytes: number;
  uploadedAt: string;
}

interface RequestData {
  id: number;
  hospitalName: string;
  contactPerson: string;
  email: string;
  phone: string;
  region: string | null;
  status: number;
  subscriptionPlan: string | null;
  billingCycle: number | null;
  currency: string | null;
  quotedPrice: number | null;
  seats: number | null;
  paymentStatus: number;
  timeline: TimelineEvent[];
  notesList: Note[];
  attachments: Attachment[];
}

const statusLabels: Record<number, { label: string; color: string }> = {
  0: { label: "Pending", color: "bg-amber-500/20 text-amber-300" },
  1: { label: "In Progress", color: "bg-blue-500/20 text-blue-300" },
  2: { label: "Active", color: "bg-emerald-500/20 text-emerald-300" },
  3: { label: "Deactivated", color: "bg-slate-500/20 text-slate-300" },
};

const paymentStatusLabels: Record<number, string> = {
  0: "Pending",
  1: "Paid",
  2: "Failed",
  3: "Refunded",
};

export default function RequestDetail({ requestId, onClose }: RequestDetailProps) {
  const [request, setRequest] = useState<RequestData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [noteText, setNoteText] = useState("");
  const [addingNote, setAddingNote] = useState(false);
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState<number | null>(null);
  const [statusReason, setStatusReason] = useState("");
  const [updatingStatus, setUpdatingStatus] = useState(false);
  const [fileInput, setFileInput] = useState<HTMLInputElement | null>(null);
  const [uploadingFile, setUploadingFile] = useState(false);

  useEffect(() => {
    loadRequest();
  }, [requestId]);

  const loadRequest = async () => {
    try {
      const res = await apiClient.get(`/api/sales/requests/${requestId}`);
      if (res.ok) {
        const data = await res.json();
        setRequest(data);
      }
    } catch (error) {
      console.error("Failed to load request", error);
    } finally {
      setLoading(false);
    }
  };

  const handleAddNote = async () => {
    if (!noteText.trim()) return;
    
    setAddingNote(true);
    try {
      const res = await apiClient.post(`/api/sales/requests/${requestId}/notes`, noteText);
      if (res.ok) {
        setNoteText("");
        setError("");
        await loadRequest();
      } else {
        setError("Failed to add note");
      }
    } catch (error) {
      setError("Error adding note");
      console.error("Failed to add note", error);
    } finally {
      setAddingNote(false);
    }
  };

  const handleStatusChange = async () => {
    if (selectedStatus === null) return;
    
    setUpdatingStatus(true);
    try {
      const res = await apiClient.patch(`/api/sales/requests/${requestId}/status`, {
        status: selectedStatus,
        reason: statusReason
      });
      if (res.ok) {
        setShowStatusModal(false);
        setStatusReason("");
        setSelectedStatus(null);
        setError("");
        await loadRequest();
      } else {
        setError("Failed to update status");
      }
    } catch (error) {
      setError("Error updating status");
      console.error("Failed to update status", error);
    } finally {
      setUpdatingStatus(false);
    }
  };

  const handleFileUpload = async (file: File) => {
    setUploadingFile(true);
    try {
      const formData = new FormData();
      formData.append("file", file);

      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/sales/requests/${requestId}/attachments`,
        {
          method: "POST",
          body: formData,
          headers: {
            Authorization: `Bearer ${localStorage.getItem("accessToken") || ""}`
          }
        }
      );

      if (res.ok) {
        setError("");
        await loadRequest();
      } else {
        const errorData = await res.json();
        setError(errorData.message || "Failed to upload file");
      }
    } catch (error) {
      setError("Error uploading file");
      console.error("Failed to upload file", error);
    } finally {
      setUploadingFile(false);
    }
  };

  const handleClaim = async () => {
    try {
      const res = await apiClient.post(`/api/sales/requests/${requestId}/claim`, "");
      if (res.ok) {
        await loadRequest();
      }
    } catch (error) {
      console.error("Failed to claim request", error);
    }
  };

  const handleUnassign = async () => {
    try {
      const res = await apiClient.post(`/api/sales/requests/${requestId}/unassign`, "");
      if (res.ok) {
        await loadRequest();
      }
    } catch (error) {
      console.error("Failed to unassign request", error);
    }
  };

  if (loading) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-gray-900 rounded-xl p-8 max-w-4xl w-full mx-4">
          <div className="flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
          </div>
        </div>
      </div>
    );
  }

  if (!request) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-gray-900 rounded-xl p-8 max-w-4xl w-full mx-4">
          <p className="text-white">Request not found</p>
          <button onClick={onClose} className="mt-4 px-4 py-2 bg-purple-600 text-white rounded-lg">
            Close
          </button>
        </div>
      </div>
    );
  }

  return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 overflow-y-auto">
      <div className="bg-gray-900 rounded-xl p-8 max-w-5xl w-full mx-4 my-8 max-h-[90vh] overflow-y-auto">
        {/* Error Banner */}
        {error && (
          <div className="mb-6 rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200 flex items-center justify-between">
            <span>{error}</span>
            <button onClick={() => setError("")} className="text-red-200 hover:text-red-100">
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        )}

        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-4">
            <button onClick={onClose} className="text-gray-400 hover:text-white">
              <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
              </svg>
            </button>
            <h2 className="text-2xl font-semibold text-white">{request.hospitalName}</h2>
          </div>
          <div className="flex items-center gap-3">
            <span className={`inline-flex rounded-full px-3 py-1 text-sm font-medium ${statusLabels[request.status]?.color}`}>
              {statusLabels[request.status]?.label}
            </span>
            <button
              onClick={() => {
                setSelectedStatus(request.status);
                setShowStatusModal(true);
              }}
              className="px-3 py-1 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700"
            >
              Change Status
            </button>
          </div>
        </div>

        {/* Info Grid */}
        <div className="grid grid-cols-2 gap-6 mb-8">
          {/* Contact Information */}
          <div className="surface rounded-xl p-6">
            <h3 className="text-lg font-semibold text-white mb-4">Contact Information</h3>
            <div className="space-y-3 text-sm">
              <div>
                <span className="text-gray-400">Contact:</span>
                <span className="ml-2 text-white">{request.contactPerson}</span>
              </div>
              <div>
                <span className="text-gray-400">Email:</span>
                <span className="ml-2 text-white">{request.email}</span>
              </div>
              <div>
                <span className="text-gray-400">Phone:</span>
                <span className="ml-2 text-white">{request.phone || "-"}</span>
              </div>
              <div>
                <span className="text-gray-400">Region:</span>
                <span className="ml-2 text-white">{request.region || "-"}</span>
              </div>
            </div>
            <div className="mt-4 flex gap-2">
              <button onClick={handleClaim} className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700">
                Claim
              </button>
              <button onClick={handleUnassign} className="px-4 py-2 bg-gray-700 text-white rounded-lg text-sm hover:bg-gray-600">
                Unassign
              </button>
            </div>
          </div>

          {/* Pricing & Payment */}
          <div className="surface rounded-xl p-6">
            <h3 className="text-lg font-semibold text-white mb-4">Pricing & Payment</h3>
            <div className="space-y-3 text-sm">
              <div>
                <span className="text-gray-400">Plan:</span>
                <span className="ml-2 text-white">{request.subscriptionPlan || "-"}</span>
              </div>
              <div>
                <span className="text-gray-400">Cycle:</span>
                <span className="ml-2 text-white">
                  {request.billingCycle === 0 ? "Monthly" : request.billingCycle === 1 ? "Annual" : "-"}
                </span>
              </div>
              <div>
                <span className="text-gray-400">Price:</span>
                <span className="ml-2 text-white">
                  {request.quotedPrice ? `${request.currency || ""} ${request.quotedPrice.toLocaleString()}` : "-"}
                </span>
              </div>
              <div>
                <span className="text-gray-400">Seats:</span>
                <span className="ml-2 text-white">{request.seats || "-"}</span>
              </div>
              <div>
                <span className="text-gray-400">Payment:</span>
                <span className="ml-2 text-white">{paymentStatusLabels[request.paymentStatus]}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Timeline */}
        <div className="surface rounded-xl p-6 mb-6">
          <h3 className="text-lg font-semibold text-white mb-4">Timeline</h3>
          <div className="space-y-4">
            {request.timeline.map((event) => (
              <div key={event.id} className="flex gap-3">
                <div className="flex-shrink-0 w-2 h-2 mt-2 rounded-full bg-purple-500"></div>
                <div>
                  <p className="text-white text-sm">{event.description}</p>
                  <p className="text-gray-400 text-xs">{new Date(event.happenedAt).toLocaleString()}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Notes */}
        <div className="surface rounded-xl p-6 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-white">Notes</h3>
          </div>
          <div className="space-y-4 mb-4">
            {request.notesList.map((note) => (
              <div key={note.id} className="bg-white/5 rounded-lg p-4">
                <p className="text-white text-sm mb-1">{note.note}</p>
                <p className="text-gray-400 text-xs">
                  User {note.authorUserId} - {new Date(note.createdAt).toLocaleString()}
                </p>
              </div>
            ))}
          </div>
          <div className="space-y-2">
            <textarea
              value={noteText}
              onChange={(e) => setNoteText(e.target.value)}
              placeholder="Add a note..."
              className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-3 text-sm text-white placeholder-gray-400 focus:border-purple-500 focus:outline-none"
              rows={3}
            />
            <button
              onClick={handleAddNote}
              disabled={addingNote || !noteText.trim()}
              className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700 disabled:opacity-50"
            >
              {addingNote ? "Adding..." : "Add Note"}
            </button>
          </div>
        </div>

        {/* Attachments */}
        <div className="surface rounded-xl p-6 mb-6">
          <h3 className="text-lg font-semibold text-white mb-4">Attachments</h3>
          <div className="space-y-3">
            <div className="flex gap-2">
              <input
                type="file"
                ref={(el) => setFileInput(el)}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) {
                    handleFileUpload(file);
                    if (fileInput) fileInput.value = "";
                  }
                }}
                disabled={uploadingFile}
                className="hidden"
              />
              <button
                onClick={() => fileInput?.click()}
                disabled={uploadingFile}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700 disabled:opacity-50"
              >
                {uploadingFile ? "Uploading..." : "Upload File"}
              </button>
              <span className="text-gray-400 text-sm self-center">Max 10MB (PDF, DOC, IMG)</span>
            </div>
            
            {request.attachments.length === 0 ? (
              <p className="text-gray-400 text-sm">No attachments</p>
            ) : (
              <div className="space-y-2">
                {request.attachments.map((attachment) => (
                  <div key={attachment.id} className="flex items-center justify-between bg-white/5 rounded-lg p-3">
                    <div className="flex items-center gap-2">
                      <svg className="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                      </svg>
                      <span className="text-white text-sm">{attachment.fileName}</span>
                      <span className="text-gray-400 text-xs">({(attachment.sizeBytes / 1024).toFixed(0)} KB)</span>
                    </div>
                    <span className="text-gray-400 text-xs">{new Date(attachment.uploadedAt).toLocaleDateString()}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Status Change Modal */}
      {showStatusModal && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-[60]">
          <div className="bg-gray-900 rounded-xl p-6 max-w-md w-full mx-4 border border-white/10">
            <h3 className="text-lg font-semibold text-white mb-4">Change Status</h3>
            
            <div className="mb-4">
              <label className="text-sm text-gray-300 mb-2 block">New Status</label>
              <select
                value={selectedStatus ?? ""}
                onChange={(e) => setSelectedStatus(parseInt(e.target.value))}
                className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
              >
                <option value="0">Pending</option>
                <option value="1">In Progress</option>
                <option value="2">Active</option>
                <option value="3">Deactivated</option>
              </select>
            </div>

            <div className="mb-6">
              <label className="text-sm text-gray-300 mb-2 block">Reason (Optional)</label>
              <textarea
                value={statusReason}
                onChange={(e) => setStatusReason(e.target.value)}
                placeholder="Why are you changing the status?"
                className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-400 focus:border-purple-500 focus:outline-none"
                rows={3}
              />
            </div>

            <div className="flex gap-2 justify-end">
              <button
                onClick={() => {
                  setShowStatusModal(false);
                  setStatusReason("");
                }}
                disabled={updatingStatus}
                className="px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={handleStatusChange}
                disabled={updatingStatus || selectedStatus === null}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
              >
                {updatingStatus ? "Updating..." : "Update"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
