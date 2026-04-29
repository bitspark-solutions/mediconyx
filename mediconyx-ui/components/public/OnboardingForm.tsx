"use client";
import { useState } from "react";
import { apiClient } from "../../lib/apiClient";

export default function OnboardingForm() {
    const [formData, setFormData] = useState({
        hospitalName: "",
        contactPerson: "",
        email: "",
        phone: "",
        region: "",
        seats: "", // Mapped to Total Employees
        notes: "",
    });
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);
    const [error, setError] = useState("");

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        setError("");

        try {
            // In development, we can send any string as captcha
            const payload = {
                ...formData,
                seats: formData.seats ? parseInt(formData.seats) : null,
                captchaToken: "dev-bypass-token",
            };

            // Use the public endpoint which allows anonymous access
            const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/sales/requests/public`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });

            if (res.ok) {
                setSuccess(true);
                setFormData({
                    hospitalName: "",
                    contactPerson: "",
                    email: "",
                    phone: "",
                    region: "",
                    seats: "",
                    notes: "",
                });
            } else {
                const data = await res.json();
                setError(data.message || "Failed to submit request");
            }
        } catch (err) {
            setError("An unexpected error occurred.");
        } finally {
            setLoading(false);
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        setFormData((prev) => ({ ...prev, [e.target.name]: e.target.value }));
    };

    if (success) {
        return (
            <div className="bg-gradient-to-br from-green-500/10 to-emerald-500/10 border border-green-500/20 rounded-2xl p-8 text-center backdrop-blur-sm">
                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-500/20 mb-6">
                    <svg className="h-8 w-8 text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                </div>
                <h3 className="text-2xl font-bold text-white mb-2">Request Submitted!</h3>
                <p className="text-gray-300 mb-6">
                    Thank you for your interest in Mediconyx. Our sales team has been notified and will contact you shortly at <strong>{formData.email}</strong>.
                </p>
                <button
                    onClick={() => setSuccess(false)}
                    className="text-green-400 hover:text-green-300 font-medium transition-colors"
                >
                    Submit another request
                </button>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-6 bg-white/5 border border-white/10 rounded-2xl p-8 backdrop-blur-md shadow-2xl">
            <div className="space-y-2">
                <h3 className="text-xl font-semibold text-white">Join the Network</h3>
                <p className="text-sm text-gray-400">Fill in your hospital details to get started.</p>
            </div>

            {error && (
                <div className="rounded-lg bg-red-500/20 border border-red-500/30 px-4 py-3 text-sm text-red-200">
                    {error}
                </div>
            )}

            <div className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">
                        Hospital / Clinic Name <span className="text-red-400">*</span>
                    </label>
                    <input
                        type="text"
                        name="hospitalName"
                        required
                        value={formData.hospitalName}
                        onChange={handleChange}
                        placeholder="e.g. City General Hospital"
                        className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                    />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Contact Person <span className="text-red-400">*</span>
                        </label>
                        <input
                            type="text"
                            name="contactPerson"
                            required
                            value={formData.contactPerson}
                            onChange={handleChange}
                            placeholder="Full Name"
                            className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Total Employees (Approx.)
                        </label>
                        <input
                            type="number"
                            name="seats"
                            min="1"
                            value={formData.seats}
                            onChange={handleChange}
                            placeholder="e.g. 150"
                            className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                        />
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Email Address <span className="text-red-400">*</span>
                        </label>
                        <input
                            type="email"
                            name="email"
                            required
                            value={formData.email}
                            onChange={handleChange}
                            placeholder="contact@hospital.com"
                            className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-300 mb-1">
                            Phone Number
                        </label>
                        <input
                            type="tel"
                            name="phone"
                            value={formData.phone}
                            onChange={handleChange}
                            placeholder="+880..."
                            className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                        />
                    </div>
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">
                        Region / City
                    </label>
                    <input
                        type="text"
                        name="region"
                        value={formData.region}
                        onChange={handleChange}
                        placeholder="e.g. Dhaka"
                        className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all"
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">
                        Requirements / Message
                    </label>
                    <textarea
                        name="notes"
                        rows={4}
                        value={formData.notes}
                        onChange={handleChange}
                        placeholder="Tell us about your hospital size, departments, or any specific needs..."
                        className="w-full rounded-lg bg-black/20 border border-white/10 px-4 py-3 text-white placeholder-gray-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500 transition-all resize-none"
                    />
                </div>
            </div>

            <button
                type="submit"
                disabled={loading}
                className="w-full rounded-lg bg-gradient-to-r from-purple-600 to-blue-600 px-4 py-3.5 font-semibold text-white shadow-lg shadow-purple-500/20 hover:from-purple-500 hover:to-blue-500 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:ring-offset-2 focus:ring-offset-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-all transform hover:-translate-y-0.5"
            >
                {loading ? (
                    <span className="flex items-center justify-center gap-2">
                        <svg className="animate-spin h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        Submitting...
                    </span>
                ) : (
                    "Submit Request"
                )}
            </button>

            <p className="text-center text-xs text-gray-500 mt-4">
                By submitting this form, you agree to our Terms of Service and Privacy Policy.
            </p>
        </form>
    );
}
