# Endpoints

## Admin
- ✅ GetDomainEventsController
- ✅ GetInstitutionsController

## Calendar
- ✅ GetCalendarController
- ✅ CreateCalendarDayController
- ✅ DeleteCalendarDayController
- ✅ UpdateCalendarDayController

## Classrooms
- ✅ GetClassroomController
- ✅ GetClassroomsController
- ✅ CreateClassroomController
- ✅ UpdateClassroomController

## Cross
- ✅ HomeController
- ✅ HealthController
- ✅ VersionController
- ✅ GetHomeStatsController

## Disciplines
- ✅ GetDisciplinesController
- ✅ CreateDisciplineController
- ✅ UpdateDisciplineController
- ✅ GetDisciplineDetailsController
- ✅ GetDisciplineTeachersController
- ✅ AssignCoursesToDisciplineController
- ✅ AssignTeachersToDisciplineController
- ✅ GetDisciplinePotentialCoursesController
- ✅ GetDisciplinePotentialTeachersController

## Users
- ✅ RegisterUserController
- ✅ GetAuthStatusController
- ✅ GetUserAccountController
- ✅ UpdateUserAccountController

## Institutions
- ✅ GetInstitutionConfigController
- ✅ SetupInstitutionConfigController

## CourseCurriculums
- ✅ CreateCourseCurriculumController
- ✅ GetCourseCurriculumDetailsController
- ✅ GetCourseCurriculumsController
- ✅ UpdateCourseCurriculumController

## Courses
- ✅ GetCoursesController
- ✅ CreateCourseController
- ✅ UpdateCourseController
- ✅ GetCourseDetailsController
- ✅ GetCourseDisciplinesController
- ✅ AssignDisciplinesToCourseController
- ✅ GetCoursePotentialDisciplinesController

## CourseOfferings
- ✅ CreateCourseOfferingController
- ✅ GetCourseOfferingDetailsController
- ✅ GetCourseOfferingsController

## Notifications
- ✅ GetNotificationsController
- ✅ CreateNotificationController
- ✅ MarkNotificationsAsViewedController
- ✅ GetInstitutionNotificationController
- ✅ GetInstitutionNotificationsController
- ✅ GetUnreadNotificationsCountController

## Periods
- ✅ CreateAcademicPeriodController
- ✅ CreateEnrollmentPeriodController
- ✅ GetAcademicPeriodsController
- ✅ GetEnrollmentPeriodsController
- ✅ UpdateEnrollmentPeriodController

## Webhooks
- ✅ GetWebhookCallsController
- ✅ GetWebhookSubscriptionController
- ✅ GetWebhookSubscriptionsController
- ✅ CreateWebhookSubscriptionController
- ✅ UpdateWebhookSubscriptionController

## Campi
- ✅ GetCampiController
- ✅ CreateCampusController
- ✅ UpdateCampusController
- ✅ GetCampusOccupancyController
- ✅ GetCampusOpeningHoursController
- ✅ UpdateCampusOpeningHoursController

---------------------------------------------------------------------------------------------------

## Classes
- ✅ StartClassController
- ✅ GetClassesController
- ✅ CreateClassController
- ✅ UpdateClassTeachersController
- ✅ UpdateClassSchedulesController
- ✅ ReleaseClassForEnrollmentController
- ❌ GetClassController (falta nota do aluno)

## Identity
- ✅ GetSsoConfigurationController
- ✅ CheckSsoAvailabilityController
- ✅ CreateSsoConfigurationController
- ✅ UpdateSsoConfigurationController
- ✅ VerifySsoDomainController
- ✅ GetRoleController
- ✅ GetRolesController
- ✅ CreateRoleController
- ✅ UpdateRoleController
- ✅ SetupTwoFactorController
- ✅ TwoFactorLoginController
- ✅ GetTwoFactorKeyController
- ✅ TwoFactorSetupLoginController
- ✅ GetTwoFactorEnforcementController
- ✅ SetTwoFactorEnforcementController
- ✅ ResetPasswordController
- ✅ EmailPasswordLoginController
- ✅ SendResetPasswordTokenController
- ✅ LogoutController
- ✅ GetPermissionsController
- ✅ MagicLinkLoginController
- ✅ CheckSocialLoginAvailabilityController
- ❌ GoogleOneTapLoginController
- ❌ SocialLoginChallengeController

## Students
- ✅ GetStudentController
- ✅ GetStudentsController
- ✅ CreateStudentController
- ✅ AssignStudentToClassController
- ✅ EnrollStudentInCourseOfferingController
- ✅ GenerateEnrollmentProofController
- ✅ ValidateEnrollmentProofController
- ✅ GetStudentClassController
- ✅ GetStudentCourseDetailsController
- ✅ GetStudentClassActivitiesController
- ✅ GetStudentClassActivityController
- ✅ CreateClassActivityWorkController
- ✅ GetStudentAgendaController
- ✅ GetStudentAttendanceCalendarController
- ✅ GetStudentCurrentClassesController
- ❌ GetStudentDetailsController (falta nota)

## Teachers
- ✅ GetTeachersController
- ✅ CreateTeacherController
- ✅ UpdateTeacherController
- ✅ GetTeacherDetailsController
- ✅ AssignCampiToTeacherController
- ✅ AssignDisciplinesToTeacherController
- ✅ CreateClassActivityController
- ✅ GetTeacherClassActivitiesController
- ✅ GetTeacherClassActivityController
- ✅ AddActivityNoteController
- ✅ CreateLessonAttendanceController
- ✅ GetTeacherClassLessonsController
- ✅ GetTeacherClassController
- ✅ GetTeacherCurrentClassesController
- ✅ GetTeacherPotentialCampiController
- ✅ GetTeacherPotentialDisciplinesController
- ✅ GetTeacherAgendaController
- ❌ GetTeacherClassStudentsController (falta nota)

## Parents
- ✅ GetParentsController
- ✅ CreateParentController
- ✅ GetParentDetailsController
- ✅ GetParentStudentsController
- ✅ GetParentStudentAgendaController
