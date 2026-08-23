namespace Estud.Back.Features.CourseOfferings.GetCourseOfferingDetails;

public class GetCourseOfferingDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetCourseOfferingDetailsOut, EstudError>> Get(int courseOfferingId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var offering = await ctx.CourseOfferings.AsNoTracking()
            .Where(o => o.InstitutionId == institutionId && o.Id == courseOfferingId)
            .Select(o => new
            {
                o.Id,
                o.CampusId,
                Campus = o.Campus!.Name,
                o.CourseId,
                Course = o.Course!.Name,
                o.Course.CourseType,
                o.CourseCurriculumId,
                Curriculum = o.CourseCurriculum!.Name,
                Disciplines = o.CourseCurriculum.Links.Count,
                Period = o.AcademicPeriod!.Name,
                o.AcademicPeriod.StartAt,
                o.AcademicPeriod.EndAt,
                o.Session,
            })
            .FirstOrDefaultAsync();
        if (offering == null) return CourseOfferingNotFound.I;

        var students = await ctx.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.CourseOfferingId == courseOfferingId && e.LeftAt == null)
            .OrderBy(e => e.Student!.Name)
            .Select(e => new GetCourseOfferingDetailsStudentOut
            {
                Id = e.StudentId,
                Name = e.Student!.Name,
                EnrollmentCode = e.Student.EnrollmentCode,
                Status = e.Student.Status,
                EnrolledAt = e.EnrolledAt,
            })
            .ToListAsync();

        return new GetCourseOfferingDetailsOut
        {
            Id = offering.Id,
            CampusId = offering.CampusId,
            Campus = offering.Campus,
            CourseId = offering.CourseId,
            Course = offering.Course,
            CourseType = offering.CourseType.GetDescription(),
            CourseCurriculumId = offering.CourseCurriculumId,
            Curriculum = offering.Curriculum,
            Period = offering.Period,
            PeriodStartAt = offering.StartAt,
            PeriodEndAt = offering.EndAt,
            Session = offering.Session,
            Disciplines = offering.Disciplines,
            Students = students,
        };
    }
}
