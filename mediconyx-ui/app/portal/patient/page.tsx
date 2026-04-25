"use client";
import { useState } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import PatientAIChat from "../../../components/patient/PatientAIChat";
import PatientReportUpload from "../../../components/patient/PatientReportUpload";
import { Bot, FileUp, Calendar, User } from "lucide-react";

export default function PatientPortal() {
  const [activeTab, setActiveTab] = useState("chat");

  const menuItems = [
    { id: "chat", label: "AI Assistant", icon: Bot, isAi: true },
    { id: "reports", label: "My Reports", icon: FileUp },
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "profile", label: "My Profile", icon: User },
  ];

  return (
    <ProtectedRoute allowedRoles={["Patient"]}>
      <ClinicalPortalLayout
        portalLabel="Patient Portal"
        activeItem={activeTab}
        navItems={menuItems}
        onNavigate={setActiveTab}
        primaryActionLabel="Upload Report"
        searchPlaceholder="Search reports, appointments..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">
              {menuItems.find(m => m.id === activeTab)?.label || "AI Assistant"}
            </h2>
            <p className="mt-1 text-sm text-slate-500">Access your health assistant, uploaded reports, appointments, and profile information.</p>
          </div>
        </div>

        {activeTab === "chat" && (
          <div className="h-[calc(100vh-176px)]">
            <PatientAIChat />
          </div>
        )}

        {activeTab === "reports" && <PatientReportUpload />}

        {activeTab === "appointments" && (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-8 text-center">
            <Calendar className="w-12 h-12 text-gray-600 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-white mb-2">Appointments</h3>
            <p className="text-gray-500">
              Your appointment history and upcoming visits will appear here.
            </p>
          </div>
        )}

        {activeTab === "profile" && (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-8 text-center">
            <User className="w-12 h-12 text-gray-600 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-white mb-2">My Profile</h3>
            <p className="text-gray-500">
              Your profile information and settings will appear here.
            </p>
          </div>
        )}
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}
