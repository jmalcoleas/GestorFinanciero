-- Esquema que la aplicación ejecuta al arrancar (es idempotente: se puede lanzar varias veces).
-- Los lotes se separan con líneas que contienen solo GO.

IF OBJECT_ID('dbo.users', 'U') IS NULL
CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);
GO

IF OBJECT_ID('dbo.categories', 'U') IS NULL
CREATE TABLE categories (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(10) NOT NULL CHECK (type IN ('income', 'expense')),
    user_id INT,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('dbo.transactions', 'U') IS NULL
CREATE TABLE transactions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    amount DECIMAL(10,2) NOT NULL,
    date DATE NOT NULL DEFAULT GETDATE(),
    description VARCHAR(255),
    category_id INT NOT NULL,
    user_id INT NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

-- Presupuesto de un mes para una categoría de gasto (p. ej. "Cenas": 55 EUR en octubre).
IF OBJECT_ID('dbo.budgets', 'U') IS NULL
CREATE TABLE budgets (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    category_id INT NOT NULL,
    [year] INT NOT NULL,
    [month] INT NOT NULL CHECK ([month] BETWEEN 1 AND 12),
    amount DECIMAL(10,2) NOT NULL CHECK (amount > 0),
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_budgets UNIQUE (user_id, category_id, [year], [month]),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);
GO

-- Meses ya felicitados, para no repetir el mensaje de enhorabuena.
IF OBJECT_ID('dbo.month_celebrations', 'U') IS NULL
CREATE TABLE month_celebrations (
    user_id INT NOT NULL,
    [year] INT NOT NULL,
    [month] INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (user_id, [year], [month]),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_transactions_user_date' AND object_id = OBJECT_ID('dbo.transactions'))
CREATE INDEX IX_transactions_user_date ON transactions (user_id, date);
GO
