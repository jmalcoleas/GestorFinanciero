using System;

namespace GestorFinancieroApp.Data
{
    internal sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }

    internal sealed class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public override string ToString() { return Name; }
    }

    internal sealed class TransactionRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Type { get; set; }
        public bool IsIncome { get { return Type == "income"; } }
    }

    internal sealed class BudgetRow
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining { get { return Amount - Spent; } }
        public double Ratio { get { return Amount > 0 ? (double)(Spent / Amount) : 0; } }
    }
}
