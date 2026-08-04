namespace Estud.Back.Domain.Students;

public class EnrollmentProofMetadata
{
    public string StudentName { get; set; }
    public string StudentEnrollmentCode { get; set; }
    public string Institution { get; set; }
    public string Course { get; set; }
    public string Campus { get; set; }
    public string Period { get; set; }
    public CourseSession Session { get; set; }

    public EnrollmentProofMetadata() {}

    public EnrollmentProofMetadata(
        string studentName,
        string studentEnrollmentCode,
        string institution,
        string course,
        string campus,
        string period,
        CourseSession session
    ) {
        StudentName = studentName;
        StudentEnrollmentCode = studentEnrollmentCode;
        Institution = institution;
        Course = course;
        Campus = campus;
        Period = period;
        Session = session;
    }
}
