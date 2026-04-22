"use client";

import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";
import ProvisionButton from "./ProvisionButton";
import { Building2, Calendar, Mail, CheckCircle2 } from "lucide-react";

interface PendingRequest {
    id: number;
    hospitalName: string;
    contactPerson: string;
    email: string;
    subscriptionPlan: string;
    quotedPrice: number;
    currency: string;
    paidAt: string;
}

const planNames = ["Starter", "Professional", "Enterprise"];

export default function AdminReviewQueue() {
    const [requests, setRequests] = useState<PendingRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [successData, setSuccessData] = useState<any>(null);

    useEffect(() => {
        loadRequests();
    }, []);

    const loadRequests = async () => {
        setLoading(true);
        try {
            const res = await apiClient.get("/api/sales/requests/pending-provision");
            if (res.ok) {
                const data = await res.json();
                setRequests(data);
            }
        } catch (err) {
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    const handleSuccess = (result: any) => {
        setSuccessData(result);
        loadRequests(); // Refresh list
    };

    return (
        <div className="space-y-6">
            <h2 className="text-xl font-semibold text-white flex items-center gap-2">
                <CheckCircle2 className="w-5 h-5 text-green-400" />
                Pending Review Queue
            </h2>

            {/* Success Banner */}
            {successData && (
                <div className="bg-green-500/10 border border-green-500/30 rounded-xl p-4 mb-6 flex items-start gap-4 animate-in fade-in slide-in-from-top-4">
                    <CheckCircle2 className="w-6 h-6 text-green-400 mt-1" />
                    <div>
                        <h3 className="text-lg font-bold text-green-100 mb-1">Tenant Provisioned Successfully!</h3>
                        <p className="text-green-200/80 mb-2">{successData.message}</p>
                        <div className="bg-black/30 rounded p-3 text-sm font-mono text-green-200">
                            <div>Subdomain: <strong>{successData.subdomain}</strong></div>
                            <div>Tenant ID: <strong>{successData.tenantId}</strong></div>
                            <div>Admin Email: <strong>{successData.adminEmail}</strong></div>
                        </div>
                        <button
                            onClick={() => setSuccessData(null)}
                            className="mt-3 text-sm text-green-400 hover:text-green-300 underline"
                        >
                            Dismiss
                        </button>
                    </div>
                </div>
            )}

            {loading ? (
                <div className="flex justify-center py-12">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
                </div>
            ) : requests.length === 0 ? (
                <div className="bg-white/5 border border-white/10 rounded-xl p-8 text-center">
                    <Building2 className="w-12 h-12 text-gray-600 mx-auto mb-3" />
                    <h3 className="text-lg font-medium text-white mb-1">No Pending Reviews</h3>
                    <p className="text-gray-400">There are no paid requests waiting for provisioning.</p>
                </div>
            ) : (
                <div className="bg-white/5 border border-white/10 rounded-xl overflow-hidden">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="border-b border-white/10 bg-white/5">
                                <th className="p-4 text-xs font-medium text-gray-400 uppercase">Hospital</th>
                                <th className="p-4 text-xs font-medium text-gray-400 uppercase">Contact</th>
                                <th className="p-4 text-xs font-medium text-gray-400 uppercase">Plan</th>
                                <th className="p-4 text-xs font-medium text-gray-400 uppercase">Paid At</th>
                                <th className="p-4 text-xs font-medium text-gray-400 uppercase text-right">Action</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-white/5">
                            {requests.map((r) => (
                                <tr key={r.id} className="hover:bg-white/5 transition-colors">
                                    <td className="p-4">
                                        <div className="font-medium text-white">{r.hospitalName}</div>
                                        <div className="text-xs text-gray-500">ID: {r.id}</div>
                                    </td>
                                    <td className="p-4">
                                        <div className="flex items-center gap-2 text-sm text-gray-300">
                                            <Mail className="w-3 h-3" /> {r.email}
                                        </div>
                                        <div className="text-xs text-gray-500 mt-0.5">{r.contactPerson}</div>
                                    </td>
                                    <td className="p-4">
                                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-500/20 text-purple-300 border border-purple-500/30">
                                            {r.subscriptionPlan}
                                        </span>
                                        <div className="text-xs text-gray-400 mt-1">
                                            {r.currency} {r.quotedPrice.toLocaleString()}
                                        </div>
                                    </td>
                                    <td className="p-4">
                                        <div className="flex items-center gap-2 text-sm text-gray-400">
                                            <Calendar className="w-3 h-3" />
                                            {new Date(r.paidAt).toLocaleDateString()}
                                        </div>
                                    </td>
                                    <td className="p-4 text-right">
                                        <ProvisionButton requestId={r.id} onSuccess={handleSuccess} />
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
