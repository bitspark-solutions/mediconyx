"use client";
import { useState } from "react";
import { apiClient } from "../../lib/apiClient";
import { AlertCircle, Loader2 } from "lucide-react";

interface ProvisionButtonProps {
    requestId: number;
    onSuccess: (result: any) => void;
}

export default function ProvisionButton({ requestId, onSuccess }: ProvisionButtonProps) {
    const [loading, setLoading] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [error, setError] = useState("");

    const handleProvision = async () => {
        setLoading(true);
        setError("");
        try {
            const res = await apiClient.post(`/api/sales/requests/${requestId}/provision`, {});
            if (res.ok) {
                const data = await res.json();
                setShowConfirm(false);
                onSuccess(data);
            } else {
                const data = await res.json();
                setError(data.message || "Provisioning failed");
            }
        } catch (err) {
            console.error(err);
            setError("Failed to connect to server");
        } finally {
            setLoading(false);
        }
    };

    if (showConfirm) {
        return (
            <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4">
                <div className="bg-gray-900 border border-purple-500/30 rounded-xl p-6 max-w-md w-full shadow-2xl">
                    <h3 className="text-xl font-bold text-white mb-2 flex items-center gap-2">
                        <AlertCircle className="w-6 h-6 text-purple-400" />
                        Confirm Provisioning
                    </h3>
                    <p className="text-gray-400 mb-6">
                        This will create a new dedicated tenant database, subdomain, and admin account for this hospital. This process may take 1-2 minutes.
                    </p>

                    {error && (
                        <div className="mb-4 bg-red-500/20 border border-red-500/30 rounded px-3 py-2 text-sm text-red-200">
                            {error}
                        </div>
                    )}

                    <div className="flex justify-end gap-3">
                        <button
                            onClick={() => setShowConfirm(false)}
                            className="px-4 py-2 text-gray-300 hover:text-white"
                            disabled={loading}
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleProvision}
                            disabled={loading}
                            className="px-4 py-2 bg-purple-600 rounded-lg text-white font-medium hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
                        >
                            {loading ? (
                                <>
                                    <Loader2 className="w-4 h-4 animate-spin" /> Provisioning...
                                </>
                            ) : (
                                "Start Provisioning"
                            )}
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <button
            onClick={() => setShowConfirm(true)}
            className="px-3 py-1.5 bg-green-600/20 text-green-400 hover:bg-green-600/30 border border-green-600/30 rounded-md text-sm font-medium transition-colors"
        >
            Provision
        </button>
    );
}
