using System.Security.Cryptography;
using System.Text;

namespace Tooark.Tests.Securities;

// Chaves AES de teste derivadas de um texto, para os testes continuarem legíveis ("segredo um", "segredo dois")
internal static class CryptographyTestKeys
{
  // A mesma derivação que o Secret em texto da linha 4.x fazia: o SHA-256 do texto, em Base64.
  // É também o valor que o guia de migração da v5 manda informar em SecretBase64.
  public static string FromText(string text) =>
    Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
