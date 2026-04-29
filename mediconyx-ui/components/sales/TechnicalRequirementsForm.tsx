"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";

interface TechnicalRequirementsFormProps {
    requestId: number;
    onSaved?: () => void;
}

interface TechnicalRequirements {
    expectedUserCount: number;
    concurrentUsers: number;
    departmentCount: number;
    departmentsList: string;
    existingEmrSystem?: string;
    integrationRequirements?: string;
    dataMigrationRequired: boolean;
    estimatedPatientRecords?: number;
    preferredGoLiveDate?: string;
    itContactName: string;
    itContactEmail: string;
    itContactPhone: string;
}

const emptyRequirements: TechnicalRequirements = {
    expectedUserCount: 50,
    concurrentUsers: 10,
    departmentCount: 5,
    departmentsList: "",
    existingEmrSystem: "",
    integrationRequirements: "",
    dataMigrationRequired: false,
    estimatedPatientRecords: undefined,
    preferredGoLiveDate: "",
    itContactName: "",
    itContactEmail: "",
    itContactPhone: "",
};

export default function TechnicalRequirementsForm({ requestId, onSaved }: TechnicalRequirementsFormProps) {
    const [requirements, setRequirements] = useState<TechnicalRequirements>(emptyRequirements);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        loadRequirements();
    }, [requestId]);

    const loadRequirements = async () => {
        setLoading(true);
        try {
            const res = await apiClient.get(`/api/sales/requests/${requestId}/technical-requirements`);
            if (res.ok) {
                const data = await res.json();
                setRequirements({
                    ...data,
                    preferredGoLiveDate: data.preferredGoLiveDate ? data.preferredGoLiveDate.split("T")[0] : "",
                });
            } else if (res.status === 404) {
                setRequirements(emptyRequirements);
            }
        } catch (err) {
            console.error("Failed to load technical requirements", err);
        } finally {
            setLoading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setError("");
        setSuccess(false);

        // Validation
        if (requirements.concurrentUsers > requirements.expectedUserCount) {
            setError("Concurrent users cannot exceed expected user count");
            setSaving(false);
            return;
        }

        if (requirements.dataMigrationRequired && !requirements.estimatedPatientRecords) {
            setError("Please provide estimated patient records for data migration");
            setSaving(false);
            return;
        }

        try {
            const res = await apiClient.post(`/api/sales/requests/${requestId}/technical-requirements`, {
                ...requirements,
                preferredGoLiveDate: requirements.preferredGoLiveDate || null,
            });
            if (res.ok) {
                setSuccess(true);
                onSaved?.();
                setTimeout(() => setSuccess(false), 3000);
            } else {
                const data = await res.json();
                setError(data.message || "Failed to save requirements");
            }
        } catch (err) {
            setError("Error saving requirements");
        } finally {
            setSaving(false);
        }
    };

    const updateField = (field: keyof TechnicalRequirements, value: any) => {
        setRequirements((prev) => ({ ...prev, [field]: value }));
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
                <div className="rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200">
                    {error}
                </div>
            )}
            {success && (
                <div className="rounded-lg bg-green-500/20 border border-green-500/30 px-4 py-3 text-sm text-green-200">
                    Requirements saved successfully!
                </div>
            )}

            {/* User & Infrastructure */}
            <div>
                <h4 className="text-white font-medium mb-4">User & Infrastructure</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Expected User Count *</label>
                        <input
                            type="number"
                            required
                            min={1}
                            max={10000}
                            value={requirements.expectedUserCount}
                            onChange={(e) => updateField("expectedUserCount", parseInt(e.target.value) || 1)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Concurrent Users *</label>
                        <input
                            type="number"
                            required
                            min={1}
                            max={1000}
                            value={requirements.concurrentUsers}
                            onChange={(e) => updateField("concurrentUsers", parseInt(e.target.value) || 1)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Department Count *</label>
                        <input
                            type="number"
                            required
                            min={1}
                            max={100}
                            value={requirements.departmentCount}
                            onChange={(e) => updateField("departmentCount", parseInt(e.target.value) || 1)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div className="col-span-3">
                        <label className="block text-sm text-gray-400 mb-1">Departments List * (comma-separated)</label>
                        <textarea
                            required
                            value={requirements.departmentsList}
                            onChange={(e) => updateField("departmentsList", e.target.value)}
                            placeholder="e.g., Cardiology, Neurology, Orthopedics, Emergency, ICU"
                            rows={2}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Existing Systems */}
            <div>
                <h4 className="text-white font-medium mb-4">Existing Systems</h4>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Existing EMR System</label>
                        <input
                            type="text"
                            value={requirements.existingEmrSystem || ""}
                            onChange={(e) => updateField("existingEmrSystem", e.target.value)}
                            placeholder="e.g., Epic, Cerner, None"
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Preferred Go-Live Date</label>
                        <input
                            type="date"
                            value={requirements.preferredGoLiveDate || ""}
                            onChange={(e) => updateField("preferredGoLiveDate", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div className="col-span-2">
                        <label className="block text-sm text-gray-400 mb-1">Integration Requirements</label>
                        <textarea
                            value={requirements.integrationRequirements || ""}
                            onChange={(e) => updateField("integrationRequirements", e.target.value)}
                            placeholder="Describe any integrations needed (lab systems, billing, insurance, etc.)"
                            rows={3}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Data Migration */}
            <div>
                <h4 className="text-white font-medium mb-4">Data Migration</h4>
                <div className="grid grid-cols-2 gap-4">
                    <div className="flex items-center gap-3">
                        <input
                            type="checkbox"
                            id="dataMigration"
                            checked={requirements.dataMigrationRequired}
                            onChange={(e) => updateField("dataMigrationRequired", e.target.checked)}
                            className="w-5 h-5 rounded border-white/20 bg-white/5 text-purple-500 focus:ring-purple-500"
                        />
                        <label htmlFor="dataMigration" className="text-white">
                            Data Migration Required
                        </label>
                    </div>
                    {requirements.dataMigrationRequired && (
                        <div>
                            <label className="block text-sm text-gray-400 mb-1">Estimated Patient Records</label>
                            <input
                                type="number"
                                min={0}
                                max={10000000}
                                value={requirements.estimatedPatientRecords || ""}
                                onChange={(e) => updateField("estimatedPatientRecords", e.target.value ? parseInt(e.target.value) : undefined)}
                                className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                            />
                        </div>
                    )}
                </div>
            </div>

            {/* IT Contact */}
            <div>
                <h4 className="text-white font-medium mb-4">IT Contact</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Name *</label>
                        <input
                            type="text"
                            required
                            value={requirements.itContactName}
                            onChange={(e) => updateField("itContactName", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Email *</label>
                        <input
                            type="email"
                            required
                            value={requirements.itContactEmail}
                            onChange={(e) => updateField("itContactEmail", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Phone *</label>
                        <input
                            type="tel"
                            required
                            value={requirements.itContactPhone}
                            onChange={(e) => updateField("itContactPhone", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Submit */}
            <div className="flex justify-end">
                <button
                    type="submit"
                    disabled={saving}
                    className="px-6 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
                >
                    {saving ? "Saving..." : "Save Requirements"}
                </button>
            </div>
        </form>
    );
}
