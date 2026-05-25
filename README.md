# EZ.Redact.Lgpd.MongoDb

[![NuGet Version](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0+](https://img.shields.io/badge/.NET-8.0%2B%20|%209.0%2B%20|%2010.0%2B-512bd4.svg)](https://dotnet.microsoft.com/download)

Extensão MongoDB para o [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core). Redige dados pessoais automaticamente durante a leitura de documentos no **MongoDB**, sem precisar chamar `ILGPDRedactService` manualmente.

Basta decorar suas models com os atributos do `EZ.Redact.Lgpd.Core` e usar `.UseRedaction()` nas queries — a redação acontece de forma transparente.

---

## Instalação

```bash
dotnet add package EZ.Redact.Lgpd.MongoDb
```

Registre os serviços no DI:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.EnableRedaction(options => options.ApplyDiscriminator = false);
builder.Services.AddLGPDRedaction()
                .AddMongoDbRedaction();
```

> `AddMongoDbRedaction()` registra o suporte a redação em queries MongoDB.

## Configuração

Refere-se às opções do pacote [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core):

| Propriedade | Padrão | Descrição |
| :--- | :--- | :--- |
| `MaskChar` | `'*'` | Caractere usado no mascaramento |
| `Guid` | `new()` | Opções de redação de GUID (ver abaixo) |
| `HmacKey` | `null` | Chave HMAC em Base64 (obrigatória se `HmacFor` não estiver vazio) |
| `HmacKeyId` | `1` | Identificador da chave para rotação |
| `HmacFor` | `HashSet<>` vazio | Tipos de dado que devem usar HMAC em vez de masking |

### Três formas de configurar

**1. Em código (`Action<LGPDRedactOptions>`)**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.MaskChar = '#';
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    options.HmacFor.Add(DadoPessoal.CPF);
});
```

**2. Via `IConfiguration` (appsettings.json + env vars)**
```csharp
builder.Services.AddLGPDRedaction(builder.Configuration);
```

```json
{
  "LGPD": {
    "MaskChar": "#",
    "HmacFor": ["CPF"],
    "HmacKeyId": 1
  }
}
```

**3. Combinando ambas**
```csharp
builder.Services.AddLGPDRedaction(options =>
{
    options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
});
builder.Services.PostConfigure<LGPDRedactOptions>(opts =>
{
    opts.HmacFor.Add(DadoPessoal.CPF);
});
```

---

## Uso

Adicione `.UseRedaction()` à sua collection para que os documentos retornados tenham os dados sensíveis redigidos automaticamente:

```csharp
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Attributes;
using EZ.Redact.Lgpd.MongoDb;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLGPDRedaction()
    .AddMongoDbRedaction();

builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("exemplo");
});

var app = builder.Build();

app.MapGet("/clientes", async (IMongoDatabase db) =>
{
    var collection = db.GetCollection<Cliente>("clientes");

    var comRedacao = await collection
        .UseRedaction()
        .Find(c => c.Ativo)
        .ToListAsync();

    var semRedacao = await collection
        .Find(c => c.Ativo)
        .ToListAsync();

    return Results.Ok(new { comRedacao, semRedacao });
});

app.Run();
```

### Sem redação

Basta **omitir** `.UseRedaction()` na mesma collection — a query executa normalmente sem redação:

```csharp
var clientes = await collection
    .Find(c => c.Ativo)
    .ToListAsync();
```

### Modelo com atributos

```csharp
public class Cliente
{
    [CPFData]
    public string? Cpf { get; set; }

    [NomeData]
    public string? Nome { get; set; }

    [EmailData]
    public string? Email { get; set; }

    public bool Ativo { get; set; }
}
```

### Resultado redigido

```json
{
  "comRedacao": [
    {
      "cpf": "123.***.***-01",
      "nome": "J*** S****",
      "email": "j*****@gmail.com",
      "ativo": true
    }
  ],
  "semRedacao": [
    {
      "cpf": "123.456.789-01",
      "nome": "João Silva",
      "email": "joao.silva@gmail.com",
      "ativo": true
    }
  ]
}
```

---

## Como funciona

1. `UseRedaction()` envolve `IMongoCollection<T>` em um wrapper que intercepta as operações de leitura
2. Quando `Find()`, `FindAsync()` ou `FindSync()` é chamado, os cursores retornados são encapsulados
3. Ao materializar cada documento, o `RedactionHelper` inspeciona as propriedades via reflection com cache (`ConcurrentDictionary`)
4. Propriedades com `DataClassificationAttribute` de taxonomia `"LGPD"` são redigidas através do `ILGPDRedactService`
5. O serviço de redação respeita as configurações de environment definidas em `AddLGPDRedaction()` — quando a redação está desabilitada, os dados retornam inalterados

---

## Atributos Suportados

Os atributos são definidos pelo pacote [EZ.Redact.Lgpd.Core](https://github.com/ez-dotnet/ez-redact-lgpd-core) e funcionam com qualquer provedor de dados.

### Identificação Pessoal

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[NomeData]` | Mantem apenas as iniciais de cada palavra | `Maria da Silva` | `M**** d* S****` |
| `[CPFData]` | Preserva 3 primeiros e 2 ultimos digitos | `123.456.789-01` | `123.***.***-01` |
| `[CNPJData]` | Preserva raiz (2 caracteres) e radical (6 ultimos) | `12.345.678/0001-90` | `12.***.***/0001-90` |
| `[EmailData]` | Preserva inicial e dominio | `joao.silva@gmail.com` | `j********@gmail.com` |
| `[TelefoneData]` | Preserva DDD, 1 digito apos DDD e 4 ultimos | `(11) 98888-4444` | `(11) 9****-4444` |
| `[EnderecoData]` | Mantem apenas as iniciais, oculta numeros | `Avenida Paulista, 1000` | `A****** P*******, ****` |
| `[DataGenericaData]` | Preserva ano, mascara dia/mes | `15/03/1990` | `**/**/1990` |

### Documentos Oficiais

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CNHData]` | Preserva 3 primeiros e 2 ultimos digitos | `12345678901` | `123******01` |
| `[TituloEleitorData]` | Preserva 4 primeiros e 4 ultimos digitos | `1234.5678.9012` | `1234.****.9012` |
| `[PISData]` | Preserva 3 primeiros e digito verificador | `123.45678.90-1` | `123.*****.**-1` |
| `[CNSData]` | Preserva 3 primeiros e 4 ultimos | `123 4567 8901 2345` | `123 **** **** 2345` |
| `[CTPSData]` | Preserva 3 primeiros e 3 ultimos | `1234567890` | `123****890` |
| `[CertidaoData]` | Preserva 6 primeiros e 2 verificadores | `123456.78.1234.5.6.7890.1.12345-67` | `123456.**.****.*.*.****.*.*****-67` |
| `[PassaporteData]` | Preserva prefixo letras e 2 ultimos digitos | `AB123456` | `AB****56` |
| `[RNEData]` | Preserva letra prefixo e digito verificador | `V1234567-8` | `V*******-8` |

### Financeiro

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[CartaoCreditoData]` | Preserva 4 primeiros e 4 ultimos digitos | `4532 1178 9012 3456` | `4532 **** **** 3456` |
| `[ContaBancariaData]` | Preserva operacao e digito, mascara conta | `013.123456-7` | `013.******-7` |
| `[PixData]` | Mascara chave aleatoria mantendo 4 primeiros e 8 ultimos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-****2e114b22` |

### Redes e Localização

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[EnderecoIPData]` | Mascara os 2 ultimos octetos (IPv4) e os ultimos 3 grupos (IPv6) | `192.168.1.100` | `192.168.*.***` |
| `[MacAddressData]` | Preserva prefixo OUI (3 primeiros bytes) | `00:1A:2B:3C:4D:5E` | `00:1A:2B:**:**:**` |
| `[CEPData]` | Mascara os 3 ultimos digitos | `01310-900` | `01310-***` |
| `[GeolocalizacaoData]` | Mascara parte decimal de latitude e longitude | `-23.5505, -46.6333` | `-23.****, -46.****` |

### Veículo

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[PlacaData]` | Mascara numeros (padrao antigo) e caracteres apos prefixo (Mercosul) | `ABC-1234` | `ABC-****` |
| `[RenavamData]` | Preserva 3 primeiros e 3 ultimos digitos | `12345678901` | `123*****901` |

### Técnico

| Atributo | O que faz? | Exemplo Original | Exemplo Redigido |
| :--- | :--- | :--- | :--- |
| `[GuidData]` | Mascara GUID mantendo 4 primeiros e 4 ultimos hex digitos | `e8d26618-2e11-4b22-8d26-66182e114b22` | `e8d2****-****-****-****-*******4b22` |

---

## Samples

Um projeto de exemplo na pasta `samples/`:

| Projeto | Descrição |
| :--- | :--- |
| [`EZ.Redact.Lgpd.MongoDb.SampleApi`](samples/EZ.Redact.Lgpd.MongoDb.SampleApi) | Minimal API com endpoints `/clientes` com e sem redação |

```bash
dotnet run --project samples/EZ.Redact.Lgpd.MongoDb.SampleApi
curl http://localhost:5000/clientes | jq
```

---

## Licença

Distribuído sob a licença MIT.
