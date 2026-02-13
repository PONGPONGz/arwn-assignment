export interface PatientResponse {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranchId: string | null;
  primaryBranchName: string | null;
  createdAt: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranchId?: string;
}
