-- =========================================================================
-- SAMSTORE - PROFILE SERVICE DATABASE SCHEMA REDESIGN
-- Database Target: ss_profile_db (PostgreSQL)
-- Standard Conventions: snake_case, Pluralized Tables, Dual-Identities (Id/PublicId)
-- =========================================================================

BEGIN;

-- =========================================================================
-- 1. OPERATIONAL & MESSAGING TABLES (Inbox/Outbox Pattern)
-- =========================================================================

-- Outbox Events: Transactional Outbox Pattern for reliable event publishing
CREATE TABLE outbox_events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID DEFAULT gen_random_uuid() UNIQUE NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(50) DEFAULT 'PENDING' NOT NULL,
    retry_count INT DEFAULT 0 NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    published_at TIMESTAMPTZ,
    error_message TEXT,
    CONSTRAINT chk_outbox_status CHECK (status IN ('PENDING', 'PUBLISHED', 'FAILED'))
);

-- Inbox Events: Idempotent Event Consumer Pattern
CREATE TABLE inbox_events (
    message_id VARCHAR(255) PRIMARY KEY,
    event_type VARCHAR(255) NOT NULL,
    aggregate_type VARCHAR(100),
    payload JSONB NOT NULL,
    status VARCHAR(50) DEFAULT 'PROCESSED' NOT NULL,
    processed_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    error_message TEXT,
    CONSTRAINT chk_inbox_status CHECK (status IN ('PROCESSED', 'FAILED'))
);

-- =========================================================================
-- 2. DOMAIN TABLES
-- =========================================================================

-- User Profiles Table
CREATE TABLE user_profiles (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID DEFAULT gen_random_uuid() UNIQUE NOT NULL,
    
    -- Correlation mapping to SS-AuthService Users
    user_id INT UNIQUE NOT NULL,
    user_public_id UUID UNIQUE NOT NULL,
    
    -- Profile Data
    full_name VARCHAR(255) NOT NULL,
    phone_number VARCHAR(50),
    avatar_url VARCHAR(1000),
    bio VARCHAR(500),
    gender VARCHAR(20),
    date_of_birth DATE,
    
    -- Audit Trail Columns
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by VARCHAR(100) DEFAULT 'System' NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100),
    
    CONSTRAINT chk_gender CHECK (gender IN ('Male', 'Female', 'Other', 'PreferNotToSay'))
);

-- Shipping/Billing Addresses (Multi-address support per profile)
CREATE TABLE user_addresses (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID DEFAULT gen_random_uuid() UNIQUE NOT NULL,
    user_profile_id INT NOT NULL,
    
    -- Address Fields
    address_label VARCHAR(100) DEFAULT 'Home' NOT NULL, -- e.g., 'Home', 'Office', 'Apartment'
    receiver_name VARCHAR(255) NOT NULL,
    receiver_phone VARCHAR(50) NOT NULL,
    street_address VARCHAR(500) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_province VARCHAR(100) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    country VARCHAR(100) DEFAULT 'Indonesia' NOT NULL,
    latitude DECIMAL(9, 6),
    longitude DECIMAL(9, 6),
    is_default BOOLEAN DEFAULT FALSE NOT NULL,
    
    -- Audit Trail Columns
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by VARCHAR(100) DEFAULT 'System' NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- =========================================================================
-- 3. INDEXING STRATEGY
-- =========================================================================

-- Operational Indexing
CREATE INDEX idx_outbox_events_status_created ON outbox_events(status, created_at) WHERE status = 'PENDING';
CREATE INDEX idx_inbox_events_processed_at ON inbox_events(processed_at);

-- Domain Indexing
CREATE INDEX idx_user_profiles_user_id ON user_profiles(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_user_profiles_user_public_id ON user_profiles(user_public_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_user_addresses_profile_id ON user_addresses(user_profile_id) WHERE deleted_at IS NULL;

-- Default Address Constraint Index (only one default address per active profile)
CREATE UNIQUE INDEX uq_user_addresses_default_idx ON user_addresses (user_profile_id) 
    WHERE is_default = TRUE AND deleted_at IS NULL;

-- =========================================================================
-- 4. RELATIONSHIP CONSTRAINTS
-- =========================================================================
ALTER TABLE user_addresses ADD CONSTRAINT fk_user_addresses_user_profile
    FOREIGN KEY (user_profile_id) REFERENCES user_profiles(id) ON DELETE CASCADE;

-- =========================================================================
-- 5. DATABASE COMMENTS
-- =========================================================================
COMMENT ON TABLE user_profiles IS 'Stores comprehensive profile data linked to authentication users';
COMMENT ON COLUMN user_profiles.user_id IS 'Corresponds to internal primary key id from ss_auth_db.users';
COMMENT ON COLUMN user_profiles.user_public_id IS 'Corresponds to public_id UUID from ss_auth_db.users';
COMMENT ON TABLE user_addresses IS 'Allows multiple shipping and billing addresses per profile';

COMMIT;
