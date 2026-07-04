# Personal Finance Manager (Gestor Financiero)

A desktop application built with **C# (Windows Forms)** and **SQL Server** to manage personal finances — track income and expenses by category and keep an eye on your monthly balance.

> **Project status:** In active development. Core database is complete; application features are being built incrementally.

## Overview

This project is a personal finance manager designed as a practical way to apply relational database design and desktop application development. It allows a user to record their income and expenses, organize them by category, and review a monthly summary of where their money goes.

## Tech Stack

- **Language:** C#
- **Framework:** .NET Framework (Windows Forms)
- **Database:** Microsoft SQL Server
- **IDE:** Visual Studio 2022

## Features

**Implemented**
- Normalized relational database (users, categories, transactions)
- Sample data and core SQL queries (monthly balance, spending by category)

**Planned**
- User login and authentication
- Add, edit, and delete income and expense records
- Category management (income / expense)
- Monthly balance overview (income − expenses)
- Export monthly reports to Excel

## Database Design

The database follows a normalized relational model with three main tables:

| Table | Description |
|-------|-------------|
| `users` | Application users, with soft-delete support (`is_active`) |
| `categories` | Income/expense categories, linked to a user |
| `transactions` | Individual financial movements, linked to a category and user |

Key design choices:
- Foreign keys with `ON DELETE CASCADE` to keep data consistent
- `CHECK` constraint to ensure category types are only `income` or `expense`
- Soft-delete pattern (`is_active`) instead of physically removing rows
- Audit columns (`created_at`, `updated_at`) on every table

The full schema and sample queries are available in the [`database`](WindowsFormsApp1/database) folder.

## Getting Started

1. Clone the repository:
```bash
   git clone https://github.com/jmalcoleas/GestorFinanciero.git
```
2. Open `GestorFinanciero.sln` in Visual Studio 2022.
3. Run the SQL script in the `database` folder using SQL Server Management Studio to create the database.
4. Update the connection string in `App.config` to point to your SQL Server instance.
5. Build and run the project (F5).

## Future Improvements

- Visual charts (spending breakdown, monthly trends) using WinForms Chart controls
- Multi-user support with proper password hashing
- Filtering and searching transactions by date range

## Author

**José María Alcolea Soriano**
Computer Engineering student
[LinkedIn](https://www.linkedin.com/in/jose-maria-alcolea-6244153b0/) · [GitHub](https://github.com/jmalcoleas)
