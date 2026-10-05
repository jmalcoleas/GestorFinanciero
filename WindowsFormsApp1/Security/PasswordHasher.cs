using System;
using System.Security.Cryptography;

namespace GestorFinancieroApp.Security
{
    /// <summary>
    /// Cifrado de contraseñas con PBKDF2-SHA256 y sal aleatoria por usuario.
    /// Formato guardado: v1$iteraciones$sal$hash (Base64).
    /// </summary>
    internal static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash = Derive(password, salt, Iterations);
            return "v1$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            string[] parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "v1") return false;

            int iterations;
            byte[] salt, expected;
            try
            {
                iterations = int.Parse(parts[1]);
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException) { return false; }
            if (iterations < 1) return false;

            byte[] actual = Derive(password, salt, iterations);
            // Comparación en tiempo constante.
            int diff = actual.Length ^ expected.Length;
            for (int i = 0; i < actual.Length && i < expected.Length; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(HashSize);
        }
    }
}
