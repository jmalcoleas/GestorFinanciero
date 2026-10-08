-- Esquema SQLite que la aplicación crea la primera vez que arranca (datos\gestor.db).
-- Es idempotente: se puede ejecutar varias veces. Fechas como texto ISO (yyyy-MM-dd).

CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE COLLATE NOCASE,
    password_hash TEXT NOT NULL,
    initial_balance REAL NOT NULL DEFAULT 0,   -- dinero en cuenta al empezar
    initial_savings REAL NOT NULL DEFAULT 0,   -- ahorros al empezar
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    type TEXT NOT NULL CHECK (type IN ('income', 'expense')),
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    amount REAL NOT NULL,
    savings_amount REAL NOT NULL DEFAULT 0,    -- parte de un ingreso que va a ahorros
    date TEXT NOT NULL,
    description TEXT,
    category_id INTEGER NOT NULL REFERENCES categories(id),
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    CHECK (savings_amount >= 0 AND savings_amount <= amount)
);

CREATE TABLE IF NOT EXISTS budgets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category_id INTEGER NOT NULL REFERENCES categories(id),
    [year] INTEGER NOT NULL,
    [month] INTEGER NOT NULL CHECK ([month] BETWEEN 1 AND 12),
    amount REAL NOT NULL CHECK (amount > 0),
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, category_id, [year], [month])
);

CREATE TABLE IF NOT EXISTS month_celebrations (
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    [year] INTEGER NOT NULL,
    [month] INTEGER NOT NULL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, [year], [month])
);

CREATE TABLE IF NOT EXISTS income_prompt_skips (
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    [year] INTEGER NOT NULL,
    [month] INTEGER NOT NULL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, [year], [month])
);

CREATE INDEX IF NOT EXISTS IX_transactions_user_date ON transactions (user_id, date);
