# Personal Finance Manager (Gestor Financiero)

A portable desktop application built with **C# (Windows Forms)** and **SQLite** to manage personal finances: record income and expenses, split each month's income between savings and spending, set budgets per category, and see at a glance whether the month is going well.

> **Project status:** Working and in daily personal use. Next steps are listed in the [Roadmap](#roadmap).

## Download

**[⬇ Download the latest version (Windows)](https://github.com/jmalcoleas/GestorFinanciero/releases/latest)**: get `GestorFinanciero_portable.zip` from the *Assets* section.

1. Extract the **whole folder** from the zip (right-click → *Extract all*).
2. Run `GestorFinancieroApp.exe`.
3. Create your account on first launch.

Requires Windows 10 or 11. Nothing else to install. Windows may warn that the app is unsigned: choose *More info → Run anyway*.

## Why this project

Most finance apps track *what happened*. This one is built around a simple monthly routine:

1. Start the month by registering your main income.
2. Decide how much of it goes straight to **savings**.
3. The rest is what you can **spend** during the month, on dinners, cinema, video games or anything else.
4. Every expense is subtracted from your bank account and from the month's available money, so you always see where you stand.

## Features

- **Accounts with password hashing**: sign-up and login, passwords stored with PBKDF2-SHA256 and a random salt per user.
- **Starting balances**: when signing up you enter the money in your bank account and your savings; they can be corrected later.
- **Monthly main income**: at the start of each month the app asks for your income and how much goes to savings. If you skip it, it does not ask again that month.
- **Totals and monthly figures**: bank account and savings (accumulated across all months), plus income, savings, expenses and available money for the selected month.
- **Budgets by category**: reserve an amount for a destination (e.g. "Dinners with my partner: 55 €"). Each expense in that category is subtracted from it.
- **Traffic-light indicator**: green / amber / red per category and for the month overall, based on spending versus budget and versus how much of the month has passed.
- **Month-complete message**: when a finished month closes with every budget respected, the app congratulates you with a personalized message.
- **Month filter** and full management of movements (add, edit, delete).
- **Portable**: the data is a single file next to the executable, so the whole folder can be copied to another Windows PC.

## How the money works

| Concept | Rule |
|---|---|
| **Bank account** | Starting balance + (every income − its part sent to savings) − every expense |
| **Savings** | Starting savings + the savings part of every income |
| **Available this month** | Month income − month savings − month expenses |
| **Budget** | A spending limit per category and month. It does not move money; it only tracks how much of the limit is used |

Example: you start with 1,000 € in the account and 700 € saved. In October you register an income of 170 € and send 60 € to savings. Savings become 760 €, the account 1,110 € and 110 € are available this month. Spending 20 € on dinner leaves 1,090 € in the account and 90 € available.

**Indicator thresholds**
- *Category:* amber from 80 % of the budget, red when exceeded.
- *Month overall:* red if total spending exceeds the total budget or runs more than 25 points ahead of the month's progress; amber when it runs more than 10 points ahead.

## Tech stack

- **Language:** C# (.NET Framework 4.7.2)
- **UI:** Windows Forms, built in code (no designer files)
- **Database:** SQLite through `System.Data.SQLite`
- **IDE:** Visual Studio 2022

## Project structure

```
GestorFinanciero.sln
GestorFinancieroApp/
├── Data/        Database access (connection, repository, models)
├── Logic/       Business rules: traffic-light indicator, money formatting
├── Security/    Password hashing
├── UI/          Forms: login, main window, dialogs
├── database/    schema.sql (embedded SQLite schema) and the original SQL Server prototype
└── Program.cs   Entry point
```

## Getting started

**To use it:** see [Download](#download) above.

**To develop:**
1. Clone the repository:
   ```bash
   git clone https://github.com/jmalcoleas/GestorFinanciero.git
   ```
2. Open `GestorFinanciero.sln` in Visual Studio 2022 (NuGet restores the SQLite package automatically).
3. Build and run (F5). The database is created on first launch in `datos/gestor.db`.

## Your data

Everything is stored locally in `datos/gestor.db` next to the executable. To back up your data, copy that file. To move to another PC, copy the whole folder. Nothing is sent over the network.

## Roadmap

Ideas under consideration, not commitments:

- Android version, possibly in Kotlin
- Sharing the same data between PC and phone
- Charts: spending by category and monthly trend
- Export monthly reports to Excel
- Search and date-range filters for movements
- Unit tests for the business rules

## Author

**José María Alcolea Soriano**
Computer Engineering student
[LinkedIn](https://www.linkedin.com/in/jose-maria-alcolea-6244153b0/) · [GitHub](https://github.com/jmalcoleas)
