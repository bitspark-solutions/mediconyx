"use client";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";

export default function AccountantPortal() {
  return (
    <ProtectedRoute allowedRoles={["Accountant"]}>
      <main>
        <Header />
        <div className="p-6">
          <h1 className="text-xl font-semibold">Accountant Portal</h1>
        </div>
      </main>
    </ProtectedRoute>
  );
}
