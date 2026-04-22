"use client";
import { useState, useEffect } from "react";
import ProtectedRoute from "../../../components/ProtectedRoute";
import PatientAIChat from "../../../components/patient/PatientAIChat";
import PatientReportUpload from "../../../components/patient/PatientReportUpload";
import { Bot, FileUp, Calendar, User, Settings, LogOut, LayoutDashboard } from "lucide-react";
import { useAuthStore } from "../../../store/useAuthStore";

export default function PatientPortal() {
  const [activeTab, setActiveTab] = useState("chat");
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const { user, logout } = useAuthStore();

  const menuItems = [
    { id: "chat", label: "AI Assistant", icon: Bot },
    { id: "reports", label: "My Reports", icon: FileUp },
    { id: "appointments", label: "Appointments", icon: Calendar },
    { id: "profile", label: "My Profile", icon: User },
  ];

  return (
    <ProtectedRoute allowedRoles={["Patient"]}>
      <main className="min-h-screen bg-black flex">
        {/* Sidebar */}
        <aside
          className={`${
            sidebarOpen ? "w-64" : "w-20"
          } bg-gray-900 border-r border-gray-800 flex flex-col fixed h-full z-40 transition-all duration-300`}
        >
          {/* Logo */}
          <div className="p-4 border-b border-gray-800">
            <div className="flex items-center justify-between">
              {sidebarOpen && (
                <div>
                  <h1 className="text-xl font-bold text-purple-400">Mediconyx</h1>
                  <p className="text-xs text-gray-500 mt-0.5">Patient Portal</p>
                </div>
              )}
              <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className="p-1.5 rounded-lg text-gray-400 hover:text-white hover:bg-gray-800 transition-colors"
              >
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>
            </div>
          </div>

          {/* Navigation */}
          <nav className="flex-1 overflow-y-auto py-4 px-3">
            <div className="space-y-1">
              {menuItems.map((item) => {
                const Icon = item.icon;
                return (
                  <button
                    key={item.id}
                    onClick={() => setActiveTab(item.id)}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors ${
                      activeTab === item.id
                        ? "bg-purple-600 text-white"
                        : "text-gray-400 hover:bg-gray-800 hover:text-white"
                    }`}
                  >
                    <Icon className="w-5 h-5 flex-shrink-0" />
                    {sidebarOpen && <span className="text-sm font-medium">{item.label}</span>}
                  </button>
                );
              })}
            </div>
          </nav>

          {/* User & Logout */}
          <div className="p-4 border-t border-gray-800">
            {sidebarOpen && user && (
              <div className="mb-3 px-2">
                <p className="text-sm font-medium text-white truncate">
                  {user.firstName} {user.lastName}
                </p>
                <p className="text-xs text-gray-500 truncate">{user.email}</p>
              </div>
            )}
            <button
              onClick={logout}
              className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-gray-400 hover:bg-red-500/10 hover:text-red-400 transition-colors"
            >
              <LogOut className="w-5 h-5 flex-shrink-0" />
              {sidebarOpen && <span className="text-sm font-medium">Logout</span>}
            </button>
          </div>
        </aside>

        {/* Main Content */}
        <div className={`flex-1 transition-all duration-300 ${sidebarOpen ? "ml-64" : "ml-20"}`}>
          {/* Top Bar */}
          <header className="sticky top-0 bg-black/80 backdrop-blur-md border-b border-gray-800 px-6 py-4 z-30">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-white">
                {menuItems.find(m => m.id === activeTab)?.label || "Dashboard"}
              </h2>
              <div className="flex items-center gap-3">
                <div className="text-right">
                  <p className="text-sm text-white">
                    {new Date().toLocaleDateString("en-US", {
                      weekday: "long",
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </div>
          </header>

          {/* Content */}
          <div className="p-6">
            {activeTab === "chat" && (
              <div className="h-[calc(100vh-140px)]">
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
          </div>
        </div>
      </main>
    </ProtectedRoute>
  );
}
