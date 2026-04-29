"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";

interface ModuleSelectionFormProps {
    requestId: number;
    onSaved?: () => void;
}

interface ModuleSelection {
    opdManagement: boolean;
    ipdManagement: boolean;
    emergencyServices: boolean;
    pharmacyManagement: boolean;
    laboratory: boolean;
    radiologyImaging: boolean;
    billingInvoicing: boolean;
    inventoryManagement: boolean;
    hrPayroll: boolean;
    appointmentScheduling: boolean;
    telemedicine: boolean;
    analyticsReporting: boolean;
}

const moduleInfo = [
    { key: "opdManagement", label: "OPD Management", description: "Outpatient department scheduling, visits, prescriptions", category: "Clinical" },
    { key: "ipdManagement", label: "IPD Management", description: "Inpatient admissions, bed management, discharge", category: "Clinical" },
    { key: "emergencyServices", label: "Emergency Services", description: "Emergency room, triage, critical care", category: "Clinical" },
    { key: "pharmacyManagement", label: "Pharmacy Management", description: "Drug inventory, dispensing, prescriptions", category: "Clinical" },
    { key: "laboratory", label: "Laboratory", description: "Lab orders, results, sample tracking", category: "Clinical" },
    { key: "radiologyImaging", label: "Radiology & Imaging", description: "X-ray, CT, MRI orders and reports", category: "Clinical" },
    { key: "billingInvoicing", label: "Billing & Invoicing", description: "Patient billing, invoices, payments", category: "Financial" },
    { key: "inventoryManagement", label: "Inventory Management", description: "Medical supplies, equipment tracking", category: "Financial" },
    { key: "hrPayroll", label: "HR & Payroll", description: "Staff management, attendance, payroll", category: "Administrative" },
    { key: "appointmentScheduling", label: "Appointment Scheduling", description: "Online booking, calendar management", category: "Administrative" },
    { key: "telemedicine", label: "Telemedicine", description: "Video consultations, remote care", category: "Advanced" },
    { key: "analyticsReporting", label: "Analytics & Reporting", description: "Dashboards, reports, insights", category: "Advanced" },
];

const presets: Record<string, (keyof ModuleSelection)[]> = {
    basic: ["opdManagement", "pharmacyManagement", "billingInvoicing", "appointmentScheduling"],
    standard: ["opdManagement", "ipdManagement", "pharmacyManagement", "laboratory", "billingInvoicing", "inventoryManagement", "appointmentScheduling"],
    premium: ["opdManagement", "ipdManagement", "emergencyServices", "pharmacyManagement", "laboratory", "radiologyImaging", "billingInvoicing", "inventoryManagement", "hrPayroll", "appointmentScheduling", "analyticsReporting"],
    enterprise: ["opdManagement", "ipdManagement", "emergencyServices", "pharmacyManagement", "laboratory", "radiologyImaging", "billingInvoicing", "inventoryManagement", "hrPayroll", "appointmentScheduling", "telemedicine", "analyticsReporting"],
};

const emptyModules: ModuleSelection = {
    opdManagement: false,
    ipdManagement: false,
    emergencyServices: false,
    pharmacyManagement: false,
    laboratory: false,
    radiologyImaging: false,
    billingInvoicing: false,
    inventoryManagement: false,
    hrPayroll: false,
    appointmentScheduling: false,
    telemedicine: false,
    analyticsReporting: false,
};

export default function ModuleSelectionForm({ requestId, onSaved }: ModuleSelectionFormProps) {
    const [modules, setModules] = useState<ModuleSelection>(emptyModules);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        loadModules();
    }, [requestId]);

    const loadModules = async () => {
        setLoading(true);
        try {
            const res = await apiClient.get(`/api/sales/requests/${requestId}/modules`);
            if (res.ok) {
                const data = await res.json();
                setModules(data);
            } else if (res.status === 404) {
                setModules(emptyModules);
            }
        } catch (err) {
            console.error("Failed to load modules", err);
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
            const res = await apiClient.post(`/api/sales/requests/${requestId}/modules`, modules);
            if (res.ok) {
                setSuccess(true);
                onSaved?.();
                setTimeout(() => setSuccess(false), 3000);
            } else {
                const data = await res.json();
                setError(data.message || "Failed to save module selection");
            }
        } catch (err) {
            setError("Error saving modules");
        } finally {
            setSaving(false);
        }
    };

    const toggleModule = (key: keyof ModuleSelection) => {
        setModules((prev) => ({ ...prev, [key]: !prev[key] }));
    };

    const applyPreset = (preset: string) => {
        const selectedModules = presets[preset] || [];
        const newModules = { ...emptyModules };
        selectedModules.forEach((key) => {
            newModules[key] = true;
        });
        setModules(newModules);
    };

    const selectAll = () => {
        const allSelected: ModuleSelection = { ...emptyModules };
        (Object.keys(allSelected) as (keyof ModuleSelection)[]).forEach((key) => {
            allSelected[key] = true;
        });
        setModules(allSelected);
    };

    const clearAll = () => {
        setModules(emptyModules);
    };

    const selectedCount = Object.values(modules).filter(Boolean).length;

    if (loading) {
        return (
            <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500"></div>
            </div>
        );
    }

    const categories = Array.from(new Set(moduleInfo.map((m) => m.category)));

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
                <div className="rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200">
                    {error}
                </div>
            )}
            {success && (
                <div className="rounded-lg bg-green-500/20 border border-green-500/30 px-4 py-3 text-sm text-green-200">
                    Modules saved successfully!
                </div>
            )}

            {/* Presets */}
            <div className="flex items-center gap-3 flex-wrap">
                <span className="text-sm text-gray-400">Quick Select:</span>
                {Object.keys(presets).map((preset) => (
                    <button
                        key={preset}
                        type="button"
                        onClick={() => applyPreset(preset)}
                        className="px-3 py-1 text-sm rounded-lg bg-white/5 text-white hover:bg-white/10 capitalize"
                    >
                        {preset}
                    </button>
                ))}
                <button
                    type="button"
                    onClick={selectAll}
                    className="px-3 py-1 text-sm rounded-lg bg-purple-600/20 text-purple-300 hover:bg-purple-600/30"
                >
                    Select All
                </button>
                <button
                    type="button"
                    onClick={clearAll}
                    className="px-3 py-1 text-sm rounded-lg bg-red-600/20 text-red-300 hover:bg-red-600/30"
                >
                    Clear All
                </button>
            </div>

            {/* Module Grid by Category */}
            {categories.map((category) => (
                <div key={category}>
                    <h4 className="text-white font-medium mb-3">{category}</h4>
                    <div className="grid grid-cols-2 gap-3">
                        {moduleInfo
                            .filter((m) => m.category === category)
                            .map((module) => (
                                <label
                                    key={module.key}
                                    className={`flex items-start gap-3 p-4 rounded-lg cursor-pointer transition-colors ${modules[module.key as keyof ModuleSelection]
                                            ? "bg-purple-600/20 border border-purple-500/50"
                                            : "bg-white/5 border border-white/10 hover:bg-white/10"
                                        }`}
                                >
                                    <input
                                        type="checkbox"
                                        checked={modules[module.key as keyof ModuleSelection]}
                                        onChange={() => toggleModule(module.key as keyof ModuleSelection)}
                                        className="mt-1 w-5 h-5 rounded border-white/20 bg-white/5 text-purple-500 focus:ring-purple-500"
                                    />
                                    <div>
                                        <span className="text-white font-medium">{module.label}</span>
                                        <p className="text-sm text-gray-400 mt-0.5">{module.description}</p>
                                    </div>
                                </label>
                            ))}
                    </div>
                </div>
            ))}

            {/* Submit */}
            <div className="flex items-center justify-between pt-4 border-t border-white/10">
                <span className="text-sm text-gray-400">
                    {selectedCount} module{selectedCount !== 1 ? "s" : ""} selected
                </span>
                <button
                    type="submit"
                    disabled={saving}
                    className="px-6 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
                >
                    {saving ? "Saving..." : "Save Modules"}
                </button>
            </div>
        </form>
    );
}
