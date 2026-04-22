"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ReceptionistSidebar from "../../../components/receptionist/ReceptionistSidebar";
import ReceptionistTopbar from "../../../components/receptionist/ReceptionistTopbar";
import ReceptionistDashboard from "../../../components/receptionist/ReceptionistDashboard";
import PatientList from "../../../components/receptionist/PatientList";
import AppointmentBooking from "../../../components/receptionist/AppointmentBooking";
import AppointmentList from "../../../components/receptionist/AppointmentList";

export default function ReceptionistPortal() {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeMenu, setActiveMenu] = useState("dashboard");
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return (
      <ProtectedRoute allowedRoles={["Receptionist"]}>
        <div className="min-h-screen bg-black flex items-center justify-center">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-purple-500"></div>
        </div>
      </ProtectedRoute>
    );
  }

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
      <main className="min-h-screen bg-black">
        <ReceptionistSidebar
          sidebarOpen={sidebarOpen}
          setSidebarOpen={setSidebarOpen}
          activeMenu={activeMenu}
          setActiveMenu={setActiveMenu}
        />
        <ReceptionistTopbar title={getPageTitle()} />
        <div className={`pt-20 transition-all duration-300 ${sidebarOpen ? "pl-64" : "pl-20"}`}>
          <div className="p-6 max-w-7xl mx-auto">
            {renderContent()}
          </div>
        </div>
      </main>
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