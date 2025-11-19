"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function AdminPortal() {
  return (
    <ProtectedRoute allowedRoles={["Admin"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Admin Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
