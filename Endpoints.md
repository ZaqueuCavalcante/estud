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

## Parents

- ✅ GetParentsController
- ✅ CreateParentController
- ✅ GetParentDetailsController
- ✅ GetParentStudentsController
- GetParentStudentAgendaController (revisar endpoint, testes e front)





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

## Webhooks

- CreateWebhookSubscriptionController
- GetWebhookCallsController
- GetWebhookSubscriptionController
- GetWebhookSubscriptionsController
- UpdateWebhookSubscriptionController
