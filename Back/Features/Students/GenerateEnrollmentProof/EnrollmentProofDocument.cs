using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Estud.Back.Domain.Students;

namespace Estud.Back.Features.Students.GenerateEnrollmentProof;

public class EnrollmentProofDocument(EnrollmentProof proof, string validationUrl) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken4).LineHeight(1.4f));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(24).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(QuestPDF.Infrastructure.IContainer header)
    {
        header.Column(col =>
        {
            col.Item().Text(proof.Metadata.Institution).FontSize(18).Bold().FontColor(Colors.Grey.Darken4);
            col.Item().Text("Comprovante de Matrícula").FontSize(13).FontColor(Colors.Grey.Darken2);
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(QuestPDF.Infrastructure.IContainer content)
    {
        content.Column(col =>
        {
            col.Spacing(18);

            col.Item().Text(text =>
            {
                text.Justify();
                text.Span("A instituição de ensino ");
                text.Span(proof.Metadata.Institution).SemiBold();
                text.Span(" declara, para os devidos fins, que o(a) aluno(a) abaixo identificado(a) " +
                    "encontra-se regularmente matriculado(a) no período letivo vigente, conforme os dados a seguir:");
            });

            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(16).Column(data =>
            {
                data.Spacing(8);
                DataRow(data, "Aluno(a)", proof.Metadata.StudentName);
                DataRow(data, "Matrícula", proof.Metadata.StudentEnrollmentCode);
                DataRow(data, "Curso", proof.Metadata.Course);
                DataRow(data, "Campus", proof.Metadata.Campus);
                DataRow(data, "Período letivo", proof.Metadata.Period);
                DataRow(data, "Turno", proof.Metadata.Session.GetDescription());
            });

            col.Item().Text($"Emitido em {IssuedAtText()}.")
                .FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeFooter(QuestPDF.Infrastructure.IContainer footer)
    {
        footer.Column(col =>
        {
            col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().Row(row =>
            {
                row.RelativeItem().PaddingRight(12).Column(info =>
                {
                    info.Spacing(3);
                    info.Item().Text("Autenticidade").SemiBold().FontSize(10);
                    info.Item().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1));
                        text.Span("Código de verificação: ");
                        text.Span(proof.Code).SemiBold().FontColor(Colors.Black);
                    });
                    info.Item().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1));
                        text.Span("Confira a validade deste comprovante em:");
                    });
                    info.Item().Text(validationUrl).FontSize(9).FontColor(Colors.Blue.Medium);
                });

                row.ConstantItem(88).Image(GenerateQrCode());
            });
        });
    }

    private static void DataRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(130).Text(label).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(value).SemiBold();
        });
    }

    private string IssuedAtText()
    {
        return proof.IssuedAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
    }

    private byte[] GenerateQrCode()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(validationUrl, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(data);

        return qrCode.GetGraphic(20);
    }
}
