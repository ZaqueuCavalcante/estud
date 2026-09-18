namespace Estud.Back.Domain.Enums;

public enum StorageContainer
{
    [Description("profile-photos")]
    ProfilePhotos = 0,

    [Description("lesson-plan-images")]
    LessonPlanImages = 1,

    [Description("class-activity-files")]
    ClassActivityFiles = 2,
}

public static class StorageContainerExtensions
{
    extension(StorageContainer container)
    {
        public bool IsPublic => container switch
        {
            StorageContainer.ProfilePhotos => true,
            StorageContainer.LessonPlanImages => true,
            StorageContainer.ClassActivityFiles => true,
            _ => false,
        };
    }
}
