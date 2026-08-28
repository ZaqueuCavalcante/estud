# Endpoints ✅

## Admin

- ✅ GetDomainEventsController
- ✅ GetInstitutionsController

## Calendar

- ✅ CreateCalendarDayController
- ✅ DeleteCalendarDayController
- ✅ GetCalendarController
- ✅ UpdateCalendarDayController

## Campi

- ✅ CreateCampusController
- ✅ UpdateCampusController
- ✅ GetCampusOpeningHoursController
- ✅ UpdateCampusOpeningHoursController
- GetCampusOccupancyController (falta revisar os testes)
- GetCampiController (falta teste com ocupação não nula)

## Classes

- CreateClassController
- GetClassController
- GetClassesController
- ReleaseClassForEnrollmentController
- StartClassController
- UpdateClassSchedulesController
- UpdateClassTeachersController

## Classrooms

- CreateClassroomController
- GetClassroomController
- GetClassroomsController
- UpdateClassroomController

## CourseCurriculums

- CreateCourseCurriculumController
- GetCourseCurriculumController
- GetCourseCurriculumDetailsController
- GetCourseCurriculumsController
- UpdateCourseCurriculumController

## CourseOfferings

- CreateCourseOfferingController
- GetCourseOfferingDetailsController
- GetCourseOfferingsController

## Courses

- AddCourseDisciplinesController
- CreateCourseController
- GetCourseController
- GetCourseDetailsController
- GetCourseDisciplinesController
- GetCoursePotentialDisciplinesController
- GetCoursesController
- RemoveCourseDisciplineController
- UpdateCourseController

## Cross

- ✅ GetHomeStatsController
- ✅ HealthController
- ✅ HomeController
- ✅ VersionController

## Disciplines

- ✅ CreateDisciplineController
- ✅ UpdateDisciplineController
- ✅ GetDisciplinesController
- ✅ AssignCoursesToDisciplineController
- ✅ AssignTeachersToDisciplineController

- GetDisciplineController
- GetDisciplineDetailsController
- GetDisciplinePotentialCoursesController
- GetDisciplinePotentialTeachersController
- GetDisciplineTeachersController

## Identity

- CheckSocialLoginAvailabilityController
- CheckSsoAvailabilityController
- CreateRoleController
- CreateSsoConfigurationController
- EmailPasswordLoginController
- GetPermissionsController
- GetRoleController
- GetRolesController
- GetSsoConfigurationController
- GetTwoFactorEnforcementController
- GetTwoFactorKeyController
- GoogleOneTapLoginController
- LogoutController
- MagicLinkLoginController
- ResetPasswordController
- SendResetPasswordTokenController
- SetTwoFactorEnforcementController
- SetupTwoFactorController
- SocialLoginChallengeController
- TwoFactorLoginController
- TwoFactorSetupLoginController
- UpdateRoleController
- UpdateSsoConfigurationController

## Institutions

- ✅ GetInstitutionConfigController
- ✅ SetupInstitutionConfigController

## Notifications

- CreateNotificationController
- GetInstitutionNotificationController
- GetInstitutionNotificationsController
- GetNotificationsController
- GetUnreadNotificationsCountController
- MarkNotificationsAsViewedController

## Parents

- CreateParentController
- GetParentDetailsController
- GetParentStudentAgendaController
- GetParentStudentsController
- GetParentsController

## Periods

- CreateAcademicPeriodController
- CreateEnrollmentPeriodController
- GetAcademicPeriodsController
- GetEnrollmentPeriodsController
- UpdateEnrollmentPeriodController

## Students

- AssignStudentToClassController
- CreateClassActivityWorkController
- CreateStudentController
- EnrollStudentInCourseOfferingController
- GenerateEnrollmentProofController
- GetStudentController
- GetStudentAgendaController
- GetStudentAttendanceCalendarController
- GetStudentClassController
- GetStudentClassActivitiesController
- GetStudentClassActivityController
- GetStudentCourseDetailsController
- GetStudentDetailsController
- GetStudentsController
- ValidateEnrollmentProofController

## Teachers

- AssignCampiToTeacherController
- AssignDisciplinesToTeacherController
- CreateClassActivityController
- CreateLessonAttendanceController
- CreateTeacherController
- GetTeacherController
- GetTeacherAgendaController
- GetTeacherClassController
- GetTeacherClassActivitiesController
- GetTeacherClassActivityController
- GetTeacherClassLessonsController
- GetTeacherClassStudentsController
- GetTeacherCurrentClassesController
- GetTeacherDetailsController
- GetTeacherPotentialCampiController
- GetTeacherPotentialDisciplinesController
- GetTeachersController
- UpdateTeacherController

## Users

- ✅ GetAuthStatusController
- ✅ GetUserAccountController
- ✅ RegisterUserController
- ✅ UpdateUserAccountController

## Webhooks

- CreateWebhookSubscriptionController
- GetWebhookCallsController
- GetWebhookSubscriptionController
- GetWebhookSubscriptionsController
- UpdateWebhookSubscriptionController
