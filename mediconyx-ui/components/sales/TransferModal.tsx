"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";

interface SalesUser {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
}

interface TransferModalProps {
    requestId: number;
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
}

export default function TransferModal({ requestId, isOpen, onClose, onSuccess }: TransferModalProps) {
    const [users, setUsers] = useState<SalesUser[]>([]);
    const [selectedUser, setSelectedUser] = useState<string>("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        if (isOpen) {
            loadUsers();
        }
    }, [isOpen]);

    const loadUsers = async () => {
        try {
            setLoading(true);
            const res = await apiClient.get("/api/sales/requests/users");
            if (res.ok) {
                setUsers(await res.json());
            } else {
                setError("Failed to load sales users.");
            }
        } catch (e) {
            setError("Error loading users.");
        } finally {
            setLoading(false);
        }
    };

    const handleTransfer = async () => {
        if (!selectedUser) return;
        try {
            setLoading(true);
            const res = await apiClient.post(`/api/sales/requests/${requestId}/transfer`, {
                targetUserId: parseInt(selectedUser),
            });

            if (res.ok) {
                onSuccess();
                onClose();
            } else {
                const data = await res.json();
                setError(data.message || "Transfer failed.");
            }
        } catch (e) {
            setError("Transfer request failed.");
        } finally {
            setLoading(false);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
            <div className="w-full max-w-md rounded-xl surface p-6 shadow-xl border border-white/10">
                <h2 className="text-xl font-semibold text-white mb-4">Transfer Request</h2>

                {error && (
                    <div className="mb-4 rounded bg-red-500/20 px-3 py-2 text-sm text-red-200">
                        {error}
                    </div>
                )}

                <div className="mb-6">
                    <label className="block text-sm text-gray-400 mb-2">Select Sales Agent</label>
                    <select
                        value={selectedUser}
                        onChange={(e) => setSelectedUser(e.target.value)}
                        className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white outline-none focus:border-purple-500"
                        disabled={loading}
                    >
                        <option value="">-- Select Agent --</option>
                        {users.map((u) => (
                            <option key={u.id} value={u.id}>
                                {u.firstName} {u.lastName} ({u.email})
                            </option>
                        ))}
                    </select>
                </div>

                <div className="flex justify-end gap-3">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 font-medium text-gray-400 hover:text-white transition-colors"
                        disabled={loading}
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleTransfer}
                        className="rounded-lg bg-purple-600 px-4 py-2 font-medium text-white hover:bg-purple-500 transition-colors disabled:opacity-50"
                        disabled={!selectedUser || loading}
                    >
                        {loading ? "Transferring..." : "Transfer Ownership"}
                    </button>
                </div>
            </div>
        </div>
    );
}
