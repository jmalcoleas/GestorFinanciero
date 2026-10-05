using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using GestorFinancieroApp.Data;

namespace GestorFinancieroApp.Logic
{
    internal enum Level { None, Good, Warning, Over }

    /// <summary>Reglas del semáforo de gastos y utilidades de formato.</summary>
    internal static class Indicator
    {
        public static readonly Color Good = Color.FromArgb(46, 160, 67);
        public static readonly Color Warn = Color.FromArgb(217, 130, 0);
        public static readonly Color Bad = Color.FromArgb(209, 52, 56);
        public static readonly Color Muted = Color.FromArgb(110, 118, 129);

        /// <summary>Por categoría: rojo si te pasas, ámbar si has gastado el 80 % o más.</summary>
        public static Level ForCategory(decimal budget, decimal spent)
        {
            if (spent > budget) return Level.Over;
            if (budget > 0 && spent >= budget * 0.8m) return Level.Warning;
            return Level.Good;
        }

        /// <summary>Qué fracción del mes "debería" llevarse gastada: 0 si no ha empezado, 1 si ya terminó.</summary>
        public static double ExpectedRatio(DateTime month, DateTime today)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            if (today.Date >= start.AddMonths(1)) return 1;
            if (today.Date < start) return 0;
            return today.Day / (double)DateTime.DaysInMonth(month.Year, month.Month);
        }

        public static bool IsClosed(DateTime month, DateTime today)
        {
            return today.Date >= new DateTime(month.Year, month.Month, 1).AddMonths(1);
        }

        /// <summary>Global del mes: compara lo gastado con el ritmo esperado, y con el total presupuestado.</summary>
        public static Level Overall(decimal budget, decimal spent, DateTime month, DateTime today)
        {
            if (budget <= 0) return Level.None;
            if (spent > budget) return Level.Over;
            double diff = (double)(spent / budget) - ExpectedRatio(month, today);
            if (diff > 0.25) return Level.Over;
            if (diff > 0.10) return Level.Warning;
            return Level.Good;
        }

        /// <summary>El mes se cumple si hay presupuestos y ninguno se ha excedido.</summary>
        public static bool MonthSuccess(IList<BudgetRow> budgets)
        {
            return budgets.Count > 0 && budgets.All(b => b.Spent <= b.Amount);
        }

        public static Color ColorOf(Level level)
        {
            switch (level)
            {
                case Level.Good: return Good;
                case Level.Warning: return Warn;
                case Level.Over: return Bad;
                default: return Muted;
            }
        }
    }

    internal static class Money
    {
        public static readonly CultureInfo Es = new CultureInfo("es-ES");
        public const decimal Max = 99999999.99m; // límite de DECIMAL(10,2)

        public static string Format(decimal v) { return v.ToString("C2", Es); }

        public static string MonthName(DateTime d)
        {
            string s = d.ToString("MMMM yyyy", Es);
            return char.ToUpper(s[0], Es) + s.Substring(1);
        }

        /// <summary>Acepta "55", "55,5", "55.5" y "1.200,50".</summary>
        public static bool TryParse(string text, out decimal value)
        {
            string s = (text ?? "").Replace("€", "").Trim();
            s = s.Contains(",") ? s.Replace(".", "") : s.Replace('.', ',');
            if (decimal.TryParse(s, NumberStyles.AllowDecimalPoint, Es, out value))
            {
                value = Math.Round(value, 2);
                return true;
            }
            return false;
        }
    }
}
