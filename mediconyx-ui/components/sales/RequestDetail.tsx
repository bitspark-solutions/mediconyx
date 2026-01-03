"use client";
import { useEffect, useState } from "react";
import { apiClient } from "../../lib/apiClient";
import HospitalProfileForm from "./HospitalProfileForm";
import TechnicalRequirementsForm from "./TechnicalRequirementsForm";
import ModuleSelectionForm from "./ModuleSelectionForm";
import PricingForm from "./PricingForm";
import StatusBadge from "./StatusBadge";

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
  documentType?: number;
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
  ownerSalesUserId: number | null;
  createdAt: string;
  timeline: TimelineEvent[];
  notesList: Note[];
  attachments: Attachment[];
}

const statusLabels: Record<number, string> = {
  0: "Pending",
  1: "Qualified",
  2: "InProgress",
  3: "AwaitingPayment",
  4: "Provisioning",
  5: "Active",
  6: "Rejected",
  7: "Suspended",
  8: "Deactivated",
};

const paymentStatusLabels: Record<number, string> = {
  0: "Pending",
  1: "Paid",
  2: "Failed",
  3: "Refunded",
};

const tabs = [
  { id: "overview", label: "Overview" },
  { id: "profile", label: "Hospital Profile" },
  { id: "technical", label: "Technical" },
  { id: "modules", label: "Modules" },
  { id: "documents", label: "Documents" },
  { id: "pricing", label: "Pricing" },
  { id: "timeline", label: "Timeline" },
];

export default function RequestDetail({ requestId, onClose }: RequestDetailProps) {
  const [request, setRequest] = useState<RequestData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeTab, setActiveTab] = useState("overview");
  const [noteText, setNoteText] = useState("");
  const [addingNote, setAddingNote] = useState(false);
  const [fileInput, setFileInput] = useState<HTMLInputElement | null>(null);
  const [uploadingFile, setUploadingFile] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    loadRequest();
  }, [requestId]);

  const loadRequest = async () => {
    setLoading(true);
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
        await loadRequest();
      } else {
        setError("Failed to add note");
      }
    } catch (error) {
      setError("Error adding note");
    } finally {
      setAddingNote(false);
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
          headers: { Authorization: `Bearer ${localStorage.getItem("accessToken") || ""}` },
        }
      );
      if (res.ok) {
        await loadRequest();
      } else {
        const errorData = await res.json();
        setError(errorData.message || "Failed to upload file");
      }
    } catch (error) {
      setError("Error uploading file");
    } finally {
      setUploadingFile(false);
    }
  };

  const handleClaim = async () => {
    const res = await apiClient.post(`/api/sales/requests/${requestId}/claim`, "");
    if (res.ok) await loadRequest();
  };

  const handleQualify = async () => {
    setActionLoading(true);
    const res = await apiClient.patch(`/api/sales/requests/${requestId}/qualify`, {});
    if (res.ok) await loadRequest();
    else setError("Failed to qualify request");
    setActionLoading(false);
  };

  const handleApprove = async () => {
    setActionLoading(true);
    const res = await apiClient.post(`/api/sales/requests/${requestId}/approve-profile`, {});
    if (res.ok) await loadRequest();
    else {
      const data = await res.json();
      setError(data.message || "Failed to approve");
    }
    setActionLoading(false);
  };

  const handleReject = async () => {
    if (!rejectReason.trim()) return;
    setActionLoading(true);
    const res = await apiClient.post(`/api/sales/requests/${requestId}/reject-profile`, { reason: rejectReason });
    if (res.ok) {
      setShowRejectModal(false);
      setRejectReason("");
      await loadRequest();
    } else {
      setError("Failed to reject");
    }
    setActionLoading(false);
  };

  const handleConfirmPayment = async () => {
    setActionLoading(true);
    const res = await apiClient.patch(`/api/sales/requests/${requestId}/payment-status`, { paymentStatus: 1 });
    if (res.ok) await loadRequest();
    else setError("Failed to confirm payment");
    setActionLoading(false);
  };

  const handleProvision = async () => {
    setActionLoading(true);
    const res = await apiClient.post(`/api/sales/provision/${requestId}`, {});
    if (res.ok) await loadRequest();
    else {
      const data = await res.json();
      setError(data.message || "Failed to provision tenant");
    }
    setActionLoading(false);
  };

  if (loading) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-gray-900 rounded-xl p-8 max-w-6xl w-full mx-4">
          <div className="flex items-center justify-center py-12">
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
      <div className="bg-gray-900 rounded-xl max-w-6xl w-full mx-4 my-8 max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-white/10">
          <div className="flex items-center gap-4">
            <button onClick={onClose} className="text-gray-400 hover:text-white">
              <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
            <div>
              <h2 className="text-xl font-semibold text-white">{request.hospitalName}</h2>
              <p className="text-sm text-gray-400">{request.contactPerson} • {request.email}</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <StatusBadge status={statusLabels[request.status] || "Unknown"} />
            {/* Workflow Action Buttons */}
            {request.status === 0 && (
              <button onClick={handleQualify} disabled={actionLoading} className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">
                Qualify
              </button>
            )}
            {request.status === 2 && ( // InProgress
              <>
                <button onClick={handleApprove} disabled={actionLoading} className="px-3 py-1.5 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50">
                  Approve
                </button>
                <button onClick={() => setShowRejectModal(true)} disabled={actionLoading} className="px-3 py-1.5 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
                  Reject
                </button>
              </>
            )}
            {request.status === 3 && request.paymentStatus === 0 && ( // AwaitingPayment, not paid
              <button onClick={handleConfirmPayment} disabled={actionLoading} className="px-3 py-1.5 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50">
                Confirm Payment
              </button>
            )}
            {request.status === 3 && request.paymentStatus === 1 && ( // AwaitingPayment, paid
              <button onClick={handleProvision} disabled={actionLoading} className="px-3 py-1.5 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50">
                Provision Tenant
              </button>
            )}
            {!request.ownerSalesUserId && (
              <button onClick={handleClaim} className="px-3 py-1.5 text-sm bg-gray-600 text-white rounded-lg hover:bg-gray-700">
                Claim
              </button>
            )}
          </div>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-white/10 px-6">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${activeTab === tab.id
                ? "border-purple-500 text-purple-400"
                : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Error Banner */}
        {error && (
          <div className="mx-6 mt-4 rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200 flex items-center justify-between">
            <span>{error}</span>
            <button onClick={() => setError("")} className="text-red-200 hover:text-red-100">✕</button>
          </div>
        )}

        {/* Tab Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {activeTab === "overview" && (
            <div className="grid grid-cols-2 gap-6">
              <div className="surface rounded-xl p-6">
                <h3 className="text-lg font-semibold text-white mb-4">Contact Information</h3>
                <div className="space-y-3 text-sm">
                  <div><span className="text-gray-400">Contact:</span> <span className="text-white ml-2">{request.contactPerson}</span></div>
                  <div><span className="text-gray-400">Email:</span> <span className="text-white ml-2">{request.email}</span></div>
                  <div><span className="text-gray-400">Phone:</span> <span className="text-white ml-2">{request.phone || "-"}</span></div>
                  <div><span className="text-gray-400">Region:</span> <span className="text-white ml-2">{request.region || "-"}</span></div>
                  <div><span className="text-gray-400">Created:</span> <span className="text-white ml-2">{new Date(request.createdAt).toLocaleDateString()}</span></div>
                </div>
              </div>
              <div className="surface rounded-xl p-6">
                <h3 className="text-lg font-semibold text-white mb-4">Pricing & Payment</h3>
                <div className="space-y-3 text-sm">
                  <div><span className="text-gray-400">Plan:</span> <span className="text-white ml-2">{request.subscriptionPlan || "-"}</span></div>
                  <div><span className="text-gray-400">Cycle:</span> <span className="text-white ml-2">{request.billingCycle === 0 ? "Monthly" : request.billingCycle === 1 ? "Annual" : "-"}</span></div>
                  <div><span className="text-gray-400">Price:</span> <span className="text-white ml-2">{request.quotedPrice ? `${request.currency || ""} ${request.quotedPrice.toLocaleString()}` : "-"}</span></div>
                  <div><span className="text-gray-400">Seats:</span> <span className="text-white ml-2">{request.seats || "-"}</span></div>
                  <div><span className="text-gray-400">Payment:</span> <span className="text-white ml-2">{paymentStatusLabels[request.paymentStatus]}</span></div>
                </div>
              </div>
              {/* Notes Section */}
              <div className="col-span-2 surface rounded-xl p-6">
                <h3 className="text-lg font-semibold text-white mb-4">Notes</h3>
                <div className="space-y-3 mb-4">
                  {request.notesList.map((note) => (
                    <div key={note.id} className="bg-white/5 rounded-lg p-4">
                      <p className="text-white text-sm">{note.note}</p>
                      <p className="text-gray-400 text-xs mt-1">{new Date(note.createdAt).toLocaleString()}</p>
                    </div>
                  ))}
                  {request.notesList.length === 0 && <p className="text-gray-500 text-sm">No notes yet</p>}
                </div>
                <div className="flex gap-2">
                  <input
                    type="text"
                    value={noteText}
                    onChange={(e) => setNoteText(e.target.value)}
                    placeholder="Add a note..."
                    className="flex-1 rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-sm text-white placeholder-gray-400 focus:border-purple-500 focus:outline-none"
                  />
                  <button onClick={handleAddNote} disabled={addingNote || !noteText.trim()} className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700 disabled:opacity-50">
                    {addingNote ? "..." : "Add"}
                  </button>
                </div>
              </div>
            </div>
          )}

          {activeTab === "profile" && (
            <HospitalProfileForm requestId={requestId} onSaved={loadRequest} />
          )}

          {activeTab === "technical" && (
            <TechnicalRequirementsForm requestId={requestId} onSaved={loadRequest} />
          )}

          {activeTab === "modules" && (
            <ModuleSelectionForm requestId={requestId} onSaved={loadRequest} />
          )}

          {activeTab === "documents" && (
            <div className="space-y-4">
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
                <button onClick={() => fileInput?.click()} disabled={uploadingFile} className="px-4 py-2 bg-purple-600 text-white rounded-lg text-sm hover:bg-purple-700 disabled:opacity-50">
                  {uploadingFile ? "Uploading..." : "Upload Document"}
                </button>
                <span className="text-gray-400 text-sm self-center">Max 10MB (PDF, DOC, IMG)</span>
              </div>
              <div className="grid gap-3">
                {request.attachments.length === 0 ? (
                  <p className="text-gray-500 text-sm">No documents uploaded yet</p>
                ) : (
                  request.attachments.map((att) => (
                    <div key={att.id} className="flex items-center justify-between bg-white/5 rounded-lg p-4">
                      <div className="flex items-center gap-3">
                        <svg className="h-8 w-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                        </svg>
                        <div>
                          <p className="text-white text-sm font-medium">{att.fileName}</p>
                          <p className="text-gray-400 text-xs">{(att.sizeBytes / 1024).toFixed(0)} KB • {new Date(att.uploadedAt).toLocaleDateString()}</p>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {activeTab === "pricing" && (
            <PricingForm requestId={requestId} onSaved={loadRequest} />
          )}

          {activeTab === "timeline" && (
            <div className="space-y-4">
              {request.timeline.length === 0 ? (
                <p className="text-gray-500 text-sm">No timeline events yet</p>
              ) : (
                request.timeline.map((event) => (
                  <div key={event.id} className="flex gap-4">
                    <div className="flex-shrink-0 w-3 h-3 mt-1.5 rounded-full bg-purple-500"></div>
                    <div>
                      <p className="text-white text-sm">{event.description}</p>
                      <p className="text-gray-400 text-xs">{new Date(event.happenedAt).toLocaleString()}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>

      {/* Reject Modal */}
      {showRejectModal && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-[60]">
          <div className="bg-gray-900 rounded-xl p-6 max-w-md w-full mx-4 border border-white/10">
            <h3 className="text-lg font-semibold text-white mb-4">Reject Request</h3>
            <p className="text-gray-400 text-sm mb-4">
              Please provide a reason for rejection. This will be sent to the hospital contact.
            </p>
            <textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="Enter rejection reason..."
              rows={4}
              className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none mb-4"
            />
            <div className="flex gap-2 justify-end">
              <button
                onClick={() => {
                  setShowRejectModal(false);
                  setRejectReason("");
                }}
                disabled={actionLoading}
                className="px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                disabled={actionLoading || !rejectReason.trim()}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {actionLoading ? "Rejecting..." : "Reject"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
