namespace Estud.Back.Domain.Enums;

public enum StorageContainer
{
    [Description("profile-photos")]
    ProfilePhotos = 0,

    [Description("lesson-plan-files")]
    LessonPlanFiles = 1,

    [Description("class-activity-files")]
    ClassActivityFiles = 2,

    [Description("class-activity-work-files")]
    ClassActivityWorkFiles = 3,
}

public static class StorageContainerExtensions
{
    extension(StorageContainer container)
    {
        public bool IsPublic => container switch
        {
            StorageContainer.ProfilePhotos => true,
            StorageContainer.LessonPlanFiles => true,
            StorageContainer.ClassActivityFiles => true,
            StorageContainer.ClassActivityWorkFiles => true,
            _ => false,
        };
    }
}
