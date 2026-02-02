namespace Toki;

using System;
using System.Text;

public static class Hasher {
  public static string ComputeMd5(string input) {
    var bytes = Encoding.UTF8.GetBytes(input);
    var hashBytes = System.Security.Cryptography.MD5.HashData(bytes);
    return Convert.ToHexStringLower(hashBytes);
  }
}
