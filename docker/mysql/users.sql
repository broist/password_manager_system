CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ad_username VARCHAR(100) NOT NULL UNIQUE COMMENT 'AD sAMAccountName, pl. biroi',
    display_name VARCHAR(200) NOT NULL COMMENT 'Megjelenítendő név, pl. Biró István',
    email VARCHAR(255) NULL COMMENT 'AD-ből betöltött email cím',
    role_id INT NOT NULL COMMENT 'Aktuális szerepkör (legmagasabb level alapján)',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    first_login_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_ad_username (ad_username),
    INDEX idx_role (role_id),
    
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;