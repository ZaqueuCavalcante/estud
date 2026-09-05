// Gerado por scripts/gen-db-schema.py a partir dos IEntityTypeConfiguration do backend.
// Não editar à mão: rode `python3 scripts/gen-db-schema.py <json> <ts>` para atualizar.

export interface DbColumn {
  name: string
  prop: string
  type: string
  clr: string
  nullable: boolean
  enum?: string
  default?: string
  converted?: boolean
}

export interface DbForeignKey {
  columns: string[]
  target: string | null
  targetEntity: string | null
  principal: string[]
  nav: string | null
  convention: boolean
}

export interface DbIndex {
  columns: string[]
  unique: boolean
}

export interface DbTable {
  table: string
  entity: string
  file: string
  pk: string[]
  columns: DbColumn[]
  fks: DbForeignKey[]
  indexes: DbIndex[]
}

export const dbSchema: DbTable[] = [
  {
    table: "academic_periods",
    entity: "AcademicPeriod",
    file: "Back/Database/Periods/AcademicPeriodDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "start_at", prop: "StartAt", type: "date", clr: "DateOnly", nullable: false },
      { name: "end_at", prop: "EndAt", type: "date", clr: "DateOnly", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "admin_users",
    entity: "AdminUser",
    file: "Back/Database/Admin/AdminUserDbConfig.cs",
    pk: ["user_id"],
    columns: [
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "audit_trails",
    entity: "AuditTrail",
    file: "Back/Audit/AuditTrailDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "activity_id", prop: "ActivityId", type: "text", clr: "string", nullable: false },
      { name: "operation", prop: "Operation", type: "text", clr: "string", nullable: false },
      { name: "entity_id", prop: "EntityId", type: "text", clr: "string", nullable: false },
      { name: "entity_type", prop: "EntityType", type: "text", clr: "string", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "action", prop: "Action", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "data", prop: "Data", type: "jsonb", clr: "JsonDocument", nullable: false },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "calendar_days",
    entity: "CalendarDay",
    file: "Back/Database/Calendar/CalendarDayDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int?", nullable: true },
      { name: "date", prop: "Date", type: "date", clr: "DateOnly", nullable: false },
      { name: "day_type", prop: "DayType", type: "integer", clr: "DayType", nullable: false, enum: "DayType" },
      { name: "description", prop: "Description", type: "text", clr: "string?", nullable: true },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: null, convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["institution_id", "campus_id", "date"], unique: true },
    ],
  },
  {
    table: "campi",
    entity: "Campus",
    file: "Back/Database/Campi/CampusDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "state", prop: "State", type: "integer", clr: "BrazilState", nullable: false, enum: "BrazilState" },
      { name: "city", prop: "City", type: "text", clr: "string", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "class_activities",
    entity: "ClassActivity",
    file: "Back/Database/Classes/ClassActivityDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "uid", prop: "Uid", type: "text", clr: "string", nullable: false },
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "note", prop: "Note", type: "integer", clr: "ClassNoteType", nullable: false, enum: "ClassNoteType" },
      { name: "title", prop: "Title", type: "text", clr: "string", nullable: false },
      { name: "description", prop: "Description", type: "text", clr: "string", nullable: false },
      { name: "activity_type", prop: "ActivityType", type: "integer", clr: "ClassActivityType", nullable: false, enum: "ClassActivityType" },
      { name: "status", prop: "Status", type: "integer", clr: "ClassActivityStatus", nullable: false, enum: "ClassActivityStatus" },
      { name: "weight", prop: "Weight", type: "integer", clr: "int", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "due_date", prop: "DueDate", type: "date", clr: "DateOnly", nullable: false },
      { name: "due_hour", prop: "DueHour", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
    ],
    fks: [
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "class_activity_works",
    entity: "ClassActivityWork",
    file: "Back/Database/Classes/ClassActivityWorkDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_activity_id", prop: "ClassActivityId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "link", prop: "Link", type: "text", clr: "string?", nullable: true },
      { name: "note", prop: "Note", type: "numeric", clr: "decimal", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "ClassActivityWorkStatus", nullable: false, enum: "ClassActivityWorkStatus" },
    ],
    fks: [
      { columns: ["class_activity_id"], target: "class_activities", targetEntity: "ClassActivity", principal: [], nav: null, convention: false },
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: ["id"], nav: "Student", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "class_lesson_attendances",
    entity: "ClassLessonAttendance",
    file: "Back/Database/Classes/ClassLessonAttendanceDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "lesson_id", prop: "LessonId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "present", prop: "Present", type: "boolean", clr: "bool", nullable: false },
    ],
    fks: [
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: [], nav: null, convention: false },
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: [], nav: null, convention: false },
      { columns: ["lesson_id"], target: "class_lessons", targetEntity: "ClassLesson", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["lesson_id", "student_id"], unique: true },
    ],
  },
  {
    table: "class_lessons",
    entity: "ClassLesson",
    file: "Back/Database/Classes/ClassLessonDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "number", prop: "Number", type: "integer", clr: "int", nullable: false },
      { name: "date", prop: "Date", type: "date", clr: "DateOnly", nullable: false },
      { name: "start_at", prop: "StartAt", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
      { name: "end_at", prop: "EndAt", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
      { name: "status", prop: "Status", type: "integer", clr: "ClassLessonStatus", nullable: false, enum: "ClassLessonStatus" },
    ],
    fks: [
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "classes",
    entity: "Class",
    file: "Back/Database/Classes/ClassDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "discipline_id", prop: "DisciplineId", type: "integer", clr: "int", nullable: false },
      { name: "period_id", prop: "PeriodId", type: "integer", clr: "int", nullable: false },
      { name: "vacancies", prop: "Vacancies", type: "integer", clr: "int", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "ClassStatus", nullable: false, enum: "ClassStatus" },
      { name: "workload", prop: "Workload", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int?", nullable: true },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: "Campus", convention: false },
      { columns: ["discipline_id"], target: "disciplines", targetEntity: "Discipline", principal: [], nav: "Discipline", convention: false },
      { columns: ["period_id", "institution_id"], target: "academic_periods", targetEntity: "AcademicPeriod", principal: ["id", "institution_id"], nav: "Period", convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "classes__students",
    entity: "ClassStudent",
    file: "Back/Database/Classes/ClassStudentDbConfig.cs",
    pk: ["class_id", "student_id"],
    columns: [
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "StudentClassStatus", nullable: false, enum: "StudentClassStatus" },
    ],
    fks: [
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: ["id"], nav: "Class", convention: false },
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: ["id"], nav: "Student", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "classes__teachers",
    entity: "ClassTeacher",
    file: "Back/Database/Classes/ClassTeacherDbConfig.cs",
    pk: ["class_id", "teacher_id"],
    columns: [
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "teacher_id", prop: "TeacherId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["teacher_id"], target: "teachers", targetEntity: "EstudTeacher", principal: [], nav: null, convention: false },
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "classrooms",
    entity: "Classroom",
    file: "Back/Database/Classrooms/ClassroomDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "capacity", prop: "Capacity", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: "Campus", convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "classrooms__classes",
    entity: "ClassroomClass",
    file: "Back/Database/Classrooms/ClassroomClassDbConfig.cs",
    pk: ["classroom_id", "class_id"],
    columns: [
      { name: "classroom_id", prop: "ClassroomId", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "is_active", prop: "IsActive", type: "boolean", clr: "bool", nullable: false },
    ],
    fks: [
      { columns: ["classroom_id"], target: "classrooms", targetEntity: "Classroom", principal: ["id"], nav: null, convention: false },
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "command_batches",
    entity: "CommandBatch",
    file: "Back/Database/Commands/CommandBatchDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "type", prop: "Type", type: "integer", clr: "CommandBatchType", nullable: false, enum: "CommandBatchType" },
      { name: "status", prop: "Status", type: "integer", clr: "CommandBatchStatus", nullable: false, enum: "CommandBatchStatus" },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "processed_at", prop: "ProcessedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
      { name: "source_command_id", prop: "SourceCommandId", type: "integer", clr: "int?", nullable: true },
      { name: "next_command_id", prop: "NextCommandId", type: "integer", clr: "int?", nullable: true },
      { name: "size", prop: "Size", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "commands",
    entity: "Command",
    file: "Back/Database/Commands/CommandDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "type", prop: "Type", type: "text", clr: "string", nullable: false },
      { name: "data", prop: "Data", type: "text", clr: "string", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "CommandStatus", nullable: false, enum: "CommandStatus" },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "duration", prop: "Duration", type: "integer", clr: "int", nullable: false },
      { name: "processed_at", prop: "ProcessedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
      { name: "processor_id", prop: "ProcessorId", type: "uuid", clr: "Guid?", nullable: true },
      { name: "error", prop: "Error", type: "text", clr: "string?", nullable: true },
      { name: "parent_id", prop: "ParentId", type: "integer", clr: "int?", nullable: true },
      { name: "original_id", prop: "OriginalId", type: "integer", clr: "int?", nullable: true },
      { name: "batch_id", prop: "BatchId", type: "integer", clr: "int?", nullable: true },
      { name: "not_before", prop: "NotBefore", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
      { name: "activity_id", prop: "ActivityId", type: "text", clr: "string?", nullable: true },
      { name: "max_retries", prop: "MaxRetries", type: "integer", clr: "int", nullable: false },
      { name: "retry_attempt", prop: "RetryAttempt", type: "integer", clr: "int", nullable: false },
      { name: "backoff_strategy", prop: "BackoffStrategy", type: "integer", clr: "BackoffStrategy", nullable: false, enum: "BackoffStrategy" },
      { name: "base_delay_seconds", prop: "BaseDelaySeconds", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "course_curriculums",
    entity: "CourseCurriculum",
    file: "Back/Database/CourseCurriculums/CourseCurriculumDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "course_id", prop: "CourseId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
    ],
    fks: [
      { columns: ["course_id"], target: "courses", targetEntity: "Course", principal: [], nav: null, convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "course_curriculums_disciplines",
    entity: "CourseCurriculumDiscipline",
    file: "Back/Database/CourseCurriculums/CourseCurriculumDisciplineDbConfig.cs",
    pk: ["course_curriculum_id", "discipline_id"],
    columns: [
      { name: "course_curriculum_id", prop: "CourseCurriculumId", type: "integer", clr: "int", nullable: false },
      { name: "discipline_id", prop: "DisciplineId", type: "integer", clr: "int", nullable: false },
      { name: "period", prop: "Period", type: "smallint", clr: "byte", nullable: false },
      { name: "credits", prop: "Credits", type: "smallint", clr: "byte", nullable: false },
    ],
    fks: [
      { columns: ["discipline_id"], target: "disciplines", targetEntity: "Discipline", principal: [], nav: "Discipline", convention: false },
      { columns: ["course_curriculum_id"], target: "course_curriculums", targetEntity: "CourseCurriculum", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "course_offerings",
    entity: "CourseOffering",
    file: "Back/Database/CourseOfferings/CourseOfferingDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int", nullable: false },
      { name: "course_id", prop: "CourseId", type: "integer", clr: "int", nullable: false },
      { name: "course_curriculum_id", prop: "CourseCurriculumId", type: "integer", clr: "int", nullable: false },
      { name: "academic_period_id", prop: "AcademicPeriodId", type: "integer", clr: "int", nullable: false },
      { name: "session", prop: "Session", type: "integer", clr: "CourseSession", nullable: false, enum: "CourseSession" },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: "Campus", convention: false },
      { columns: ["course_id"], target: "courses", targetEntity: "Course", principal: [], nav: "Course", convention: false },
      { columns: ["course_curriculum_id"], target: "course_curriculums", targetEntity: "CourseCurriculum", principal: [], nav: "CourseCurriculum", convention: false },
      { columns: ["academic_period_id"], target: "academic_periods", targetEntity: "AcademicPeriod", principal: [], nav: "AcademicPeriod", convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "courses",
    entity: "Course",
    file: "Back/Database/Courses/CourseDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "course_type", prop: "CourseType", type: "integer", clr: "CourseType", nullable: false, enum: "CourseType" },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "courses_disciplines",
    entity: "CourseDiscipline",
    file: "Back/Database/Courses/CourseDisciplineDbConfig.cs",
    pk: ["course_id", "discipline_id"],
    columns: [
      { name: "course_id", prop: "CourseId", type: "integer", clr: "int", nullable: false },
      { name: "discipline_id", prop: "DisciplineId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["discipline_id"], target: "disciplines", targetEntity: "Discipline", principal: [], nav: null, convention: false },
      { columns: ["course_id"], target: "courses", targetEntity: "Course", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "data_protection_keys",
    entity: "DataProtectionKey",
    file: "Back/Database/Identity/DataProtectionKeyDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "friendly_name", prop: "FriendlyName", type: "text", clr: "string?", nullable: true },
      { name: "xml", prop: "Xml", type: "text", clr: "string?", nullable: true },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "disciplines",
    entity: "Discipline",
    file: "Back/Database/Disciplines/DisciplineDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "code", prop: "Code", type: "text", clr: "string", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["code"], unique: true },
    ],
  },
  {
    table: "domain_events",
    entity: "DomainEvent",
    file: "Back/Database/DomainEvents/DomainEventDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "entity_uid", prop: "EntityUid", type: "varchar(26)", clr: "string", nullable: false },
      { name: "type", prop: "Type", type: "text", clr: "string", nullable: false },
      { name: "data", prop: "Data", type: "text", clr: "string", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "DomainEventStatus", nullable: false, enum: "DomainEventStatus" },
      { name: "occurred_at", prop: "OccurredAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "processed_at", prop: "ProcessedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
      { name: "processor_id", prop: "ProcessorId", type: "uuid", clr: "Guid?", nullable: true },
      { name: "error", prop: "Error", type: "text", clr: "string?", nullable: true },
      { name: "duration", prop: "Duration", type: "integer", clr: "int", nullable: false },
      { name: "activity_id", prop: "ActivityId", type: "text", clr: "string?", nullable: true },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "enrollment_periods",
    entity: "EnrollmentPeriod",
    file: "Back/Database/Periods/EnrollmentPeriodDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "start_at", prop: "StartAt", type: "date", clr: "DateOnly", nullable: false },
      { name: "end_at", prop: "EndAt", type: "date", clr: "DateOnly", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "enrollment_proofs",
    entity: "EnrollmentProof",
    file: "Back/Database/Students/EnrollmentProofDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "code", prop: "Code", type: "text", clr: "string", nullable: false },
      { name: "issued_at", prop: "IssuedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "metadata", prop: "Metadata", type: "jsonb", clr: "EnrollmentProofMetadata", nullable: false, converted: true },
    ],
    fks: [
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["code"], unique: true },
    ],
  },
  {
    table: "institution_configs",
    entity: "InstitutionConfig",
    file: "Back/Database/Institutions/InstitutionConfigDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "note_limit", prop: "NoteLimit", type: "numeric(4,2)", clr: "decimal", nullable: false },
      { name: "frequency_limit", prop: "FrequencyLimit", type: "numeric(5,2)", clr: "decimal", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["institution_id"], unique: true },
    ],
  },
  {
    table: "institutions",
    entity: "Institution",
    file: "Back/Database/Institutions/InstitutionDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "magic_links",
    entity: "MagicLink",
    file: "Back/Database/Identity/MagicLinkDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "uuid", clr: "Guid", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "expires_at", prop: "ExpiresAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "used_at", prop: "UsedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: "User", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "notifications",
    entity: "Notification",
    file: "Back/Database/Notifications/NotificationDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "notification_type", prop: "NotificationType", type: "integer", clr: "NotificationType", nullable: false, enum: "NotificationType" },
      { name: "title", prop: "Title", type: "text", clr: "string", nullable: false },
      { name: "description", prop: "Description", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "metadata", prop: "Metadata", type: "jsonb", clr: "JsonDocument?", nullable: true },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "opening_hours",
    entity: "OpeningHour",
    file: "Back/Database/Campi/OpeningHourDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int", nullable: false },
      { name: "day", prop: "Day", type: "integer", clr: "Day", nullable: false, enum: "Day" },
      { name: "start", prop: "Start", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
      { name: "end", prop: "End", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: "Campus", convention: false },
    ],
    indexes: [
      { columns: ["campus_id", "day"], unique: false },
    ],
  },
  {
    table: "parent_students",
    entity: "ParentStudent",
    file: "Back/Database/Parents/ParentStudentDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "parent_id", prop: "ParentId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "relationship", prop: "Relationship", type: "integer", clr: "ParentRelationship", nullable: false, enum: "ParentRelationship" },
      { name: "status", prop: "Status", type: "integer", clr: "ParentStudentStatus", nullable: false, enum: "ParentStudentStatus" },
      { name: "revoked_by_student", prop: "RevokedByStudent", type: "boolean", clr: "bool", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["parent_id"], target: "parents", targetEntity: "EstudParent", principal: [], nav: "Parent", convention: false },
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: [], nav: "Student", convention: false },
    ],
    indexes: [
      { columns: ["parent_id", "student_id"], unique: true },
    ],
  },
  {
    table: "parents",
    entity: "EstudParent",
    file: "Back/Database/Parents/EstudParentDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
    ],
    fks: [
      { columns: ["institution_id", "user_id"], target: "users", targetEntity: "EstudUser", principal: ["institution_id", "id"], nav: "User", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "received_webhook_events",
    entity: "ReceivedWebhookEvent",
    file: "Back/Database/Webhooks/ReceivedWebhookEventDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "external_id", prop: "ExternalId", type: "text", clr: "string", nullable: false },
      { name: "source", prop: "Source", type: "integer", clr: "ReceivedWebhookEventSource", nullable: false, enum: "ReceivedWebhookEventSource" },
      { name: "type", prop: "Type", type: "text", clr: "string", nullable: false },
      { name: "payload", prop: "Payload", type: "text", clr: "string", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "ReceivedWebhookEventStatus", nullable: false, enum: "ReceivedWebhookEventStatus" },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "processed_at", prop: "ProcessedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
      { name: "processor_id", prop: "ProcessorId", type: "uuid", clr: "Guid?", nullable: true },
      { name: "error", prop: "Error", type: "text", clr: "string?", nullable: true },
      { name: "command_id", prop: "CommandId", type: "integer", clr: "int?", nullable: true },
    ],
    fks: [
      { columns: ["command_id"], target: "commands", targetEntity: "Command", principal: ["id"], nav: "Command", convention: false },
    ],
    indexes: [
      { columns: ["external_id", "source"], unique: true },
    ],
  },
  {
    table: "reset_password_tokens",
    entity: "ResetPasswordToken",
    file: "Back/Database/Identity/ResetPasswordTokenDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "uuid", clr: "Guid", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "token", prop: "Token", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "expires_at", prop: "ExpiresAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "used_at", prop: "UsedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: null, convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["token"], unique: true },
    ],
  },
  {
    table: "role_claims",
    entity: "EstudRoleClaim",
    file: "Back/Database/Identity/EstudRoleClaimDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "role_id", prop: "RoleId", type: "integer", clr: "int", nullable: false },
      { name: "claim_type", prop: "ClaimType", type: "text", clr: "string?", nullable: true },
      { name: "claim_value", prop: "ClaimValue", type: "text", clr: "string?", nullable: true },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "roles",
    entity: "EstudRole",
    file: "Back/Database/Identity/EstudRoleDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string?", nullable: true },
      { name: "normalized_name", prop: "NormalizedName", type: "text", clr: "string?", nullable: true },
      { name: "concurrency_stamp", prop: "ConcurrencyStamp", type: "text", clr: "string?", nullable: true },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "description", prop: "Description", type: "text", clr: "string", nullable: false },
      { name: "base_type", prop: "BaseType", type: "integer", clr: "UserType", nullable: false, enum: "UserType" },
      { name: "permissions", prop: "Permissions", type: "integer[]", clr: "List<int>", nullable: false },
      { name: "two_factor_required", prop: "TwoFactorRequired", type: "boolean", clr: "bool", nullable: false, default: "false" },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["normalized_name"], unique: true },
      { columns: ["institution_id", "normalized_name"], unique: true },
    ],
  },
  {
    table: "schedules",
    entity: "Schedule",
    file: "Back/Database/Classes/ScheduleDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int?", nullable: true },
      { name: "teacher_id", prop: "TeacherId", type: "integer", clr: "int?", nullable: true },
      { name: "classroom_id", prop: "ClassroomId", type: "integer", clr: "int?", nullable: true },
      { name: "day", prop: "Day", type: "integer", clr: "Day", nullable: false, enum: "Day" },
      { name: "start", prop: "Start", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
      { name: "end", prop: "End", type: "integer", clr: "Hour", nullable: false, enum: "Hour" },
    ],
    fks: [
      { columns: ["class_id"], target: "classes", targetEntity: "Class", principal: [], nav: null, convention: false },
      { columns: ["teacher_id"], target: "teachers", targetEntity: "EstudTeacher", principal: [], nav: null, convention: false },
      { columns: ["classroom_id"], target: "classrooms", targetEntity: "Classroom", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "sso_allowed_domains",
    entity: "SsoAllowedDomain",
    file: "Back/Database/Identity/SsoAllowedDomainDbConfig.cs",
    pk: ["domain"],
    columns: [
      { name: "domain", prop: "Domain", type: "text", clr: "string", nullable: false },
      { name: "sso_configuration_id", prop: "SsoConfigurationId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["sso_configuration_id"], target: "sso_configurations", targetEntity: "SsoConfiguration", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "sso_configurations",
    entity: "SsoConfiguration",
    file: "Back/Database/Identity/SsoConfigurationDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "public_id", prop: "PublicId", type: "uuid", clr: "Guid", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "provider_type", prop: "ProviderType", type: "integer", clr: "SsoProviderType", nullable: false, enum: "SsoProviderType" },
      { name: "authority", prop: "Authority", type: "text", clr: "string", nullable: false },
      { name: "client_id", prop: "ClientId", type: "text", clr: "string", nullable: false },
      { name: "client_secret", prop: "ClientSecret", type: "text", clr: "string", nullable: false },
      { name: "is_active", prop: "IsActive", type: "boolean", clr: "bool", nullable: false },
      { name: "require_sso", prop: "RequireSso", type: "boolean", clr: "bool", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "updated_at", prop: "UpdatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["public_id"], unique: true },
    ],
  },
  {
    table: "student_class_notes",
    entity: "StudentClassNote",
    file: "Back/Database/Students/StudentClassNoteDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "class_id", prop: "ClassId", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "type", prop: "Type", type: "integer", clr: "ClassNoteType", nullable: false, enum: "ClassNoteType" },
      { name: "note", prop: "Note", type: "numeric(4,2)", clr: "decimal", nullable: false },
    ],
    fks: [
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
      { columns: ["class_id", "student_id", "type"], unique: true },
    ],
  },
  {
    table: "student_course_enrollments",
    entity: "StudentCourseEnrollment",
    file: "Back/Database/Students/StudentCourseEnrollmentDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "student_id", prop: "StudentId", type: "integer", clr: "int", nullable: false },
      { name: "course_offering_id", prop: "CourseOfferingId", type: "integer", clr: "int", nullable: false },
      { name: "enrolled_at", prop: "EnrolledAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "left_at", prop: "LeftAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
    ],
    fks: [
      { columns: ["student_id"], target: "students", targetEntity: "EstudStudent", principal: [], nav: "Student", convention: false },
      { columns: ["course_offering_id"], target: "course_offerings", targetEntity: "CourseOffering", principal: [], nav: "CourseOffering", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "students",
    entity: "EstudStudent",
    file: "Back/Database/Students/EstudStudentDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "uid", prop: "Uid", type: "text", clr: "string", nullable: false },
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "enrollment_code", prop: "EnrollmentCode", type: "text", clr: "string", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "StudentStatus", nullable: false, enum: "StudentStatus" },
      { name: "yield_coefficient", prop: "YieldCoefficient", type: "numeric(4,2)", clr: "decimal", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
      { columns: ["institution_id", "user_id"], target: "users", targetEntity: "EstudUser", principal: ["institution_id", "id"], nav: "User", convention: false },
    ],
    indexes: [
      { columns: ["enrollment_code"], unique: true },
    ],
  },
  {
    table: "teachers",
    entity: "EstudTeacher",
    file: "Back/Database/Teachers/EstudTeacherDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
      { columns: ["institution_id", "user_id"], target: "users", targetEntity: "EstudUser", principal: ["institution_id", "id"], nav: "User", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "teachers_campi",
    entity: "TeacherCampus",
    file: "Back/Database/Teachers/TeacherCampusDbConfig.cs",
    pk: ["teacher_id", "campus_id"],
    columns: [
      { name: "teacher_id", prop: "TeacherId", type: "integer", clr: "int", nullable: false },
      { name: "campus_id", prop: "CampusId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["campus_id"], target: "campi", targetEntity: "Campus", principal: [], nav: null, convention: false },
      { columns: ["teacher_id"], target: "teachers", targetEntity: "EstudTeacher", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "teachers_disciplines",
    entity: "TeacherDiscipline",
    file: "Back/Database/Teachers/TeacherDisciplineDbConfig.cs",
    pk: ["teacher_id", "discipline_id"],
    columns: [
      { name: "teacher_id", prop: "TeacherId", type: "integer", clr: "int", nullable: false },
      { name: "discipline_id", prop: "DisciplineId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["discipline_id"], target: "disciplines", targetEntity: "Discipline", principal: [], nav: null, convention: false },
      { columns: ["teacher_id"], target: "teachers", targetEntity: "EstudTeacher", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "user_activities",
    entity: "UserActivity",
    file: "Back/Database/Activities/UserActivityDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int?", nullable: true },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int?", nullable: true },
      { name: "feature_group", prop: "FeatureGroup", type: "integer", clr: "FeatureGroup", nullable: false, enum: "FeatureGroup" },
      { name: "severity", prop: "Severity", type: "integer", clr: "UserActivitySeverity", nullable: false, enum: "UserActivitySeverity" },
      { name: "activity_type", prop: "ActivityType", type: "integer", clr: "UserActivityType", nullable: false, enum: "UserActivityType" },
      { name: "metadata", prop: "Metadata", type: "jsonb", clr: "JsonDocument?", nullable: true },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: null, convention: false },
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "user_claims",
    entity: "EstudUserClaim",
    file: "Back/Database/Identity/EstudUserClaimDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "claim_type", prop: "ClaimType", type: "text", clr: "string?", nullable: true },
      { name: "claim_value", prop: "ClaimValue", type: "text", clr: "string?", nullable: true },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "user_logins",
    entity: "EstudUserLogin",
    file: "Back/Database/Identity/EstudUserLoginDbConfig.cs",
    pk: ["login_provider", "provider_key"],
    columns: [
      { name: "login_provider", prop: "LoginProvider", type: "text", clr: "string", nullable: false },
      { name: "provider_key", prop: "ProviderKey", type: "text", clr: "string", nullable: false },
      { name: "provider_display_name", prop: "ProviderDisplayName", type: "text", clr: "string?", nullable: true },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "user_notifications",
    entity: "UserNotification",
    file: "Back/Database/Notifications/UserNotificationDbConfig.cs",
    pk: ["user_id", "notification_id"],
    columns: [
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "notification_id", prop: "NotificationId", type: "integer", clr: "int", nullable: false },
      { name: "viewed_at", prop: "ViewedAt", type: "timestamp with time zone", clr: "DateTime?", nullable: true },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: null, convention: false },
      { columns: ["notification_id"], target: "notifications", targetEntity: "Notification", principal: ["id"], nav: "Notification", convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "user_roles",
    entity: "EstudUserRole",
    file: "Back/Database/Identity/EstudUserRoleDbConfig.cs",
    pk: ["institution_id", "user_id", "role_id"],
    columns: [
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "role_id", prop: "RoleId", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: ["id"], nav: "Institution", convention: false },
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: "User", convention: false },
      { columns: ["role_id"], target: "roles", targetEntity: "EstudRole", principal: ["id"], nav: "Role", convention: false },
    ],
    indexes: [
      { columns: ["institution_id", "user_id"], unique: true },
    ],
  },
  {
    table: "user_social_logins",
    entity: "UserSocialLogin",
    file: "Back/Database/Identity/UserSocialLoginDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
      { name: "provider", prop: "Provider", type: "integer", clr: "SocialLoginProvider", nullable: false, enum: "SocialLoginProvider" },
      { name: "provider_key", prop: "ProviderKey", type: "text", clr: "string", nullable: false },
      { name: "email", prop: "Email", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["user_id"], target: "users", targetEntity: "EstudUser", principal: ["id"], nav: "User", convention: false },
    ],
    indexes: [
      { columns: ["provider", "provider_key"], unique: true },
      { columns: ["provider", "user_id"], unique: true },
      { columns: ["provider", "email"], unique: false },
    ],
  },
  {
    table: "user_tokens",
    entity: "EstudUserToken",
    file: "Back/Database/Identity/EstudUserTokenDbConfig.cs",
    pk: ["user_id", "login_provider", "name"],
    columns: [
      { name: "login_provider", prop: "LoginProvider", type: "text", clr: "string", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "value", prop: "Value", type: "text", clr: "string?", nullable: true },
      { name: "user_id", prop: "UserId", type: "integer", clr: "int", nullable: false },
    ],
    fks: [
    ],
    indexes: [
    ],
  },
  {
    table: "users",
    entity: "EstudUser",
    file: "Back/Database/Identity/EstudUserDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "user_name", prop: "UserName", type: "text", clr: "string?", nullable: true },
      { name: "normalized_user_name", prop: "NormalizedUserName", type: "text", clr: "string?", nullable: true },
      { name: "email", prop: "Email", type: "text", clr: "string?", nullable: true },
      { name: "normalized_email", prop: "NormalizedEmail", type: "text", clr: "string?", nullable: true },
      { name: "email_confirmed", prop: "EmailConfirmed", type: "boolean", clr: "bool", nullable: false },
      { name: "password_hash", prop: "PasswordHash", type: "text", clr: "string?", nullable: true },
      { name: "security_stamp", prop: "SecurityStamp", type: "text", clr: "string?", nullable: true },
      { name: "concurrency_stamp", prop: "ConcurrencyStamp", type: "text", clr: "string?", nullable: true },
      { name: "phone_number", prop: "PhoneNumber", type: "text", clr: "string?", nullable: true },
      { name: "phone_number_confirmed", prop: "PhoneNumberConfirmed", type: "boolean", clr: "bool", nullable: false },
      { name: "two_factor_enabled", prop: "TwoFactorEnabled", type: "boolean", clr: "bool", nullable: false },
      { name: "lockout_end", prop: "LockoutEnd", type: "timestamp with time zone", clr: "DateTimeOffset?", nullable: true },
      { name: "lockout_enabled", prop: "LockoutEnabled", type: "boolean", clr: "bool", nullable: false },
      { name: "access_failed_count", prop: "AccessFailedCount", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "profile_photo", prop: "ProfilePhoto", type: "text", clr: "string?", nullable: true },
      { name: "birthdate", prop: "Birthdate", type: "date", clr: "DateOnly?", nullable: true },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "webhook_call_attempts",
    entity: "WebhookCallAttempt",
    file: "Back/Database/Webhooks/WebhookCallAttemptDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "webhook_call_id", prop: "WebhookCallId", type: "integer", clr: "int", nullable: false },
      { name: "status", prop: "Status", type: "integer", clr: "WebhookCallAttemptStatus", nullable: false, enum: "WebhookCallAttemptStatus" },
      { name: "status_code", prop: "StatusCode", type: "integer", clr: "int", nullable: false },
      { name: "response", prop: "Response", type: "text", clr: "string", nullable: false },
      { name: "duration_ms", prop: "DurationMs", type: "integer", clr: "int", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["webhook_call_id"], target: "webhook_calls", targetEntity: "WebhookCall", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "webhook_calls",
    entity: "WebhookCall",
    file: "Back/Database/Webhooks/WebhookCallDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "uid", prop: "Uid", type: "text", clr: "string", nullable: false },
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "webhook_subscription_id", prop: "WebhookSubscriptionId", type: "integer", clr: "int", nullable: false },
      { name: "payload", prop: "Payload", type: "text", clr: "string", nullable: false },
      { name: "event_type", prop: "EventType", type: "integer", clr: "WebhookEventType", nullable: false, enum: "WebhookEventType" },
      { name: "status", prop: "Status", type: "integer", clr: "WebhookCallStatus", nullable: false, enum: "WebhookCallStatus" },
      { name: "attempts_count", prop: "AttemptsCount", type: "integer", clr: "int", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
      { columns: ["webhook_subscription_id"], target: "webhook_subscriptions", targetEntity: "WebhookSubscription", principal: ["id"], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
  {
    table: "webhook_subscriptions",
    entity: "WebhookSubscription",
    file: "Back/Database/Webhooks/WebhookSubscriptionDbConfig.cs",
    pk: ["id"],
    columns: [
      { name: "id", prop: "Id", type: "integer", clr: "int", nullable: false },
      { name: "institution_id", prop: "InstitutionId", type: "integer", clr: "int", nullable: false },
      { name: "name", prop: "Name", type: "text", clr: "string", nullable: false },
      { name: "url", prop: "Url", type: "text", clr: "string", nullable: false },
      { name: "is_active", prop: "IsActive", type: "boolean", clr: "bool", nullable: false },
      { name: "created_at", prop: "CreatedAt", type: "timestamp with time zone", clr: "DateTime", nullable: false },
      { name: "events", prop: "Events", type: "integer[]", clr: "List<WebhookEventType>", nullable: false, converted: true },
      { name: "custom_headers", prop: "CustomHeaders", type: "jsonb", clr: "Dictionary<string, string>", nullable: false, converted: true },
    ],
    fks: [
      { columns: ["institution_id"], target: "institutions", targetEntity: "Institution", principal: [], nav: null, convention: false },
    ],
    indexes: [
    ],
  },
]
