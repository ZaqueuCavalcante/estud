# Endpoints ✅

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


---------------------------------------------------------------------------------------------------


## Campi
- ✅ CreateCampusController
- ✅ UpdateCampusController
- ✅ GetCampusOpeningHoursController
- ✅ UpdateCampusOpeningHoursController
- GetCampusOccupancyController (falta revisar os testes)
- GetCampiController (falta teste com ocupação não nula)

## Classes
- GetClassController
- GetClassesController
- StartClassController
- ✅ CreateClassController
- ✅ UpdateClassTeachersController
- ✅ UpdateClassSchedulesController
- ✅ ReleaseClassForEnrollmentController

## Identity
- ✅ GetSsoConfigurationController
- ✅ CheckSsoAvailabilityController
- ✅ CreateSsoConfigurationController
- ✅ UpdateSsoConfigurationController
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

- GoogleOneTapLoginController
- SocialLoginChallengeController

## Students
- ✅ GetStudentController
- ✅ GetStudentsController
- ✅ CreateStudentController
- ✅ AssignStudentToClassController
- ✅ EnrollStudentInCourseOfferingController

- CreateClassActivityWorkController
- GenerateEnrollmentProofController
- GetStudentAgendaController
- GetStudentAttendanceCalendarController
- GetStudentClassController
- GetStudentClassActivitiesController
- GetStudentClassActivityController
- GetStudentCourseDetailsController
- GetStudentDetailsController
- ValidateEnrollmentProofController

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
- CreateLessonAttendanceController

- GetTeacherAgendaController
- GetTeacherClassController
- GetTeacherClassLessonsController
- GetTeacherClassStudentsController

- ✅ GetTeacherCurrentClassesController
- ✅ GetTeacherPotentialCampiController
- ✅ GetTeacherPotentialDisciplinesController

## Parents
- ✅ GetParentsController
- ✅ CreateParentController
- ✅ GetParentDetailsController
- ✅ GetParentStudentsController
- GetParentStudentAgendaController (revisar endpoint, testes e front)
