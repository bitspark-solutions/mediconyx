"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function PatientPortal() {
  return (
    <ProtectedRoute allowedRoles={["Patient"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Patient Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
