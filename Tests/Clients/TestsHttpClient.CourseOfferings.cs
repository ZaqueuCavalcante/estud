using System.Net.Http.Json;
using Estud.Back.Features.CourseOfferings.GetCourseOfferings;
using Estud.Back.Features.CourseOfferings.CreateCourseOffering;
using Estud.Back.Features.CourseOfferings.GetCourseOfferingDetails;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateCourseOfferingOut, ErrorOut>> CreateCourseOffering(
        int campusId,
        int courseId,
        int courseCurriculumId,
        int academicPeriodId,
        CourseSession session = CourseSession.Evening
    ) {
        var data = new CreateCourseOfferingIn
        {
            CampusId = campusId,
            CourseId = courseId,
            CourseCurriculumId = courseCurriculumId,
            AcademicPeriodId = academicPeriodId,
            CourseSession = session,
        };
        var response = await http.PostAsJsonAsync("/course-offerings", data);
        return await response.Resolve<CreateCourseOfferingOut>();
    }

    public async Task<OneOf<GetCourseOfferingDetailsOut, ErrorOut>> GetCourseOfferingDetails(int courseOfferingId)
    {
        var response = await http.GetAsync($"/course-offerings/{courseOfferingId}/details");
        return await response.Resolve<GetCourseOfferingDetailsOut>();
    }

    public async Task<OneOf<GetCourseOfferingsOut, ErrorOut>> GetCourseOfferings(
        int? page = null,
        int? pageSize = null
    ) {
        var data = new GetCourseOfferingsIn
        {
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
        };

        var response = await http.GetAsync("/course-offerings".AddQueryString(data));
        return await response.Resolve<GetCourseOfferingsOut>();
    }
}
