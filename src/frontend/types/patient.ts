export interface PatientResponse {
  id: string;
  tenantId: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranchId: string | null;
  primaryBranchName: string | null;
  createdAt: string;
}

export interface CreatePatientRequest {
  tenantId: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranchId?: string;
}
