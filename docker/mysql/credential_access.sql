CREATE TABLE credential_access (
    id INT AUTO_INCREMENT PRIMARY KEY,
    credential_id INT NOT NULL,
    role_id INT NOT NULL,
    can_read BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Láthatja a bejegyzést',
    can_write BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Módosíthatja a bejegyzést',
    can_delete BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Törölheti a bejegyzést',
    granted_by_user_id INT NULL,
    granted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_credential_role (credential_id, role_id),
    INDEX idx_credential (credential_id),
    INDEX idx_role (role_id),
    
    FOREIGN KEY (credential_id) REFERENCES credentials(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (granted_by_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;