"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";

interface PricingFormProps {
    requestId: number;
    onSaved?: () => void;
}

interface PricingData {
    subscriptionPlan: string;
    billingCycle: number;
    currency: string;
    quotedPrice: number | null;
    seats: number | null;
    discountPercent: number;
    contractDurationMonths: number;
    autoRenewal: boolean;
    specialTerms: string;
}

const subscriptionPlans = [
    { value: "Basic", label: "Basic", basePrice: 500 },
    { value: "Standard", label: "Standard", basePrice: 1000 },
    { value: "Premium", label: "Premium", basePrice: 2000 },
    { value: "Enterprise", label: "Enterprise", basePrice: 5000 },
];

const billingCycles = [
    { value: 0, label: "Monthly" },
    { value: 1, label: "Annual (10% discount)" },
];

const currencies = ["USD", "BDT", "EUR", "GBP"];

const emptyPricing: PricingData = {
    subscriptionPlan: "Standard",
    billingCycle: 0,
    currency: "BDT",
    quotedPrice: null,
    seats: 50,
    discountPercent: 0,
    contractDurationMonths: 12,
    autoRenewal: true,
    specialTerms: "",
};

export default function PricingForm({ requestId, onSaved }: PricingFormProps) {
    const [pricing, setPricing] = useState<PricingData>(emptyPricing);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        loadPricing();
    }, [requestId]);

    const loadPricing = async () => {
        setLoading(true);
        try {
            const res = await apiClient.get(`/api/sales/requests/${requestId}`);
            if (res.ok) {
                const data = await res.json();
                setPricing({
                    subscriptionPlan: data.subscriptionPlan || "Standard",
                    billingCycle: data.billingCycle ?? 0,
                    currency: data.currency || "BDT",
                    quotedPrice: data.quotedPrice,
                    seats: data.seats || 50,
                    discountPercent: data.discountPercent || 0,
                    contractDurationMonths: data.contractDurationMonths || 12,
                    autoRenewal: data.autoRenewal ?? true,
                    specialTerms: data.specialTerms || "",
                });
            }
        } catch (err) {
            console.error("Failed to load pricing", err);
        } finally {
            setLoading(false);
        }
    };

    const calculatePrice = () => {
        const plan = subscriptionPlans.find((p) => p.value === pricing.subscriptionPlan);
        if (!plan) return 0;

        let basePrice = plan.basePrice * (pricing.seats || 1);

        // Annual discount
        if (pricing.billingCycle === 1) {
            basePrice = basePrice * 12 * 0.9; // 10% annual discount
        }

        // Additional discount
        if (pricing.discountPercent > 0) {
            basePrice = basePrice * (1 - pricing.discountPercent / 100);
        }

        return Math.round(basePrice);
    };

    const handleCalculate = () => {
        setPricing((prev) => ({ ...prev, quotedPrice: calculatePrice() }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        setError("");
        setSuccess(false);

        try {
            const res = await apiClient.patch(`/api/sales/requests/${requestId}/pricing`, {
                subscriptionPlan: pricing.subscriptionPlan,
                billingCycle: pricing.billingCycle,
                currency: pricing.currency,
                quotedPrice: pricing.quotedPrice,
                seats: pricing.seats,
            });
            if (res.ok) {
                setSuccess(true);
                onSaved?.();
                setTimeout(() => setSuccess(false), 3000);
            } else {
                const data = await res.json();
                setError(data.message || "Failed to save pricing");
            }
        } catch (err) {
            setError("Error saving pricing");
        } finally {
            setSaving(false);
        }
    };

    const updateField = (field: keyof PricingData, value: any) => {
        setPricing((prev) => ({ ...prev, [field]: value }));
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
                    Pricing saved successfully!
                </div>
            )}

            {/* Subscription Plan */}
            <div>
                <h4 className="text-white font-medium mb-4">Subscription Plan</h4>
                <div className="grid grid-cols-4 gap-3">
                    {subscriptionPlans.map((plan) => (
                        <button
                            key={plan.value}
                            type="button"
                            onClick={() => updateField("subscriptionPlan", plan.value)}
                            className={`p-4 rounded-lg border transition-colors text-left ${pricing.subscriptionPlan === plan.value
                                    ? "bg-purple-600/20 border-purple-500/50 text-white"
                                    : "bg-white/5 border-white/10 text-gray-300 hover:bg-white/10"
                                }`}
                        >
                            <span className="block font-medium">{plan.label}</span>
                            <span className="block text-sm text-gray-400 mt-1">
                                ${plan.basePrice}/user/mo
                            </span>
                        </button>
                    ))}
                </div>
            </div>

            {/* Billing Details */}
            <div>
                <h4 className="text-white font-medium mb-4">Billing Details</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Billing Cycle</label>
                        <select
                            value={pricing.billingCycle}
                            onChange={(e) => updateField("billingCycle", parseInt(e.target.value))}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        >
                            {billingCycles.map((cycle) => (
                                <option key={cycle.value} value={cycle.value}>
                                    {cycle.label}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Currency</label>
                        <select
                            value={pricing.currency}
                            onChange={(e) => updateField("currency", e.target.value)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        >
                            {currencies.map((curr) => (
                                <option key={curr} value={curr}>
                                    {curr}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">User Seats</label>
                        <input
                            type="number"
                            min={1}
                            max={10000}
                            value={pricing.seats || ""}
                            onChange={(e) => updateField("seats", e.target.value ? parseInt(e.target.value) : null)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                </div>
            </div>

            {/* Discount & Terms */}
            <div>
                <h4 className="text-white font-medium mb-4">Discount & Terms</h4>
                <div className="grid grid-cols-3 gap-4">
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Discount %</label>
                        <input
                            type="number"
                            min={0}
                            max={50}
                            value={pricing.discountPercent}
                            onChange={(e) => updateField("discountPercent", parseInt(e.target.value) || 0)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div>
                        <label className="block text-sm text-gray-400 mb-1">Contract Duration (months)</label>
                        <input
                            type="number"
                            min={1}
                            max={60}
                            value={pricing.contractDurationMonths}
                            onChange={(e) => updateField("contractDurationMonths", parseInt(e.target.value) || 12)}
                            className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none"
                        />
                    </div>
                    <div className="flex items-end">
                        <label className="flex items-center gap-3 cursor-pointer">
                            <input
                                type="checkbox"
                                checked={pricing.autoRenewal}
                                onChange={(e) => updateField("autoRenewal", e.target.checked)}
                                className="w-5 h-5 rounded border-white/20 bg-white/5 text-purple-500 focus:ring-purple-500"
                            />
                            <span className="text-white">Auto Renewal</span>
                        </label>
                    </div>
                </div>
            </div>

            {/* Special Terms */}
            <div>
                <label className="block text-sm text-gray-400 mb-1">Special Terms (optional)</label>
                <textarea
                    value={pricing.specialTerms}
                    onChange={(e) => updateField("specialTerms", e.target.value)}
                    placeholder="Any special pricing arrangements or terms..."
                    rows={3}
                    className="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none"
                />
            </div>

            {/* Price Calculator */}
            <div className="p-6 rounded-xl bg-gradient-to-r from-purple-600/20 to-blue-600/20 border border-purple-500/30">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-gray-400 text-sm">Calculated Price</p>
                        <p className="text-3xl font-bold text-white mt-1">
                            {pricing.currency} {calculatePrice().toLocaleString()}
                            <span className="text-lg font-normal text-gray-400">
                                {pricing.billingCycle === 0 ? " /month" : " /year"}
                            </span>
                        </p>
                    </div>
                    <button
                        type="button"
                        onClick={handleCalculate}
                        className="px-4 py-2 bg-white/10 text-white rounded-lg hover:bg-white/20"
                    >
                        Apply to Quote
                    </button>
                </div>
                {pricing.quotedPrice && (
                    <div className="mt-4 pt-4 border-t border-white/10">
                        <p className="text-gray-400 text-sm">Quoted Price</p>
                        <p className="text-2xl font-semibold text-green-400">
                            {pricing.currency} {pricing.quotedPrice.toLocaleString()}
                        </p>
                    </div>
                )}
            </div>

            {/* Submit */}
            <div className="flex justify-end">
                <button
                    type="submit"
                    disabled={saving || !pricing.quotedPrice}
                    className="px-6 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
                >
                    {saving ? "Saving..." : "Save Pricing"}
                </button>
            </div>
        </form>
    );
}
