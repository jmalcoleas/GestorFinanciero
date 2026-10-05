using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace GestorFinancieroApp.Data
{
    /// <summary>Acceso a datos. Todas las consultas están parametrizadas y limitadas al usuario.</summary>
    internal static class Repository
    {
        private static readonly string[] DefaultExpense =
            { "Alimentación", "Transporte", "Ocio", "Cenas", "Vivienda", "Salud" };
        private static readonly string[] DefaultIncome = { "Salario", "Freelance", "Otros ingresos" };

        private static SqlConnection Open()
        {
            var cn = new SqlConnection(Db.ConnectionString);
            cn.Open();
            return cn;
        }

        // Parámetros como pares nombre/valor alternados.
        private static SqlCommand Cmd(SqlConnection cn, string sql, params object[] kv)
        {
            var cmd = new SqlCommand(sql, cn);
            for (int i = 0; i < kv.Length; i += 2)
                cmd.Parameters.AddWithValue((string)kv[i], kv[i + 1] ?? DBNull.Value);
            return cmd;
        }

        private static string NullIfEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        // ------------------------------------------------------------ Usuarios

        public static bool EmailExists(string email)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn, "SELECT COUNT(*) FROM users WHERE email = @e", "@e", email))
                return (int)cmd.ExecuteScalar() > 0;
        }

        public static User FindUserByEmail(string email)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "SELECT id, name, email, password_hash, initial_balance, initial_savings FROM users WHERE email = @e AND is_active = 1",
                "@e", email))
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return null;
                return new User
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Email = r.GetString(2),
                    PasswordHash = r.GetString(3),
                    InitialBalance = r.GetDecimal(4),
                    InitialSavings = r.GetDecimal(5)
                };
            }
        }

        public static User CreateUser(string name, string email, string passwordHash, decimal initialBalance, decimal initialSavings)
        {
            using (var cn = Open())
            using (var tx = cn.BeginTransaction())
            {
                int id;
                using (var cmd = Cmd(cn,
                    "INSERT INTO users (name, email, password_hash, initial_balance, initial_savings) " +
                    "OUTPUT INSERTED.id VALUES (@n, @e, @p, @b, @s)",
                    "@n", name, "@e", email, "@p", passwordHash, "@b", initialBalance, "@s", initialSavings))
                {
                    cmd.Transaction = tx;
                    id = (int)cmd.ExecuteScalar();
                }

                InsertDefaults(cn, tx, id, DefaultExpense, "expense");
                InsertDefaults(cn, tx, id, DefaultIncome, "income");
                tx.Commit();
                return new User
                {
                    Id = id, Name = name, Email = email,
                    InitialBalance = initialBalance, InitialSavings = initialSavings
                };
            }
        }

        public static void UpdateInitialBalances(int userId, decimal initialBalance, decimal initialSavings)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "UPDATE users SET initial_balance = @b, initial_savings = @s, updated_at = GETDATE() WHERE id = @u",
                "@b", initialBalance, "@s", initialSavings, "@u", userId))
                cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Saldos acumulados de todos los meses: cuenta = saldo inicial + ingresos que llegan a la cuenta - gastos;
        /// ahorros = ahorros iniciales + todo lo destinado a ahorros desde los ingresos.
        /// </summary>
        public static void GetTotals(int userId, out decimal account, out decimal savings)
        {
            account = 0;
            savings = 0;
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
SELECT u.initial_balance + ISNULL(SUM(CASE WHEN c.type = 'income' THEN t.amount - t.savings_amount
                                           WHEN c.type = 'expense' THEN -t.amount END), 0),
       u.initial_savings + ISNULL(SUM(CASE WHEN c.type = 'income' THEN t.savings_amount END), 0)
FROM users u
LEFT JOIN transactions t ON t.user_id = u.id AND t.is_active = 1
LEFT JOIN categories c ON c.id = t.category_id
WHERE u.id = @u
GROUP BY u.initial_balance, u.initial_savings", "@u", userId))
            using (var r = cmd.ExecuteReader())
                if (r.Read())
                {
                    account = r.GetDecimal(0);
                    savings = r.GetDecimal(1);
                }
        }

        public static bool IsIncomePromptSkipped(int userId, int year, int month)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "SELECT COUNT(*) FROM income_prompt_skips WHERE user_id = @u AND [year] = @y AND [month] = @m",
                "@u", userId, "@y", year, "@m", month))
                return (int)cmd.ExecuteScalar() > 0;
        }

        public static void SkipIncomePrompt(int userId, int year, int month)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
IF NOT EXISTS (SELECT 1 FROM income_prompt_skips WHERE user_id = @u AND [year] = @y AND [month] = @m)
    INSERT INTO income_prompt_skips (user_id, [year], [month]) VALUES (@u, @y, @m);",
                "@u", userId, "@y", year, "@m", month))
                cmd.ExecuteNonQuery();
        }

        private static void InsertDefaults(SqlConnection cn, SqlTransaction tx, int userId, string[] names, string type)
        {
            foreach (string n in names)
                using (var cmd = Cmd(cn, "INSERT INTO categories (name, type, user_id) VALUES (@n, @t, @u)",
                    "@n", n, "@t", type, "@u", userId))
                {
                    cmd.Transaction = tx;
                    cmd.ExecuteNonQuery();
                }
        }

        // ---------------------------------------------------------- Categorías

        public static List<Category> GetCategories(int userId, string type)
        {
            var list = new List<Category>();
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "SELECT id, name, type FROM categories WHERE user_id = @u AND type = @t AND is_active = 1 ORDER BY name",
                "@u", userId, "@t", type))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new Category { Id = r.GetInt32(0), Name = r.GetString(1), Type = r.GetString(2) });
            return list;
        }

        /// <summary>Devuelve la categoría con ese nombre y tipo; si no existe, la crea.</summary>
        public static int GetOrCreateCategory(int userId, string name, string type)
        {
            const string sql = @"
DECLARE @id INT = (SELECT TOP 1 id FROM categories WHERE user_id = @u AND type = @t AND name = @n);
IF @id IS NULL
BEGIN
    INSERT INTO categories (name, type, user_id) VALUES (@n, @t, @u);
    SET @id = SCOPE_IDENTITY();
END
ELSE
    UPDATE categories SET is_active = 1, updated_at = GETDATE() WHERE id = @id AND is_active = 0;
SELECT @id;";
            using (var cn = Open())
            using (var cmd = Cmd(cn, sql, "@u", userId, "@t", type, "@n", name.Trim()))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // -------------------------------------------------------- Transacciones

        public static List<TransactionRow> GetTransactions(int userId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var list = new List<TransactionRow>();
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
SELECT t.id, t.date, t.description, t.amount, t.category_id, c.name, c.type, t.savings_amount
FROM transactions t JOIN categories c ON c.id = t.category_id
WHERE t.user_id = @u AND t.is_active = 1 AND t.date >= @from AND t.date < @to
ORDER BY t.date DESC, t.id DESC",
                "@u", userId, "@from", from, "@to", from.AddMonths(1)))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new TransactionRow
                    {
                        Id = r.GetInt32(0),
                        Date = r.GetDateTime(1),
                        Description = r.IsDBNull(2) ? "" : r.GetString(2),
                        Amount = r.GetDecimal(3),
                        CategoryId = r.GetInt32(4),
                        CategoryName = r.GetString(5),
                        Type = r.GetString(6),
                        Savings = r.GetDecimal(7)
                    });
            return list;
        }

        /// <param name="savings">Parte del importe que va a ahorros (solo tiene sentido en ingresos).</param>
        public static void AddTransaction(int userId, int categoryId, DateTime date, decimal amount, decimal savings, string description)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "INSERT INTO transactions (amount, savings_amount, date, description, category_id, user_id) " +
                "VALUES (@a, @sv, @d, @desc, @c, @u)",
                "@a", amount, "@sv", savings, "@d", date.Date, "@desc", NullIfEmpty(description), "@c", categoryId, "@u", userId))
                cmd.ExecuteNonQuery();
        }

        public static void UpdateTransaction(int userId, int id, int categoryId, DateTime date, decimal amount, decimal savings, string description)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
UPDATE transactions
SET amount = @a, savings_amount = @sv, date = @d, description = @desc, category_id = @c, updated_at = GETDATE()
WHERE id = @id AND user_id = @u AND is_active = 1",
                "@a", amount, "@sv", savings, "@d", date.Date, "@desc", NullIfEmpty(description), "@c", categoryId, "@id", id, "@u", userId))
                cmd.ExecuteNonQuery();
        }

        /// <summary>Borrado lógico: la fila se conserva con is_active = 0.</summary>
        public static void DeleteTransaction(int userId, int id)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "UPDATE transactions SET is_active = 0, updated_at = GETDATE() WHERE id = @id AND user_id = @u",
                "@id", id, "@u", userId))
                cmd.ExecuteNonQuery();
        }

        public static void GetSummary(int userId, int year, int month, out decimal income, out decimal savings, out decimal expenses)
        {
            var from = new DateTime(year, month, 1);
            income = 0;
            savings = 0;
            expenses = 0;
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
SELECT c.type, SUM(t.amount), SUM(t.savings_amount)
FROM transactions t JOIN categories c ON c.id = t.category_id
WHERE t.user_id = @u AND t.is_active = 1 AND t.date >= @from AND t.date < @to
GROUP BY c.type",
                "@u", userId, "@from", from, "@to", from.AddMonths(1)))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    if (r.GetString(0) == "income")
                    {
                        income = r.GetDecimal(1);
                        savings = r.GetDecimal(2);
                    }
                    else expenses = r.GetDecimal(1);
                }
        }

        // ---------------------------------------------------------- Presupuestos

        public static List<BudgetRow> GetBudgets(int userId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var list = new List<BudgetRow>();
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
SELECT b.id, b.category_id, c.name, b.amount,
       ISNULL((SELECT SUM(t.amount) FROM transactions t
               WHERE t.user_id = b.user_id AND t.category_id = b.category_id AND t.is_active = 1
                 AND t.date >= @from AND t.date < @to), 0) AS spent
FROM budgets b JOIN categories c ON c.id = b.category_id
WHERE b.user_id = @u AND b.[year] = @y AND b.[month] = @m
ORDER BY c.name",
                "@u", userId, "@y", year, "@m", month, "@from", from, "@to", from.AddMonths(1)))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new BudgetRow
                    {
                        Id = r.GetInt32(0),
                        CategoryId = r.GetInt32(1),
                        CategoryName = r.GetString(2),
                        Amount = r.GetDecimal(3),
                        Spent = r.GetDecimal(4)
                    });
            return list;
        }

        /// <summary>Crea el presupuesto o, si la categoría ya tenía uno ese mes, actualiza su importe.</summary>
        public static void SaveBudget(int userId, int categoryId, int year, int month, decimal amount)
        {
            const string sql = @"
UPDATE budgets SET amount = @a, updated_at = GETDATE()
WHERE user_id = @u AND category_id = @c AND [year] = @y AND [month] = @m;
IF @@ROWCOUNT = 0
    INSERT INTO budgets (user_id, category_id, [year], [month], amount) VALUES (@u, @c, @y, @m, @a);";
            using (var cn = Open())
            using (var cmd = Cmd(cn, sql, "@a", amount, "@u", userId, "@c", categoryId, "@y", year, "@m", month))
                cmd.ExecuteNonQuery();
        }

        public static void DeleteBudget(int userId, int id)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn, "DELETE FROM budgets WHERE id = @id AND user_id = @u", "@id", id, "@u", userId))
                cmd.ExecuteNonQuery();
        }

        /// <summary>Copia los presupuestos de un mes a otro sin pisar los que ya existan. Devuelve cuántos copió.</summary>
        public static int CopyBudgets(int userId, int fromYear, int fromMonth, int toYear, int toMonth)
        {
            const string sql = @"
INSERT INTO budgets (user_id, category_id, [year], [month], amount)
SELECT b.user_id, b.category_id, @ty, @tm, b.amount
FROM budgets b JOIN categories c ON c.id = b.category_id AND c.is_active = 1
WHERE b.user_id = @u AND b.[year] = @fy AND b.[month] = @fm
  AND NOT EXISTS (SELECT 1 FROM budgets x
                  WHERE x.user_id = b.user_id AND x.category_id = b.category_id AND x.[year] = @ty AND x.[month] = @tm);
SELECT @@ROWCOUNT;";
            using (var cn = Open())
            using (var cmd = Cmd(cn, sql, "@u", userId, "@fy", fromYear, "@fm", fromMonth, "@ty", toYear, "@tm", toMonth))
                return (int)cmd.ExecuteScalar();
        }

        // ---------------------------------------------------------- Felicitaciones

        public static bool IsCelebrated(int userId, int year, int month)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn,
                "SELECT COUNT(*) FROM month_celebrations WHERE user_id = @u AND [year] = @y AND [month] = @m",
                "@u", userId, "@y", year, "@m", month))
                return (int)cmd.ExecuteScalar() > 0;
        }

        public static void MarkCelebrated(int userId, int year, int month)
        {
            using (var cn = Open())
            using (var cmd = Cmd(cn, @"
IF NOT EXISTS (SELECT 1 FROM month_celebrations WHERE user_id = @u AND [year] = @y AND [month] = @m)
    INSERT INTO month_celebrations (user_id, [year], [month]) VALUES (@u, @y, @m);",
                "@u", userId, "@y", year, "@m", month))
                cmd.ExecuteNonQuery();
        }
    }
}
