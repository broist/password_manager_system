-- ============================================================
-- Password Manager System – Adatbázis inicializálás
-- Verzió: 1.1.0-security-model
-- ============================================================

CREATE DATABASE IF NOT EXISTS password_manager
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE password_manager;

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- ----- ELLENŐRZŐ TÁBLA -----
CREATE TABLE IF NOT EXISTS _system_info (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    initialized_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version VARCHAR(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO _system_info (version)
VALUES ('1.1.0-security-model');

-- ----- 1. ROLES -----
CREATE TABLE roles (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    name VARCHAR(50) NOT NULL UNIQUE,
    ad_group_name VARCHAR(150) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    description TEXT NULL,

    level INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    INDEX idx_roles_name (name),
    INDEX idx_roles_ad_group_name (ad_group_name),
    INDEX idx_roles_level (level)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO roles (name, ad_group_name, display_name, description, level) VALUES
('ITAdmin',    'erp_kp_itadm',         'IT Administrator', 'Teljes hozzáférés minden funkcióhoz és minden jelszóhoz', 100),
('IT',         'erp_kp_it',            'IT Support',       'Jelszavak kezelése és RDP/SSH indítás engedélyezett bejegyzéseken', 75),
('Consultant', 'erp_kp_erpconsultant', 'ERP Tanácsadó',    'Csak az engedélyezett bejegyzésekhez fér hozzá', 50),
('Support',    'erp_kp_erpsupport',    'ERP Support',      'Korlátozott hozzáférés, csak kifejezetten megosztott bejegyzésekhez', 25);

-- ----- 2. USERS -----
CREATE TABLE users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    ad_username VARCHAR(150) NOT NULL UNIQUE,
    display_name VARCHAR(200) NULL,
    email VARCHAR(255) NULL,

    role_id BIGINT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    first_login_at TIMESTAMP NULL,
    last_login_at TIMESTAMP NULL,
    role_synced_at TIMESTAMP NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    INDEX idx_users_ad_username (ad_username),
    INDEX idx_users_role_id (role_id),
    INDEX idx_users_is_active (is_active),

    CONSTRAINT fk_users_roles
        FOREIGN KEY (role_id) REFERENCES roles(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----- 3. COMPANIES -----
CREATE TABLE companies (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    name VARCHAR(200) NOT NULL UNIQUE,
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_by_user_id BIGINT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    INDEX idx_companies_name (name),
    INDEX idx_companies_is_active (is_active),

    CONSTRAINT fk_companies_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----- 4. CREDENTIALS -----
CREATE TABLE credentials (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    company_id BIGINT NOT NULL,
    title VARCHAR(255) NOT NULL,

    encrypted_username BLOB NULL,
    username_iv VARBINARY(12) NULL,
    username_tag VARBINARY(16) NULL,

    encrypted_password BLOB NULL,
    password_iv VARBINARY(12) NULL,
    password_tag VARBINARY(16) NULL,

    connection_value VARCHAR(500) NULL,
    notes TEXT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_by_user_id BIGINT NOT NULL,
    updated_by_user_id BIGINT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_accessed_at TIMESTAMP NULL,

    INDEX idx_credentials_company_id (company_id),
    INDEX idx_credentials_title (title),
    INDEX idx_credentials_is_active (is_active),
    INDEX idx_credentials_created_by (created_by_user_id),

    CONSTRAINT fk_credentials_companies
        FOREIGN KEY (company_id) REFERENCES companies(id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_credentials_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES users(id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_credentials_updated_by
        FOREIGN KEY (updated_by_user_id) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----- 5. CREDENTIAL_ACCESS -----
CREATE TABLE credential_access (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    credential_id BIGINT NOT NULL,

    role_id BIGINT NULL,
    user_id BIGINT NULL,

    can_view BOOLEAN NOT NULL DEFAULT TRUE,
    can_write BOOLEAN NOT NULL DEFAULT FALSE,
    can_delete BOOLEAN NOT NULL DEFAULT FALSE,

    expires_at TIMESTAMP NULL,

    created_by_user_id BIGINT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_access_credential_id (credential_id),
    INDEX idx_access_role_id (role_id),
    INDEX idx_access_user_id (user_id),
    INDEX idx_access_expires_at (expires_at),

    UNIQUE KEY uk_access_credential_role (credential_id, role_id),
    UNIQUE KEY uk_access_credential_user (credential_id, user_id),

    CONSTRAINT fk_access_credentials
        FOREIGN KEY (credential_id) REFERENCES credentials(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_access_roles
        FOREIGN KEY (role_id) REFERENCES roles(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_access_users
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_access_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES users(id)
        ON DELETE SET NULL,

    CONSTRAINT chk_access_role_or_user
        CHECK (
            (role_id IS NOT NULL AND user_id IS NULL)
            OR
            (role_id IS NULL AND user_id IS NOT NULL)
        )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ----- 6. AUDIT_LOG -----
CREATE TABLE audit_log (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    user_id BIGINT NULL,
    ad_username VARCHAR(150) NOT NULL,

    action VARCHAR(100) NOT NULL,
    target_type VARCHAR(100) NULL,
    target_id BIGINT NULL,

    credential_id BIGINT NULL,
    company_id BIGINT NULL,
    target_user_id BIGINT NULL,

    ip_address VARCHAR(45) NULL,
    user_agent VARCHAR(500) NULL,

    success BOOLEAN NOT NULL DEFAULT TRUE,
    details TEXT NULL,

    previous_hash CHAR(64) NULL,
    hash CHAR(64) NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_audit_user_id (user_id),
    INDEX idx_audit_ad_username (ad_username),
    INDEX idx_audit_action (action),
    INDEX idx_audit_target (target_type, target_id),
    INDEX idx_audit_credential_id (credential_id),
    INDEX idx_audit_company_id (company_id),
    INDEX idx_audit_created_at (created_at),

    CONSTRAINT fk_audit_users
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE SET NULL,

    CONSTRAINT fk_audit_credentials
        FOREIGN KEY (credential_id) REFERENCES credentials(id)
        ON DELETE SET NULL,

    CONSTRAINT fk_audit_companies
        FOREIGN KEY (company_id) REFERENCES companies(id)
        ON DELETE SET NULL,

    CONSTRAINT fk_audit_target_users
        FOREIGN KEY (target_user_id) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;