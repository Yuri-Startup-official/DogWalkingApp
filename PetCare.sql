BEGIN;

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $$ BEGIN
    CREATE TYPE pet_species AS ENUM (
        'dog', 'cat', 'bird', 'rodent', 'reptile', 'fish', 'other'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE user_role AS ENUM ('owner', 'sitter', 'both');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE service_type AS ENUM ('walking', 'boarding');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE order_status AS ENUM (
        'pending', 'accepted', 'in_progress',
        'completed', 'cancelled', 'disputed'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE reviewer_role AS ENUM ('owner', 'sitter');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ПОЛЬЗОВАТЕЛИ

CREATE TABLE IF NOT EXISTS users (
    id                  BIGSERIAL PRIMARY KEY,
    public_id           UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    email               VARCHAR(255) UNIQUE NOT NULL,
    phone               VARCHAR(30) UNIQUE,
    password_hash       VARCHAR(255) NOT NULL,
    name                VARCHAR(100) NOT NULL,
    avatar_url          TEXT,
    role                user_role NOT NULL DEFAULT 'owner',
    email_verified      BOOLEAN DEFAULT false,
    phone_verified      BOOLEAN DEFAULT false,
    rating_as_sitter    NUMERIC(3,2) DEFAULT 0,
    reviews_as_sitter   INT DEFAULT 0,
    rating_as_owner     NUMERIC(3,2) DEFAULT 0,
    reviews_as_owner    INT DEFAULT 0,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW(),
    deleted_at          TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE deleted_at IS NULL;

-- ПРОФИЛИ

CREATE TABLE IF NOT EXISTS sitter_profiles (
    user_id             BIGINT PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    bio                 TEXT,

    offers_walking      BOOLEAN DEFAULT false,
    walking_hourly_rate NUMERIC(10,2),
    walking_radius_m    INT DEFAULT 5000,
    max_dogs_per_walk   INT DEFAULT 1,

    offers_boarding     BOOLEAN DEFAULT false,
    boarding_daily_rate NUMERIC(10,2),
    max_pets            INT DEFAULT 1,

    accepts_dogs        BOOLEAN DEFAULT false,
    accepts_cats        BOOLEAN DEFAULT false,
    accepts_birds       BOOLEAN DEFAULT false,
    accepts_rodents     BOOLEAN DEFAULT false,
    accepts_reptiles    BOOLEAN DEFAULT false,
    accepts_fish        BOOLEAN DEFAULT false,
    accepts_other       BOOLEAN DEFAULT false,

    has_yard            BOOLEAN DEFAULT false,
    has_own_pets        BOOLEAN DEFAULT false,
    own_pets_notes      TEXT,
    has_children        BOOLEAN DEFAULT false,
    can_give_meds       BOOLEAN DEFAULT false,
    can_handle_special  BOOLEAN DEFAULT false,

    id_verified         BOOLEAN DEFAULT false,
    verified_at         TIMESTAMPTZ,

    location            GEOGRAPHY(POINT, 4326),
    address_text        TEXT,

    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sitter_location ON sitter_profiles USING GIST(location);
CREATE INDEX IF NOT EXISTS idx_sitter_walking  ON sitter_profiles(offers_walking)  WHERE offers_walking;
CREATE INDEX IF NOT EXISTS idx_sitter_boarding ON sitter_profiles(offers_boarding) WHERE offers_boarding;

CREATE TABLE IF NOT EXISTS owner_profiles (
    user_id         BIGINT PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    bio             TEXT,
    location        GEOGRAPHY(POINT, 4326),
    address_text    TEXT,
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_owner_location ON owner_profiles USING GIST(location);

-- ПИТОМЦЫ

CREATE TABLE IF NOT EXISTS pets (
    id                  BIGSERIAL PRIMARY KEY,
    public_id           UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    owner_id            BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name                VARCHAR(100) NOT NULL,
    species             pet_species NOT NULL,
    breed               VARCHAR(100),
    birth_date          DATE,
    weight_kg           NUMERIC(5,2),
    sex                 VARCHAR(10),
    sterilized          BOOLEAN DEFAULT false,
    photo_url           TEXT,
    medical_notes       TEXT,
    medications         TEXT,
    vaccinated          BOOLEAN DEFAULT false,
    vaccination_expires DATE,
    behavior_notes      TEXT,
    aggression_level    SMALLINT DEFAULT 0 CHECK (aggression_level BETWEEN 0 AND 3),
    special_needs       TEXT,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    deleted_at          TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_pets_owner ON pets(owner_id) WHERE deleted_at IS NULL;

-- ЗАКАЗЫ

CREATE TABLE IF NOT EXISTS orders (
    id                  BIGSERIAL PRIMARY KEY,
    public_id           UUID NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
    service_type        service_type NOT NULL,
    owner_id            BIGINT NOT NULL REFERENCES users(id),
    sitter_id           BIGINT REFERENCES users(id),
    status              order_status NOT NULL DEFAULT 'pending',
    total_price         NUMERIC(10,2),
    platform_fee        NUMERIC(10,2),
    payment_intent_id   TEXT,
    payment_status      VARCHAR(20) DEFAULT 'pending',
    paid_at             TIMESTAMPTZ,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW(),
    cancelled_at        TIMESTAMPTZ,
    cancel_reason       TEXT
);

CREATE INDEX IF NOT EXISTS idx_orders_owner  ON orders(owner_id);
CREATE INDEX IF NOT EXISTS idx_orders_sitter ON orders(sitter_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);

CREATE TABLE IF NOT EXISTS walking_orders (
    order_id        BIGINT PRIMARY KEY REFERENCES orders(id) ON DELETE CASCADE,
    start_time      TIMESTAMPTZ NOT NULL,
    duration_min    INT NOT NULL CHECK (duration_min > 0),
    meeting_point   GEOGRAPHY(POINT, 4326),
    pets_count      INT NOT NULL DEFAULT 1 CHECK (pets_count > 0),
    started_at      TIMESTAMPTZ,
    finished_at     TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS boarding_orders (
    order_id         BIGINT PRIMARY KEY REFERENCES orders(id) ON DELETE CASCADE,
    check_in         DATE NOT NULL,
    check_out        DATE NOT NULL,
    pets_count       INT NOT NULL DEFAULT 1 CHECK (pets_count > 0),
    actual_check_in  TIMESTAMPTZ,
    actual_check_out TIMESTAMPTZ,
    CONSTRAINT valid_boarding_dates CHECK (check_out > check_in)
);

CREATE INDEX IF NOT EXISTS idx_boarding_dates ON boarding_orders
    USING GIST (daterange(check_in, check_out, '[)'));

CREATE TABLE IF NOT EXISTS order_pets (
    order_id    BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    pet_id      BIGINT NOT NULL REFERENCES pets(id) ON DELETE RESTRICT,
    PRIMARY KEY (order_id, pet_id)
);

CREATE INDEX IF NOT EXISTS idx_order_pets_pet ON order_pets(pet_id);

-- ДОСТУПНОСТЬ

CREATE TABLE IF NOT EXISTS availability_blocks (
    id          BIGSERIAL PRIMARY KEY,
    sitter_id   BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_date  DATE NOT NULL,
    end_date    DATE NOT NULL,
    reason      TEXT,
    CONSTRAINT valid_block CHECK (end_date > start_date)
);

CREATE INDEX IF NOT EXISTS idx_blocks_sitter ON availability_blocks
    USING GIST (sitter_id, daterange(start_date, end_date, '[)'));

-- ОТЗЫВЫ + РЕЙТИНГ

CREATE TABLE IF NOT EXISTS reviews (
    id                  BIGSERIAL PRIMARY KEY,
    order_id            BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    reviewer_id         BIGINT NOT NULL REFERENCES users(id),
    reviewee_id         BIGINT NOT NULL REFERENCES users(id),
    reviewer_role       reviewer_role NOT NULL,
    rating              SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment             TEXT,
    punctuality         SMALLINT CHECK (punctuality BETWEEN 1 AND 5),
    care_quality        SMALLINT CHECK (care_quality BETWEEN 1 AND 5),
    communication       SMALLINT CHECK (communication BETWEEN 1 AND 5),
    cleanliness         SMALLINT CHECK (cleanliness BETWEEN 1 AND 5),
    pet_behavior        SMALLINT CHECK (pet_behavior BETWEEN 1 AND 5),
    owner_communication SMALLINT CHECK (owner_communication BETWEEN 1 AND 5),
    payment_punctuality SMALLINT CHECK (payment_punctuality BETWEEN 1 AND 5),
    is_public           BOOLEAN DEFAULT true,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (order_id, reviewer_role),
    CONSTRAINT no_self_review CHECK (reviewer_id <> reviewee_id)
);

CREATE INDEX IF NOT EXISTS idx_reviews_reviewee ON reviews(reviewee_id);
CREATE INDEX IF NOT EXISTS idx_reviews_order    ON reviews(order_id);

-- ФОТО-ОТЧЁТЫ, ПРИЁМ ПРЕТЕНЗИЙ, ЧАТ

CREATE TABLE IF NOT EXISTS order_updates (
    id          BIGSERIAL PRIMARY KEY,
    order_id    BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    author_id   BIGINT NOT NULL REFERENCES users(id),
    text        TEXT,
    media_url   TEXT,
    location    GEOGRAPHY(POINT, 4326),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_updates_order ON order_updates(order_id);

CREATE TABLE IF NOT EXISTS incidents (
    id          BIGSERIAL PRIMARY KEY,
    order_id    BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    reported_by BIGINT NOT NULL REFERENCES users(id),
    type        VARCHAR(30) NOT NULL,
    description TEXT,
    media_url   TEXT,
    resolved    BOOLEAN DEFAULT false,
    resolution  TEXT,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_incidents_order ON incidents(order_id);

CREATE TABLE IF NOT EXISTS messages (
    id              BIGSERIAL PRIMARY KEY,
    order_id        BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    sender_id       BIGINT NOT NULL REFERENCES users(id),

    message_type    VARCHAR(20) DEFAULT 'text',

    text            TEXT,
    media_url       TEXT,
    media_metadata  JSONB,

    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT valid_message_content CHECK (
        (message_type = 'text' AND text IS NOT NULL) OR
        (message_type IN ('image', 'video') AND media_url IS NOT NULL) OR
        (message_type = 'system')
    )
);
CREATE INDEX IF NOT EXISTS idx_messages_order ON messages(order_id, created_at);

-- КНИГА КОНТАКТОВ

CREATE TABLE IF NOT EXISTS user_contacts (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    contact_id      BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    is_favorite     BOOLEAN DEFAULT true,
    is_mutual       BOOLEAN DEFAULT false,

    note            TEXT,

    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT no_self_contact CHECK (user_id <> contact_id),
    UNIQUE (user_id, contact_id)
);

CREATE INDEX IF NOT EXISTS idx_contacts_user    ON user_contacts(user_id);
CREATE INDEX IF NOT EXISTS idx_contacts_contact ON user_contacts(contact_id);

-- ПЕРЕСЧЁТ РЕЙТИНГА

CREATE OR REPLACE FUNCTION refresh_user_ratings()
RETURNS TRIGGER AS $$
DECLARE
    v_reviewee BIGINT;
BEGIN
    v_reviewee := COALESCE(NEW.reviewee_id, OLD.reviewee_id);

    UPDATE users u SET
        rating_as_sitter = COALESCE((
            SELECT ROUND(AVG(rating)::numeric, 2) FROM reviews
            WHERE reviewee_id = v_reviewee AND reviewer_role = 'owner'
        ), 0),
        reviews_as_sitter = (
            SELECT COUNT(*) FROM reviews
            WHERE reviewee_id = v_reviewee AND reviewer_role = 'owner'
        ),
        rating_as_owner = COALESCE((
            SELECT ROUND(AVG(rating)::numeric, 2) FROM reviews
            WHERE reviewee_id = v_reviewee AND reviewer_role = 'sitter'
        ), 0),
        reviews_as_owner = (
            SELECT COUNT(*) FROM reviews
            WHERE reviewee_id = v_reviewee AND reviewer_role = 'sitter'
        ),
        updated_at = NOW()
    WHERE u.id = v_reviewee;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_refresh_ratings ON reviews;
CREATE TRIGGER trg_refresh_ratings
AFTER INSERT OR UPDATE OR DELETE ON reviews
FOR EACH ROW EXECUTE FUNCTION refresh_user_ratings();

-- ПРОВЕРКА ВМЕСТИМОСТИ ПЕРЕДЕРЖКИ

CREATE OR REPLACE FUNCTION can_book_boarding(
    p_sitter_id   BIGINT,
    p_check_in    DATE,
    p_check_out   DATE,
    p_new_pets    INT
) RETURNS BOOLEAN AS $$
DECLARE
    v_capacity   INT;
    v_blocked    BOOLEAN;
    v_peak       INT;
BEGIN
    SELECT max_pets INTO v_capacity
    FROM sitter_profiles
    WHERE user_id = p_sitter_id
    FOR UPDATE;

    IF v_capacity IS NULL THEN
        RETURN false;
    END IF;

    SELECT EXISTS (
        SELECT 1 FROM availability_blocks
        WHERE sitter_id = p_sitter_id
          AND daterange(start_date, end_date, '[)') &&
              daterange(p_check_in, p_check_out, '[)')
    ) INTO v_blocked;

    IF v_blocked THEN
        RETURN false;
    END IF;

    WITH events AS (
        SELECT bo.check_in  AS day,  bo.pets_count AS delta
        FROM boarding_orders bo
        JOIN orders o ON o.id = bo.order_id
        WHERE o.sitter_id = p_sitter_id
          AND o.status IN ('accepted', 'in_progress')
          AND daterange(bo.check_in, bo.check_out, '[)') &&
              daterange(p_check_in, p_check_out, '[)')
        UNION ALL
        SELECT bo.check_out, -bo.pets_count
        FROM boarding_orders bo
        JOIN orders o ON o.id = bo.order_id
        WHERE o.sitter_id = p_sitter_id
          AND o.status IN ('accepted', 'in_progress')
          AND daterange(bo.check_in, bo.check_out, '[)') &&
              daterange(p_check_in, p_check_out, '[)')
        UNION ALL SELECT p_check_in,  p_new_pets
        UNION ALL SELECT p_check_out, -p_new_pets
    )
    SELECT COALESCE(MAX(running), 0) INTO v_peak
    FROM (
        SELECT SUM(delta) OVER (ORDER BY day, delta DESC) AS running
        FROM events
    ) t;

    RETURN v_peak <= v_capacity;
END;
$$ LANGUAGE plpgsql;

-- АПГРЕЙД: автоматическое обновление updated_at


CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_updated ON users;
CREATE TRIGGER trg_users_updated
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_orders_updated ON orders;
CREATE TRIGGER trg_orders_updated
BEFORE UPDATE ON orders
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_sitter_profiles_updated ON sitter_profiles;
CREATE TRIGGER trg_sitter_profiles_updated
BEFORE UPDATE ON sitter_profiles
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_owner_profiles_updated ON owner_profiles;
CREATE TRIGGER trg_owner_profiles_updated
BEFORE UPDATE ON owner_profiles
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_user_contacts_updated ON user_contacts;
CREATE TRIGGER trg_user_contacts_updated
BEFORE UPDATE ON user_contacts
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

COMMIT;