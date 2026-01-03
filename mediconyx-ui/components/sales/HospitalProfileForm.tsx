"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";

interface HospitalProfileFormProps {
    requestId: number;
    onSaved?: () => void;
}

interface HospitalProfile {
    hospitalLegalName: string;
    licenseNumber: string;
    taxId: string;
    hospitalType: number;
    bedCapacity: number;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    stateProvince: string;
    postalCode: string;
    country: string;
    primaryContactName: string;
    primaryContactEmail: string;
    primaryContactPhone: string;
    secondaryContactName?: string;
    secondaryContactEmail?: string;
    secondaryContactPhone?: string;
    websiteUrl?: string;
    establishedYear?: number;
}

const hospitalTypes = [
    { value: 0, label: "Public" },
    { value: 1, label: "Private" },
    { value: 2, label: "Specialized" },
    { value: 3, label: "Clinic" },
];

const emptyProfile: HospitalProfile = {
    hospitalLegalName: "",
    licenseNumber: "",
    taxId: "",
    hospitalType: 1,
    bedCapacity: 50,
    addressLine1: "",
    addressLine2: "",
    city: "",
    stateProvince: "",
    postalCode: "",
    country: "Bangladesh",
    primaryContactName: "",
    primaryContactEmail: "",
    primaryContactPhone: "",
    secondaryContactName: "",
    secondaryContactEmail: "",
    secondaryContactPhone: "",
    websiteUrl: "",
    establishedYear: undefined,
};

export default function HospitalProfileForm({ requestId, onSaved }: HospitalProfileFormProps) {
    const [profile, setProfile] = useState<HospitalProfile>(emptyProfile);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        loadProfile();
    }, [requestId]);

    const loadProfile = async () => {
        setLoading(true);
        try {
            const res = await apiClient.get(`/api/sales/requests/${requestId}/hospital-profile`);
            if (res.ok) {
                const data = await res.json();
                setProfile(data);
            } else if (res.status === 404) {
                // No profile yet, use empty form
                setProfile(emptyProfile);
            }
        } catch (err) {
            console.error("Failed to load hospital profile", err);
        } finally {
            setLoading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setError("");
        setSuccess(false);

        try {
            const res = await apiClient.post(`/api/sales/requests/${requestId}/hospital-profile`, profile);
            if (res.ok) {
                setSuccess(true);
                onSaved?.();
                setTimeout(() => setSuccess(false), 3000);
            } else {
                const data = await res.json();
                setError(data.message || "Failed to save profile");
            }
        } catch (err) {
            setError("Error saving profile");
        } finally {
            setSaving(false);
        }
    };

    const updateField = (field: keyof HospitalProfile, value: any) => {
        setProfile((prev) => ({ ...prev, [field]: value }));
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
                    Profile saved successfully!
                </div>
            )}

            {/* Legal Information */}
            <div>
                <h4 className="text-white font-medium mb-4">Legal Information</h4>
                <div className="grid grid-cols-2 gap-4">
                    <div className="col-span-2">
                        <label className="block text-sm text-gray-400 mb-1">Hospital Legal Name *</label>
                        <input
                            type="text"
                            required
                            value={profile.hospitalLegalName}
                            onChange={(e) => updateField("hospitalLegalName", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">License Number *</label>
                        <input
                            type="text"
                            required
                            value={profile.licenseNumber}
                            onChange={(e) => updateField("licenseNumber", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Tax ID *</label>
                        <input
                            type="text"
                            required
                            value={profile.taxId}
                            onChange={(e) => updateField("taxId", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Hospital Type *</label>
                        <select
                            required
                            value={profile.hospitalType}
                            onChange={(e) => updateField("hospitalType", parseInt(e.target.value))}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        >
                            {hospitalTypes.map((type) => (
                                <option key={type.value} value={type.value}>
                                    {type.label}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Bed Capacity *</label>
                        <input
                            type="number"
                            required
                            min={1}
                            max={10000}
                            value={profile.bedCapacity}
                            onChange={(e) => updateField("bedCapacity", parseInt(e.target.value) || 1)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Address Information */}
            <div>
                <h4 className="text-white font-medium mb-4">Address</h4>
                <div className="grid grid-cols-2 gap-4">
                    <div className="col-span-2">
                        <label className="block text-sm text-gray-400 mb-1">Address Line 1 *</label>
                        <input
                            type="text"
                            required
                            value={profile.addressLine1}
                            onChange={(e) => updateField("addressLine1", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div className="col-span-2">
                        <label className="block text-sm text-gray-400 mb-1">Address Line 2</label>
                        <input
                            type="text"
                            value={profile.addressLine2 || ""}
                            onChange={(e) => updateField("addressLine2", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">City *</label>
                        <input
                            type="text"
                            required
                            value={profile.city}
                            onChange={(e) => updateField("city", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">State/Province *</label>
                        <input
                            type="text"
                            required
                            value={profile.stateProvince}
                            onChange={(e) => updateField("stateProvince", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Postal Code *</label>
                        <input
                            type="text"
                            required
                            value={profile.postalCode}
                            onChange={(e) => updateField("postalCode", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Country *</label>
                        <input
                            type="text"
                            required
                            value={profile.country}
                            onChange={(e) => updateField("country", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Primary Contact */}
            <div>
                <h4 className="text-white font-medium mb-4">Primary Contact</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Name *</label>
                        <input
                            type="text"
                            required
                            value={profile.primaryContactName}
                            onChange={(e) => updateField("primaryContactName", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Email *</label>
                        <input
                            type="email"
                            required
                            value={profile.primaryContactEmail}
                            onChange={(e) => updateField("primaryContactEmail", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Phone *</label>
                        <input
                            type="tel"
                            required
                            value={profile.primaryContactPhone}
                            onChange={(e) => updateField("primaryContactPhone", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Secondary Contact */}
            <div>
                <h4 className="text-white font-medium mb-4">Secondary Contact (Optional)</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Name</label>
                        <input
                            type="text"
                            value={profile.secondaryContactName || ""}
                            onChange={(e) => updateField("secondaryContactName", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Email</label>
                        <input
                            type="email"
                            value={profile.secondaryContactEmail || ""}
                            onChange={(e) => updateField("secondaryContactEmail", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Phone</label>
                        <input
                            type="tel"
                            value={profile.secondaryContactPhone || ""}
                            onChange={(e) => updateField("secondaryContactPhone", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Additional Info */}
            <div>
                <h4 className="text-white font-medium mb-4">Additional Information</h4>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Website URL</label>
                        <input
                            type="url"
                            value={profile.websiteUrl || ""}
                            onChange={(e) => updateField("websiteUrl", e.target.value)}
                            placeholder="https://"
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Established Year</label>
                        <input
                            type="number"
                            min={1800}
                            max={2100}
                            value={profile.establishedYear || ""}
                            onChange={(e) => updateField("establishedYear", e.target.value ? parseInt(e.target.value) : undefined)}
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
                    {saving ? "Saving..." : "Save Profile"}
                </button>
            </div>
        </form>
    );
}
