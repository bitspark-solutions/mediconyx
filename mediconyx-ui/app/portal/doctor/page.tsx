"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function DoctorPortal() {
  return (
    <ProtectedRoute allowedRoles={["Doctor"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Doctor Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
