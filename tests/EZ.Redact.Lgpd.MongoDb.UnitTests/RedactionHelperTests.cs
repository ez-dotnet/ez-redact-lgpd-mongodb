using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Attributes;
using EZ.Redact.Lgpd.MongoDb.Internal;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EZ.Redact.Lgpd.MongoDb.UnitTests;

public class RedactionHelperTests
{
    private class Cliente
    {
        [CPFData]
        public string? Cpf { get; set; }

        [NomeData]
        public string? Nome { get; set; }

        public string? SemAtributo { get; set; }
    }

    [Fact]
    public void Redact_DeveRedigirPropriedadesComAtributo()
    {
        var service = Substitute.For<ILGPDRedactService>();
        service.Redact(DadoPessoal.CPF, "123.456.789-01").Returns("***.456.789-**");
        service.Redact(DadoPessoal.Nome, "João Silva").Returns("João S***a");

        var cliente = new Cliente
        {
            Cpf = "123.456.789-01",
            Nome = "João Silva",
            SemAtributo = "Visível"
        };

        RedactionHelper.Redact(cliente, service);

        cliente.Cpf.Should().Be("***.456.789-**");
        cliente.Nome.Should().Be("João S***a");
        cliente.SemAtributo.Should().Be("Visível");
    }
}
