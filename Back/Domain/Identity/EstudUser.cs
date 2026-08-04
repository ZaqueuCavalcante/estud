using Estud.Back.Domain.Institutions;
using Estud.Back.Domain.Notifications;

namespace Estud.Back.Domain.Identity;

public class EstudUser : IdentityUser<int>
{
    public int InstitutionId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProfilePhoto { get; set; }
    public DateOnly? Birthdate { get; set; }

    public Institution? Institution { get; set; }
    public List<UserNotification> Notifications { get; set; } = [];

    public EstudUser() { }

    public EstudUser(
        Institution institution,
        string name,
        string email,
        bool emailConfirmed
    ) {
        Name = name;
        UserName = email;
        Email = email;
        CreatedAt = DateTime.UtcNow;
        Institution = institution;
        EmailConfirmed = emailConfirmed;
        Notifications = [.. institution.Notifications.Select(n => new UserNotification(n))];
    }

    public EstudUser(
        int institutionId,
        string name,
        string email
    ) {
        Name = name;
        UserName = email;
        Email = email;
        CreatedAt = DateTime.UtcNow;
        InstitutionId = institutionId;
    }

    public void Update(string name, string profilePhoto)
    {
        Name = name;
        ProfilePhoto = profilePhoto;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }
}
