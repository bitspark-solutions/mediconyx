"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function NursePortal() {
  return (
    <ProtectedRoute allowedRoles={["Nurse"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Nurse Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
