using Bogus;

namespace Estud.Tests.Extensions;

public class StringExtensionsUnitTests
{
    [Test]
    [TestCaseSource(nameof(CamelCaseNames))]
    public void StringExtensions_Should_change_to_snake_case(string camel, string snake)
    {
        // Arrange / Act
        var result = camel.ToSnakeCase();

        // Assert
        result.Should().Be(snake);
    }

    [Test]
    [TestCaseSource(nameof(FormatedStrings))]
    public void StringExtensions_Should_change_to_only_numbers(string text, string numbers)
    {
        // Arrange / Act
        var result = text.OnlyNumbers();

        // Assert
        result.Should().Be(numbers);
    }

    [Test]
    [Repeat(100)]
    public void StringExtensions_Should_return_true_when_email_is_valid()
    {
        // Arrange
        var email = new Faker().Internet.Email();

        // Act
        var result = email.IsValidEmail();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    [TestCaseSource(nameof(InvalidEmails))]
    public void StringExtensions_Should_return_false_when_email_is_invalid(string email)
    {
        // Arrange // Act
        var result = email.IsValidEmail();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    [TestCaseSource(nameof(TextsWithoutEmailDomain))]
    public void StringExtensions_Should_return_empty_email_domain_when_text_has_no_domain(string text)
    {
        // Arrange / Act
        var result = text.GetEmailDomain();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    [TestCaseSource(nameof(EmailsWithDomain))]
    public void StringExtensions_Should_get_email_domain(string email, string domain)
    {
        // Arrange / Act
        var result = email.GetEmailDomain();

        // Assert
        result.Should().Be(domain);
    }

    [Test]
    [TestCaseSource(nameof(ValidPhoneNumbers))]
    public void StringExtensions_Should_return_true_when_phone_number_is_valid(string phoneNumber)
    {
        // Arrange / Act
        var result = phoneNumber.IsValidPhoneNumber();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    [TestCaseSource(nameof(InvalidPhoneNumbers))]
    public void StringExtensions_Should_return_false_when_phone_number_is_invalid(string phoneNumber)
    {
        // Arrange / Act
        var result = phoneNumber.IsValidPhoneNumber();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    [TestCase("SELECT * FROM estud.courses", "SELECT")]
    [TestCase("INSERT INTO estud.courses (name) VALUES (@p0)", "INSERT")]
    [TestCase("UPDATE estud.courses SET name = @p0 WHERE id = @p1", "UPDATE")]
    [TestCase("DELETE FROM estud.courses WHERE id = @p0", "DELETE")]
    public void StringExtensions_GetSqlSpanName_Should_return_single_command(string sql, string expected)
    {
        // Arrange / Act
        var result = sql.GetSqlSpanName();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("select * from estud.courses", "SELECT")]
    [TestCase("insert into estud.courses (name) values (@p0)", "INSERT")]
    [TestCase("update estud.courses set name = @p0", "UPDATE")]
    [TestCase("delete from estud.courses", "DELETE")]
    [TestCase("Select * From estud.courses", "SELECT")]
    [TestCase("InSeRt INTO estud.courses (name) VALUES (@p0)", "INSERT")]
    public void StringExtensions_GetSqlSpanName_Should_ignore_case(string sql, string expected)
    {
        // Arrange / Act
        var result = sql.GetSqlSpanName();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("INSERT INTO estud.courses (name) SELECT name FROM estud.old_courses", "INSERT")]
    [TestCase("UPDATE estud.courses SET name = (SELECT name FROM estud.old_courses LIMIT 1)", "UPDATE")]
    [TestCase("DELETE FROM estud.courses WHERE id IN (SELECT id FROM estud.old_courses)", "DELETE")]
    public void StringExtensions_GetSqlSpanName_Should_ignore_select_when_there_is_a_write_command(string sql, string expected)
    {
        // Arrange / Act
        var result = sql.GetSqlSpanName();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("INSERT INTO estud.courses (id, name) VALUES (@p0, @p1) ON CONFLICT (id) DO UPDATE SET name = @p1", "INSERT UPDATE")]
    [TestCase("INSERT INTO estud.courses (name) VALUES (@p0); DELETE FROM estud.old_courses", "INSERT DELETE")]
    [TestCase("UPDATE estud.courses SET name = @p0; DELETE FROM estud.old_courses", "UPDATE DELETE")]
    [TestCase("INSERT INTO estud.a VALUES (1); UPDATE estud.b SET x = 1; DELETE FROM estud.c", "INSERT UPDATE DELETE")]
    [TestCase("DELETE FROM estud.c; UPDATE estud.b SET x = 1; INSERT INTO estud.a VALUES (1)", "INSERT UPDATE DELETE")]
    [TestCase("SELECT 1; INSERT INTO estud.a VALUES (1); UPDATE estud.b SET x = 1; DELETE FROM estud.c", "INSERT UPDATE DELETE")]
    public void StringExtensions_GetSqlSpanName_Should_join_write_commands_in_fixed_order(string sql, string expected)
    {
        // Arrange / Act
        var result = sql.GetSqlSpanName();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("BEGIN")]
    [TestCase("COMMIT")]
    [TestCase("SAVEPOINT __EFSavePoint")]
    [TestCase("CREATE TABLE estud.courses (id integer)")]
    public void StringExtensions_GetSqlSpanName_Should_return_empty_when_there_is_no_known_command(string sql)
    {
        // Arrange / Act
        var result = sql.GetSqlSpanName();

        // Assert
        result.Should().BeEmpty();
    }

    private static IEnumerable<object[]> CamelCaseNames()
    {
        foreach (var (camel, snake) in new List<(string, string)>()
        {
            ("", ""),
            (" ", ""),
            (null!, ""),
            ("AspNetUsers", "asp_net_users"),
            ("AspNetUserRoles", "asp_net_user_roles"),
            ("AspNetRoleClaims", "asp_net_role_claims"),
        })
        {
            yield return [camel, snake];
        }
    }

    private static IEnumerable<object[]> FormatedStrings()
    {
        foreach (var (text, numbers) in new List<(string, string)>()
        {
            ("", ""),
            (" ", ""),
            (null!, ""),
            ("ewfewfewf", ""),
            ("629.219.140-00", "62921914000"),
            ("(81) 98578-9526", "81985789526"),
            ("yu2v34y1434u6b54u6b", "23414346546"),
            ("18.297.767/0001-90", "18297767000190"),
        })
        {
            yield return [text, numbers];
        }
    }

    private static IEnumerable<object[]> InvalidEmails()
    {
        List<string> emails = [
            "",
            " ",
            "zaqueugmail",
            "majuasp.net",
            "#@%^%#$@#$@#.com",
            "@example.com",
            "Joe Smith <email@example.com>",
            "email.example.com",
            "email@example@example.com",
            ".email@example.com",
            "email.@example.com",
            "email..email@example.com",
            "email@example.com (Joe Smith)",
            "email@example",
            "email@-example.com",
            "email@example..com",
            "Abc..123@example.com",
        ];
        foreach (var email in emails)
        {
            yield return [email];
        }
    }

    private static IEnumerable<object[]> TextsWithoutEmailDomain()
    {
        List<string> texts = [
            null!,
            "",
            " ",
            "   ",
            "\t",
            "\n",
            " \t\r\n ",
            "zaqueugmail",
            "email.example.com",
            "example.com",
            "Joe Smith",
            "email#example.com",
            "email＠example.com",
            "@",
            "@@",
            "email@",
            "email@example.com@",
        ];
        foreach (var text in texts)
        {
            yield return [text];
        }
    }

    private static IEnumerable<object[]> EmailsWithDomain()
    {
        foreach (var (email, domain) in new List<(string, string)>()
        {
            ("email@example.com", "example.com"),
            ("zaqueu@gmail.com", "gmail.com"),
            ("director@estud.com.br", "estud.com.br"),
            ("professor@sub.dominio.edu.br", "sub.dominio.edu.br"),
            ("first.last+tag@example.com", "example.com"),
            ("123.test@456.estud.com.br", "456.estud.com.br"),
            ("email@localhost", "localhost"),
            ("email@127.0.0.1", "127.0.0.1"),
            ("email@[127.0.0.1]", "[127.0.0.1]"),
            ("email@example", "example"),
            ("@example.com", "example.com"),
            ("Diretor@Empresa.COM", "Empresa.COM"),
            ("email@example@other.com", "other.com"),
            ("email@@example.com", "example.com"),
            (" email@example.com ", "example.com "),
            ("email@ example.com", " example.com"),
            ("Joe Smith <email@example.com>", "example.com>"),
            ("email@exämple.com", "exämple.com"),
        })
        {
            yield return [email, domain];
        }
    }

    private static IEnumerable<object[]> ValidPhoneNumbers()
    {
        List<string> phoneNumbers = [
            "8233334444",
            "82988887777",
            "1198578952",
            "11985789526",
        ];
        foreach (var phoneNumber in phoneNumbers)
        {
            yield return [phoneNumber];
        }
    }

    private static IEnumerable<object[]> InvalidPhoneNumbers()
    {
        List<string> phoneNumbers = [
            null!,
            "",
            " ",
            "123456789",            // menos de 10 dígitos
            "123456789012",         // mais de 11 dígitos
            "(82) 98888-7777",      // com máscara
            "82 98888 7777",        // com espaços
            "+5582988887777",       // com código do país
            "8298888777a",
            "abcdefghijk",
        ];
        foreach (var phoneNumber in phoneNumbers)
        {
            yield return [phoneNumber];
        }
    }
}
