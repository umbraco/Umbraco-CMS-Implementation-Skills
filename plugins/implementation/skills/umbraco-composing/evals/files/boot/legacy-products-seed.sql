-- Eval 1's MySite.Core reads its SKU lookup from this table, which a fresh install doesn't have.
-- boot-check runs it between the install boot and the checked boot (--seed-sql).
CREATE TABLE legacySku (Sku TEXT NOT NULL, ProductKey TEXT NOT NULL);
