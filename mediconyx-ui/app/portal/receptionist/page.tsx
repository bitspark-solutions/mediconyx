"use client";
import { useState } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import ReceptionistDashboard from "../../../components/receptionist/ReceptionistDashboard";
import PatientList from "../../../components/receptionist/PatientList";
import AppointmentBooking from "../../../components/receptionist/AppointmentBooking";
import AppointmentList from "../../../components/receptionist/AppointmentList";
import { Calendar, ClipboardList, LayoutDashboard, Settings, Users } from "lucide-react";

export default function ReceptionistPortal() {
  const [activeMenu, setActiveMenu] = useState("dashboard");

  const menuItems = [
    { id: "dashboard", label: "Dashboard", icon: LayoutDashboard },
    { id: "patients", label: "Patients", icon: Users },
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "queue", label: "Queue", icon: ClipboardList },
    { id: "settings", label: "Settings", icon: Settings },
  ];

  const getPageTitle = () => {
    switch (activeMenu) {
      case "dashboard": return "Dashboard";
      case "patients": return "Patient Registry";
      case "appointments": return "Appointment Booking";
      case "queue": return "Appointment Queue";
      case "settings": return "Settings";
      default: return "Dashboard";
    }
  };

  const renderContent = () => {
    switch (activeMenu) {
      case "dashboard": return <ReceptionistDashboard />;
      case "patients": return <PatientList />;
      case "appointments": return <AppointmentBooking />;
      case "queue": return <AppointmentList />;
      case "settings": return <SettingsPage />;
      default: return <ReceptionistDashboard />;
    }
  };

  return (
    <ProtectedRoute allowedRoles={["Receptionist"]}>
      <ClinicalPortalLayout
        portalLabel="Receptionist Portal"
        activeItem={activeMenu}
        navItems={menuItems}
        onNavigate={setActiveMenu}
        primaryActionLabel="New Admission"
        searchPlaceholder="Search patients, appointments..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">{getPageTitle()}</h2>
            <p className="mt-1 text-sm text-slate-500">Coordinate patient intake, appointment booking, and front-desk queue operations.</p>
          </div>
        </div>
        {renderContent()}
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}

function SettingsPage() {
  return (
    <div className="space-y-6">
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 className="text-xl font-semibold text-white mb-4">Settings</h2>
        <p className="text-gray-500">Settings panel coming soon...</p>
      </div>
    </div>
  );
}