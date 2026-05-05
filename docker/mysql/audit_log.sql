CREATE TABLE audit_log (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL COMMENT 'Ki csinálta (NULL ha sikertelen login)',
    action VARCHAR(50) NOT NULL COMMENT 'Művelet típusa',
    credential_id INT NULL COMMENT 'Melyik bejegyzéssel (ha releváns)',
    company_id INT NULL COMMENT 'Melyik céggel (ha releváns)',
    target_user_id INT NULL COMMENT 'Másik user ha admin művelet (pl. szerepkör módosítás)',
    
    -- Részletek
    description TEXT NULL COMMENT 'Részletes leírás (opcionális)',
    ip_address VARCHAR(45) NULL COMMENT 'IPv4/IPv6 cím',
    user_agent VARCHAR(255) NULL COMMENT 'Kliens info (program verzió, OS)',
    success BOOLEAN NOT NULL DEFAULT TRUE,
    
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_user (user_id),
    INDEX idx_action (action),
    INDEX idx_credential (credential_id),
    INDEX idx_created_at (created_at),
    
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    FOREIGN KEY (credential_id) REFERENCES credentials(id) ON DELETE SET NULL,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL,
    FOREIGN KEY (target_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;