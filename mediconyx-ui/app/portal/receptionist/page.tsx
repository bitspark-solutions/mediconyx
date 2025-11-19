"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function ReceptionistPortal() {
  return (
    <ProtectedRoute allowedRoles={["Receptionist"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Receptionist Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
