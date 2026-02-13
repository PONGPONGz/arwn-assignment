"use client";

import { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import { api, ApiRequestError } from "@/lib/api";
import type { PatientResponse } from "@/types/patient";
import type { BranchResponse } from "@/types/branch";

export default function PatientsPage() {
  const [patients, setPatients] = useState<PatientResponse[]>([]);
  const [branches, setBranches] = useState<BranchResponse[]>([]);
  const [selectedBranch, setSelectedBranch] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPatients = useCallback(async (branchId?: string) => {
    setLoading(true);
    setError(null);
    try {
      const path = branchId
        ? `/api/v1/patients?branchId=${branchId}`
        : "/api/v1/patients";
      const data = await api.get<PatientResponse[]>(path);
      setPatients(data);
    } catch (err) {
      if (err instanceof ApiRequestError) {
        setError(err.message);
      } else {
        setError("Failed to load patients");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    api
      .get<BranchResponse[]>("/api/v1/branches")
      .then(setBranches)
      .catch(() => {});
    fetchPatients();
  }, [fetchPatients]);

  const handleBranchFilter = (branchId: string) => {
    setSelectedBranch(branchId);
    fetchPatients(branchId || undefined);
  };

  return (
    <div>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "1rem",
        }}
      >
        <h1>Patients</h1>
        <Link
          href="/patients/new"
          style={{
            padding: "0.5rem 1rem",
            background: "#0070f3",
            color: "white",
            borderRadius: "4px",
            textDecoration: "none",
          }}
        >
          + New Patient
        </Link>
      </div>

      <div style={{ marginBottom: "1rem" }}>
        <label htmlFor="branchFilter" style={{ marginRight: "0.5rem" }}>
          Filter by Branch:
        </label>
        <select
          id="branchFilter"
          value={selectedBranch}
          onChange={(e) => handleBranchFilter(e.target.value)}
          style={{ padding: "0.4rem" }}
        >
          <option value="">All Branches</option>
          {branches.map((b) => (
            <option key={b.id} value={b.id}>
              {b.name}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <div
          style={{
            padding: "0.75rem",
            background: "#fdd",
            border: "1px solid #c00",
            borderRadius: "4px",
            marginBottom: "1rem",
          }}
        >
          {error}
        </div>
      )}

      {loading ? (
        <p>Loading...</p>
      ) : patients.length === 0 ? (
        <p>No patients found.</p>
      ) : (
        <table
          style={{
            width: "100%",
            borderCollapse: "collapse",
          }}
        >
          <thead>
            <tr
              style={{
                borderBottom: "2px solid #ddd",
                textAlign: "left",
              }}
            >
              <th style={{ padding: "0.5rem" }}>First Name</th>
              <th style={{ padding: "0.5rem" }}>Last Name</th>
              <th style={{ padding: "0.5rem" }}>Phone</th>
              <th style={{ padding: "0.5rem" }}>Branch</th>
              <th style={{ padding: "0.5rem" }}>Created</th>
            </tr>
          </thead>
          <tbody>
            {patients.map((p) => (
              <tr
                key={p.id}
                style={{ borderBottom: "1px solid #eee" }}
              >
                <td style={{ padding: "0.5rem" }}>{p.firstName}</td>
                <td style={{ padding: "0.5rem" }}>{p.lastName}</td>
                <td style={{ padding: "0.5rem" }}>{p.phoneNumber}</td>
                <td style={{ padding: "0.5rem" }}>
                  {p.primaryBranchName || "—"}
                </td>
                <td style={{ padding: "0.5rem" }}>
                  {new Date(p.createdAt).toLocaleDateString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
