namespace Estud.Back.Errors;

public class InvalidEmail : EstudError
{
    public static readonly InvalidEmail I = new();
    public override string Code { get; set; } = nameof(InvalidEmail);
    public override string Message { get; set; } = "Email inválido.";
}

public class EmailAlreadyUsed : EstudError
{
    public static readonly EmailAlreadyUsed I = new();
    public override string Code { get; set; } = nameof(EmailAlreadyUsed);
    public override string Message { get; set; } = "Email já utilizado.";
}

public class InvalidUserName : EstudError
{
    public static readonly InvalidUserName I = new();
    public override string Code { get; set; } = nameof(InvalidUserName);
    public override string Message { get; set; } = "Nome de usuário inválido.";
}

public class InvalidPhoneNumber : EstudError
{
    public static readonly InvalidPhoneNumber I = new();
    public override string Code { get; set; } = nameof(InvalidPhoneNumber);
    public override string Message { get; set; } = "Número de telefone inválido.";
}

public class InvalidBirthdate : EstudError
{
    public static readonly InvalidBirthdate I = new();
    public override string Code { get; set; } = nameof(InvalidBirthdate);
    public override string Message { get; set; } = "Data de nascimento inválida.";
}

public class InvalidProfilePhotoContentType : EstudError
{
    public static readonly InvalidProfilePhotoContentType I = new();
    public override string Code { get; set; } = nameof(InvalidProfilePhotoContentType);
    public override string Message { get; set; } = "Formato de foto inválido. Envie PNG, JPEG ou WebP.";
}

public class InvalidProfilePhotoSize : EstudError
{
    public static readonly InvalidProfilePhotoSize I = new();
    public override string Code { get; set; } = nameof(InvalidProfilePhotoSize);
    public override string Message { get; set; } = "A foto deve ter no máximo 3 MB.";
}

public class InvalidProfilePhotoPath : EstudError
{
    public static readonly InvalidProfilePhotoPath I = new();
    public override string Code { get; set; } = nameof(InvalidProfilePhotoPath);
    public override string Message { get; set; } = "Caminho da foto inválido.";
}

public class ProfilePhotoNotFound : EstudError
{
    public static readonly ProfilePhotoNotFound I = new();
    public override string Code { get; set; } = nameof(ProfilePhotoNotFound);
    public override string Message { get; set; } = "Foto não encontrada. Envie o arquivo antes de confirmar.";
}
