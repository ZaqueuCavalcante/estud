namespace Estud.Back.Errors;

public abstract class EstudError
{
    public abstract string Code { get; set; }
    public abstract string Message { get; set; }

    public SwaggerExample<ErrorOut> ToSwaggerExampleErrorOut()
    {
        return SwaggerExample.Create(Message, new ErrorOut { Code = Code, Message = Message });
    }
}

public class EstudSuccess
{
    public static readonly EstudSuccess I = new();
}
