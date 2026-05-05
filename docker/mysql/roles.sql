-- ============================================================
-- 1. ROLES tábla (AD csoport-szintű szerepkörök)
-- ============================================================
CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ad_group_name VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    level INT NOT NULL DEFAULT 0 COMMENT 'Magasabb szám = több jog (rangsoroláshoz)',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ad_group (ad_group_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AD csoportokhoz tartozó szerepkörök
INSERT INTO roles (ad_group_name, display_name, description, level) VALUES
('erp_kp_itadm',         'IT Administrator',  'Teljes hozzáférés minden funkcióhoz és minden jelszóhoz',          100),
('erp_kp_it',            'IT Support',        'Jelszavak kezelése és RDP indítás minden bejegyzésen',              75),
('erp_kp_erpconsultant', 'ERP Tanácsadó',     'Csak az engedélyezett bejegyzésekhez fér hozzá',                    50),
('erp_kp_erpsupport',    'ERP Support',       'Korlátozott hozzáférés, csak kifejezetten megosztott bejegyzésekhez', 25);


