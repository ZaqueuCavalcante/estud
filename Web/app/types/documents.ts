// Contrato do endpoint GET /students/enrollment-proofs
// (Back/Features/Students/GetEnrollmentProofs).
export interface EnrollmentProofItem {
  code: string
  issuedAt: string // ISO em UTC, ex: '2026-02-10T13:30:00Z'
}

export interface GetEnrollmentProofsOut {
  total: number
  items: EnrollmentProofItem[]
}

// Contrato do endpoint público POST /students/enrollment-proofs/{code}/validate
// (Back/Features/Students/ValidateEnrollmentProof).
export interface ValidateEnrollmentProofOut {
  code: string
  institution: string
  studentName: string
  enrollmentCode: string
  course: string
  campus: string
  period: string
  session: string // 'Morning' | 'Afternoon' | 'Evening'
  issuedAt: string
}
