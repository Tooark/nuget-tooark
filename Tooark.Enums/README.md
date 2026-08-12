# Tooark.Enums

Biblioteca que fornece tipos enumerados validados, permitindo a padronização para projetos .NET. Inclui métodos para conversão e validação de valores enumerados.

## Conteúdo

- [ECloudProvider](#1-provedor-de-cloud)
- [EDocumentType](#2-tipo-de-documento)
- [EFileType](#3-tipo-de-arquivo)

## Enumeradores

Os enumeradores disponíveis são:

### 1. Provedor de Cloud

**Funcionalidade:**
Representa diferentes provedores de cloud.

**Tipos de Provedores:**

- `None`: Nenhum provedor.
- `Amazon`: AWS.
- `Google`: GCP.
- `Microsoft`: Azure.

**Métodos:**

- `ToString()`: Retorna a descrição do provedor de cloud.
- `ToInt()`: Retorna o ID do provedor de cloud.

[**Exemplo de Uso**](#provedor-de-cloud)

### 2. Tipo de Documento

**Funcionalidade:**
Representa diferentes tipos de documentos.

**Tipos de Documentos:**

- `None`
- `CPF`
- `RG`
- `CNH`
- `CNPJ`
- `CPF_CNPJ`
- `CPF_RG`
- `CPF_RG_CNH`

**Métodos:**

- `ToString()`: Retorna a descrição do tipo de documento.
- `ToInt()`: Retorna o ID do tipo de documento.
- `ToRegex()`: Retorna o padrão de regex do tipo de documento.
- `IsValid(string)`: Retorna a função de validação do tipo de documento.

[**Exemplo de Uso**](#tipo-de-documento)

### 3. Tipo de Arquivo

**Funcionalidade:**
Representa diferentes tipos de arquivos.

**Tipos de Arquivos:**

- `Image`: Imagem.
- `Document`: Documento.
- `Video`: Vídeo.
- `Audio`: Áudio.
- `Unknown`: Tipo desconhecido.

**Métodos:**

- `ToString()`: Retorna a descrição do tipo de arquivo.
- `ToInt()`: Retorna o ID do tipo de arquivo.

[**Exemplo de Uso**](#tipo-de-arquivo)

## Exemplo de Uso

### Provedor de Cloud

```csharp
using Tooark.Enums;

class Program
{
  static void Main()
  {
    ECloudProvider cloudProvider = ECloudProvider.Amazon;
    Console.WriteLine(cloudProvider.ToString()); // Output: AWS
    Console.WriteLine(cloudProvider.ToInt()); // Output: 1
  }
}
```

### Tipo de Documento

```csharp
using Tooark.Enums;

class Program
{
  static void Main()
  {
    EDocumentType docType = EDocumentType.CPF;
    Console.WriteLine(docType.ToString()); // Output: CPF
    Console.WriteLine(docType.ToInt()); // Output: 1
    Console.WriteLine(docType.ToRegex()); // Output: @"^\d{3}\.\d{3}\.\d{3}-\d{2}$"
    Console.WriteLine(docType.IsValid("12345678909")); // Output: True ou False dependendo da validade do CPF
  }
}
```

### Tipo de Arquivo

```csharp
using Tooark.Enums;

class Program
{
  static void Main()
  {
    EFileType fileType = EFileType.Image;
    Console.WriteLine(fileType.ToString()); // Output: Image
    Console.WriteLine(fileType.ToInt()); // Output: 1
  }
}
```

## Dependências

- [Tooark.Validations](../Tooark.Validations/README.md)

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests no repositório [Tooark.Enums](https://github.com/Tooark/tooark-cs/issues).

## Licença

Este projeto está licenciado sob a licença BSD 3-Clause. Veja o arquivo [LICENSE](../LICENSE) para mais detalhes.
