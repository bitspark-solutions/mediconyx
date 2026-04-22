"use client";

import { useState } from "react";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";
import AdminReviewQueue from "../../../components/admin/AdminReviewQueue";
import { ListChecks, Users, Settings } from "lucide-react";

export default function AdminPortal() {
  const [activeTab, setActiveTab] = useState("review");

  return (
    <ProtectedRoute allowedRoles={["Admin", "Master"]}>
      <main className="min-h-screen bg-black text-white">
        <Header />
        <div className="max-w-7xl mx-auto p-6">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-pink-400">
              Admin Portal
            </h1>
          </div>

          {/* KPI Cards Placeholder */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">Pending Reviews</h3>
              <div className="text-3xl font-bold text-white">--</div>
            </div>
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">Active Tenants</h3>
              <div className="text-3xl font-bold text-white">--</div>
            </div>
            <div className="bg-white/5 border border-white/10 rounded-xl p-6">
              <h3 className="text-gray-400 text-sm font-medium mb-2">System Health</h3>
              <div className="text-3xl font-bold text-green-400">Good</div>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex items-center gap-1 border-b border-white/10 mb-6">
            <button
              onClick={() => setActiveTab("review")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "review"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <ListChecks className="w-4 h-4" /> Review Queue
            </button>
            <button
              onClick={() => setActiveTab("tenants")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "tenants"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <Users className="w-4 h-4" /> All Tenants
            </button>
            <button
              onClick={() => setActiveTab("settings")}
              className={`px-4 py-2 text-sm font-medium flex items-center gap-2 border-b-2 transition-colors ${activeTab === "settings"
                  ? "border-purple-500 text-purple-400"
                  : "border-transparent text-gray-400 hover:text-white"
                }`}
            >
              <Settings className="w-4 h-4" /> Settings
            </button>
          </div>

          {/* Content */}
          <div className="min-h-[400px]">
            {activeTab === "review" && <AdminReviewQueue />}

            {activeTab === "tenants" && (
              <div className="text-center py-12 text-gray-500">
                <Users className="w-12 h-12 mx-auto mb-3 opacity-20" />
                <p>Tenant management coming soon...</p>
              </div>
            )}

            {activeTab === "settings" && (
              <div className="text-center py-12 text-gray-500">
                <Settings className="w-12 h-12 mx-auto mb-3 opacity-20" />
                <p>System settings coming soon...</p>
              </div>
            )}
          </div>
        </div>
      </main>
    </ProtectedRoute>
  );
}
