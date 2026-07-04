-- ==================  --
-- GESTIÓN FINANCIERA  --
-- ==================  --
CREATE DATABASE GestionFinanciera;
GO
USE GestionFinanciera;
GO

-- TABLA USUARIOS
CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);

-- TABLA CATEGORIAS
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

-- TABLA TRANSACCIONES
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

-- ==================  --
-- DATOS DE PRUEBA     --
-- ==================  --
INSERT INTO users (name, email, password_hash)
VALUES ('Jose', 'jose@email.com', 'hash_provisional');

INSERT INTO categories (name, type, user_id) VALUES ('Alimentación', 'expense', 1);
INSERT INTO categories (name, type, user_id) VALUES ('Transporte', 'expense', 1);
INSERT INTO categories (name, type, user_id) VALUES ('Ocio', 'expense', 1);
INSERT INTO categories (name, type, user_id) VALUES ('Salario', 'income', 1);
INSERT INTO categories (name, type, user_id) VALUES ('Freelance', 'income', 1);

INSERT INTO transactions (amount, date, description, category_id, user_id) VALUES (45.50, '2026-02-01', 'Compra supermercado', 1, 1);
INSERT INTO transactions (amount, date, description, category_id, user_id) VALUES (30.00, '2026-02-05', 'Gasolina', 2, 1);
INSERT INTO transactions (amount, date, description, category_id, user_id) VALUES (12.99, '2026-02-10', 'Netflix', 3, 1);
INSERT INTO transactions (amount, date, description, category_id, user_id) VALUES (1200.00, '2026-02-01', 'Nómina febrero', 4, 1);
INSERT INTO transactions (amount, date, description, category_id, user_id) VALUES (250.00, '2026-02-15', 'Proyecto web', 5, 1);
GO

-- ==================  --
-- CONSULTAS           --
-- ==================  --

-- CONSULTA: Ver transacciones con su categoría
SELECT 
    t.description,
    t.amount,
    c.name AS categoria,
    c.type AS tipo,
    t.date
FROM transactions t
JOIN categories c ON t.category_id = c.id
WHERE t.user_id = 1;

-- CONSULTA: Balance mensual (ingresos, gastos y balance)
SELECT
    SUM(CASE WHEN c.type = 'income' THEN t.amount ELSE 0 END) AS total_ingresos,
    SUM(CASE WHEN c.type = 'expense' THEN t.amount ELSE 0 END) AS total_gastos,
    SUM(CASE WHEN c.type = 'income' THEN t.amount ELSE 0 END)
        - SUM(CASE WHEN c.type = 'expense' THEN t.amount ELSE 0 END) AS balance
FROM transactions t
JOIN categories c ON t.category_id = c.id
WHERE t.user_id = 1
    AND MONTH(t.date) = 2
    AND YEAR(t.date) = 2026;

-- CONSULTA: Gasto total por categoría (de mayor a menor)
SELECT
    c.name AS categoria,
    SUM(t.amount) AS total_gastado
FROM transactions t
JOIN categories c ON t.category_id = c.id
WHERE t.user_id = 1
    AND c.type = 'expense'
    AND MONTH(t.date) = 2
    AND YEAR(t.date) = 2026
-- GROUP BY agrupa los resultados por categoría
GROUP BY c.name
-- ORDER BY DESC ordena de mayor a menor
ORDER BY total_gastado DESC;