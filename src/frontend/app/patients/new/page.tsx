"use client";

import { useState, useEffect, FormEvent } from "react";
import { useRouter } from "next/navigation";
import { api, ApiRequestError } from "@/lib/api";
import type { CreatePatientRequest } from "@/types/patient";
import type { BranchResponse } from "@/types/branch";

export default function NewPatientPage() {
  const router = useRouter();
  const [branches, setBranches] = useState<BranchResponse[]>([]);
  const [form, setForm] = useState<CreatePatientRequest>({
    tenantId: process.env.NEXT_PUBLIC_TENANT_ID || "a0000000-0000-0000-0000-000000000001",
    firstName: "",
    lastName: "",
    phoneNumber: "",
  });
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    api
      .get<BranchResponse[]>("/api/v1/branches")
      .then(setBranches)
      .catch(() => {});
  }, []);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setFieldErrors({});
    setGeneralError(null);

    try {
      const body: CreatePatientRequest = {
        ...form,
        primaryBranchId: form.primaryBranchId || undefined,
      };
      await api.post("/api/v1/patients", body);
      router.push("/patients");
    } catch (err) {
      if (err instanceof ApiRequestError) {
        if (err.details) {
          setFieldErrors(err.details);
        } else {
          setGeneralError(err.message);
        }
      } else {
        setGeneralError("An unexpected error occurred");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const inputStyle = {
    width: "100%",
    padding: "0.5rem",
    border: "1px solid #ccc",
    borderRadius: "4px",
    boxSizing: "border-box" as const,
  };

  const labelStyle = {
    display: "block",
    marginBottom: "0.25rem",
    fontWeight: "bold" as const,
  };

  return (
    <div style={{ maxWidth: "480px" }}>
      <h1>New Patient</h1>

      {generalError && (
        <div
          style={{
            padding: "0.75rem",
            background: "#fdd",
            border: "1px solid #c00",
            borderRadius: "4px",
            marginBottom: "1rem",
          }}
        >
          {generalError}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: "1rem" }}>
          <label htmlFor="tenantId" style={labelStyle}>
            Tenant ID *
          </label>
          <input
            id="tenantId"
            type="text"
            value={form.tenantId}
            onChange={(e) => setForm({ ...form, tenantId: e.target.value })}
            style={inputStyle}
            placeholder="e.g. a0000000-0000-0000-0000-000000000001"
            required
          />
          {fieldErrors.tenantId?.map((msg, i) => (
            <small key={i} style={{ color: "red" }}>
              {msg}
            </small>
          ))}
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label htmlFor="firstName" style={labelStyle}>
            First Name *
          </label>
          <input
            id="firstName"
            type="text"
            value={form.firstName}
            onChange={(e) => setForm({ ...form, firstName: e.target.value })}
            style={inputStyle}
            required
          />
          {fieldErrors.firstName?.map((msg, i) => (
            <small key={i} style={{ color: "red" }}>
              {msg}
            </small>
          ))}
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label htmlFor="lastName" style={labelStyle}>
            Last Name *
          </label>
          <input
            id="lastName"
            type="text"
            value={form.lastName}
            onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            style={inputStyle}
            required
          />
          {fieldErrors.lastName?.map((msg, i) => (
            <small key={i} style={{ color: "red" }}>
              {msg}
            </small>
          ))}
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label htmlFor="phoneNumber" style={labelStyle}>
            Phone Number *
          </label>
          <input
            id="phoneNumber"
            type="tel"
            value={form.phoneNumber}
            onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
            style={inputStyle}
            required
          />
          {fieldErrors.phoneNumber?.map((msg, i) => (
            <small key={i} style={{ color: "red" }}>
              {msg}
            </small>
          ))}
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label htmlFor="branch" style={labelStyle}>
            Primary Branch
          </label>
          <select
            id="branch"
            value={form.primaryBranchId || ""}
            onChange={(e) =>
              setForm({ ...form, primaryBranchId: e.target.value || undefined })
            }
            style={inputStyle}
          >
            <option value="">None</option>
            {branches.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </select>
        </div>

        <div style={{ display: "flex", gap: "0.5rem" }}>
          <button
            type="submit"
            disabled={submitting}
            style={{
              padding: "0.5rem 1.5rem",
              background: "#0070f3",
              color: "white",
              border: "none",
              borderRadius: "4px",
              cursor: submitting ? "not-allowed" : "pointer",
            }}
          >
            {submitting ? "Creating..." : "Create Patient"}
          </button>
          <button
            type="button"
            onClick={() => router.push("/patients")}
            style={{
              padding: "0.5rem 1.5rem",
              background: "#eee",
              border: "none",
              borderRadius: "4px",
              cursor: "pointer",
            }}
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
