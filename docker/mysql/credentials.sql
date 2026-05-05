CREATE TABLE credentials (
    id INT AUTO_INCREMENT PRIMARY KEY,
    company_id INT NOT NULL COMMENT 'Melyik partnercéghez tartozik',
    
    -- A felhasználó által kitöltendő mezők
    title VARCHAR(255) NOT NULL COMMENT 'Cím (kötelező)',
    username VARCHAR(255) NULL COMMENT 'Felhasználónév (opcionális)',
    connection VARCHAR(500) NULL COMMENT 'URL vagy RDP útvonal (opcionális)',
    notes TEXT NULL COMMENT 'Megjegyzés (opcionális)',
    
    -- Titkosított jelszó (zero-knowledge: csak a kliens tudja visszafejteni)
    encrypted_password BLOB NULL COMMENT 'AES-256-GCM titkosított jelszó',
    encryption_iv VARBINARY(12) NULL COMMENT 'GCM nonce (12 byte)',
    encryption_tag VARBINARY(16) NULL COMMENT 'GCM auth tag (16 byte)',
    
    -- Metaadatok
    created_by_user_id INT NOT NULL,
    updated_by_user_id INT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_accessed_at TIMESTAMP NULL COMMENT 'Mikor nézte meg utoljára valaki',
    
    INDEX idx_company (company_id),
    INDEX idx_title (title),
    INDEX idx_created_by (created_by_user_id),
    
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE RESTRICT,
    FOREIGN KEY (created_by_user_id) REFERENCES users(id) ON DELETE RESTRICT,
    FOREIGN KEY (updated_by_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;