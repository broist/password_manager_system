CREATE TABLE IF NOT EXISTS credential_usage_sessions (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    credential_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    ad_username VARCHAR(255) NOT NULL,
    connection_value VARCHAR(1000) NULL,
    process_id INT NULL,
    started_at DATETIME NOT NULL,
    ended_at DATETIME NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',

    INDEX idx_usage_credential_status (credential_id, status),
    INDEX idx_usage_user_id (user_id),
    INDEX idx_usage_started_at (started_at),

    CONSTRAINT fk_usage_credential
        FOREIGN KEY (credential_id) REFERENCES credentials(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_usage_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
);