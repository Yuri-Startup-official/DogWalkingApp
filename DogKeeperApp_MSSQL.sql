CREATE DATABASE DogWalkingApp;
GO
USE DogWalkingApp;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.users', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.users (
            id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            public_id           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            email               VARCHAR(255) NOT NULL,
            phone               VARCHAR(30) NULL,
            password_hash       VARCHAR(255) NOT NULL,
            name                VARCHAR(100) NOT NULL,
            avatar_url          NVARCHAR(MAX) NULL,
            role                VARCHAR(10) NOT NULL DEFAULT 'owner',
            email_verified      BIT NOT NULL DEFAULT 0,
            phone_verified      BIT NOT NULL DEFAULT 0,
            rating_as_sitter    DECIMAL(3,2) NOT NULL DEFAULT 0,
            reviews_as_sitter   INT NOT NULL DEFAULT 0,
            rating_as_owner     DECIMAL(3,2) NOT NULL DEFAULT 0,
            reviews_as_owner    INT NOT NULL DEFAULT 0,
            created_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            updated_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            deleted_at          DATETIME2 NULL,
            CONSTRAINT uq_users_public_id UNIQUE (public_id),
            CONSTRAINT uq_users_email UNIQUE (email),
            CONSTRAINT ck_users_role CHECK (role IN ('owner','sitter','both'))
        );
        CREATE INDEX idx_users_role ON dbo.users(role) WHERE deleted_at IS NULL;
    END

    -- Профили ситтеров
    IF OBJECT_ID(N'dbo.sitter_profiles', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.sitter_profiles (
            user_id             BIGINT NOT NULL PRIMARY KEY
                                REFERENCES dbo.users(id) ON DELETE CASCADE,
            bio                 NVARCHAR(MAX) NULL,
            offers_walking      BIT NOT NULL DEFAULT 0,
            walking_hourly_rate DECIMAL(10,2) NULL,
            walking_radius_m    INT NOT NULL DEFAULT 5000,
            max_dogs_per_walk   INT NOT NULL DEFAULT 1,
            offers_boarding     BIT NOT NULL DEFAULT 0,
            boarding_daily_rate DECIMAL(10,2) NULL,
            max_pets            INT NOT NULL DEFAULT 1,
            accepts_dogs        BIT NOT NULL DEFAULT 0,
            accepts_cats        BIT NOT NULL DEFAULT 0,
            accepts_birds       BIT NOT NULL DEFAULT 0,
            accepts_rodents     BIT NOT NULL DEFAULT 0,
            accepts_reptiles    BIT NOT NULL DEFAULT 0,
            accepts_fish        BIT NOT NULL DEFAULT 0,
            accepts_other       BIT NOT NULL DEFAULT 0,
            has_yard            BIT NOT NULL DEFAULT 0,
            has_own_pets        BIT NOT NULL DEFAULT 0,
            own_pets_notes      NVARCHAR(MAX) NULL,
            has_children        BIT NOT NULL DEFAULT 0,
            can_give_meds       BIT NOT NULL DEFAULT 0,
            can_handle_special  BIT NOT NULL DEFAULT 0,
            id_verified         BIT NOT NULL DEFAULT 0,
            verified_at         DATETIME2 NULL,
            -- [TEST SIMPLIFICATION] GEOGRAPHY(POINT,4326) → NVARCHAR
            location            NVARCHAR(100) NULL,
            address_text        NVARCHAR(MAX) NULL,
            updated_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
    END

    -- Профили пользователей
    IF OBJECT_ID(N'dbo.owner_profiles', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.owner_profiles (
            user_id         BIGINT NOT NULL PRIMARY KEY
                            REFERENCES dbo.users(id) ON DELETE CASCADE,
            bio             NVARCHAR(MAX) NULL,
            location        NVARCHAR(100) NULL, -- [TEST SIMPLIFICATION]
            address_text    NVARCHAR(MAX) NULL,
            updated_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
    END

    -- Реестр питомцев
    IF OBJECT_ID(N'dbo.pets', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.pets (
            id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            public_id           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            owner_id            BIGINT NOT NULL
                                REFERENCES dbo.users(id) ON DELETE CASCADE,
            name                VARCHAR(100) NOT NULL,
            species             VARCHAR(10) NOT NULL,
            breed               VARCHAR(100) NULL,
            birth_date          DATE NULL,
            weight_kg           DECIMAL(5,2) NULL,
            sex                 VARCHAR(10) NULL,
            sterilized          BIT NOT NULL DEFAULT 0,
            photo_url           NVARCHAR(MAX) NULL,
            medical_notes       NVARCHAR(MAX) NULL,
            medications         NVARCHAR(MAX) NULL,
            vaccinated          BIT NOT NULL DEFAULT 0,
            vaccination_expires DATE NULL,
            behavior_notes      NVARCHAR(MAX) NULL,
            aggression_level    SMALLINT NOT NULL DEFAULT 0,
            special_needs       NVARCHAR(MAX) NULL,
            created_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            deleted_at          DATETIME2 NULL,
            CONSTRAINT uq_pets_public_id UNIQUE (public_id),
            CONSTRAINT ck_pets_species CHECK (species IN
                ('dog','cat','bird','rodent','reptile','fish','other')),
            CONSTRAINT ck_pets_aggression CHECK (aggression_level BETWEEN 0 AND 3)
        );
        CREATE INDEX idx_pets_owner ON dbo.pets(owner_id) WHERE deleted_at IS NULL;
    END

    -- Заказы
    IF OBJECT_ID(N'dbo.orders', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.orders (
            id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            public_id           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            service_type        VARCHAR(10) NOT NULL,
            owner_id            BIGINT NOT NULL REFERENCES dbo.users(id),
            sitter_id           BIGINT NULL REFERENCES dbo.users(id),
            status              VARCHAR(20) NOT NULL DEFAULT 'pending',
            total_price         DECIMAL(10,2) NULL,
            platform_fee        DECIMAL(10,2) NULL,
            payment_intent_id   NVARCHAR(MAX) NULL,
            payment_status      VARCHAR(20) NOT NULL DEFAULT 'pending',
            paid_at             DATETIME2 NULL,
            created_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            updated_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            cancelled_at        DATETIME2 NULL,
            cancel_reason       NVARCHAR(MAX) NULL,
            CONSTRAINT uq_orders_public_id UNIQUE (public_id),
            CONSTRAINT ck_orders_service CHECK (service_type IN ('walking','boarding')),
            CONSTRAINT ck_orders_status CHECK (status IN
                ('pending','accepted','in_progress','completed','cancelled','disputed'))
        );
        CREATE INDEX idx_orders_owner  ON dbo.orders(owner_id);
        CREATE INDEX idx_orders_sitter ON dbo.orders(sitter_id);
        CREATE INDEX idx_orders_status ON dbo.orders(status);
    END

    -- Заказы прогулок
    IF OBJECT_ID(N'dbo.walking_orders', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.walking_orders (
            order_id        BIGINT NOT NULL PRIMARY KEY
                            REFERENCES dbo.orders(id) ON DELETE CASCADE,
            start_time      DATETIME2 NOT NULL,
            duration_min    INT NOT NULL,
            meeting_point   NVARCHAR(100) NULL, -- [TEST SIMPLIFICATION]
            pets_count      INT NOT NULL DEFAULT 1,
            started_at      DATETIME2 NULL,
            finished_at     DATETIME2 NULL,
            CONSTRAINT ck_walking_duration CHECK (duration_min > 0),
            CONSTRAINT ck_walking_pets CHECK (pets_count > 0)
        );
    END

    -- Заказы передержки
    IF OBJECT_ID(N'dbo.boarding_orders', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.boarding_orders (
            order_id         BIGINT NOT NULL PRIMARY KEY
                             REFERENCES dbo.orders(id) ON DELETE CASCADE,
            check_in         DATE NOT NULL,
            check_out        DATE NOT NULL,
            pets_count       INT NOT NULL DEFAULT 1,
            actual_check_in  DATETIME2 NULL,
            actual_check_out DATETIME2 NULL,
            CONSTRAINT ck_boarding_dates CHECK (check_out > check_in),
            CONSTRAINT ck_boarding_pets CHECK (pets_count > 0)
        );
        -- [TEST SIMPLIFICATION] GIST-индекс на daterange → обычный
        CREATE INDEX idx_boarding_dates ON dbo.boarding_orders(check_in, check_out);
    END

    -- Животные 
    IF OBJECT_ID(N'dbo.order_pets', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.order_pets (
            order_id    BIGINT NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
            pet_id      BIGINT NOT NULL REFERENCES dbo.pets(id),
            CONSTRAINT pk_order_pets PRIMARY KEY (order_id, pet_id)
        );
        CREATE INDEX idx_order_pets_pet ON dbo.order_pets(pet_id);
    END

    -- Доступность
    IF OBJECT_ID(N'dbo.availability_blocks', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.availability_blocks (
            id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            sitter_id   BIGINT NOT NULL REFERENCES dbo.users(id) ON DELETE CASCADE,
            start_date  DATE NOT NULL,
            end_date    DATE NOT NULL,
            reason      NVARCHAR(MAX) NULL,
            CONSTRAINT ck_block_dates CHECK (end_date > start_date)
        );
        CREATE INDEX idx_blocks_sitter ON dbo.availability_blocks(sitter_id, start_date, end_date);
    END

    -- Отзывы
    IF OBJECT_ID(N'dbo.reviews', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.reviews (
            id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            order_id            BIGINT NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
            reviewer_id         BIGINT NOT NULL REFERENCES dbo.users(id),
            reviewee_id         BIGINT NOT NULL REFERENCES dbo.users(id),
            reviewer_role       VARCHAR(10) NOT NULL,
            rating              SMALLINT NOT NULL,
            comment             NVARCHAR(MAX) NULL,
            punctuality         SMALLINT NULL,
            care_quality        SMALLINT NULL,
            communication       SMALLINT NULL,
            cleanliness         SMALLINT NULL,
            pet_behavior        SMALLINT NULL,
            owner_communication SMALLINT NULL,
            payment_punctuality SMALLINT NULL,
            is_public           BIT NOT NULL DEFAULT 1,
            created_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT uq_reviews_order_role UNIQUE (order_id, reviewer_role),
            CONSTRAINT ck_reviews_role CHECK (reviewer_role IN ('owner','sitter')),
            CONSTRAINT ck_reviews_rating CHECK (rating BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_punctuality CHECK (punctuality BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_care CHECK (care_quality BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_comm CHECK (communication BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_clean CHECK (cleanliness BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_pet CHECK (pet_behavior BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_owner_comm CHECK (owner_communication BETWEEN 1 AND 5),
            CONSTRAINT ck_reviews_payment CHECK (payment_punctuality BETWEEN 1 AND 5),
            CONSTRAINT ck_no_self_review CHECK (reviewer_id <> reviewee_id)
        );
        CREATE INDEX idx_reviews_reviewee ON dbo.reviews(reviewee_id);
        CREATE INDEX idx_reviews_order    ON dbo.reviews(order_id);
    END

    -- Обновление заказов
    IF OBJECT_ID(N'dbo.order_updates', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.order_updates (
            id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            order_id    BIGINT NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
            author_id   BIGINT NOT NULL REFERENCES dbo.users(id),
            text        NVARCHAR(MAX) NULL,
            media_url   NVARCHAR(MAX) NULL,
            location    NVARCHAR(100) NULL, -- [TEST SIMPLIFICATION]
            created_at  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
        CREATE INDEX idx_updates_order ON dbo.order_updates(order_id);
    END

    -- Индиденты
    IF OBJECT_ID(N'dbo.incidents', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.incidents (
            id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            order_id    BIGINT NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
            reported_by BIGINT NOT NULL REFERENCES dbo.users(id),
            type        VARCHAR(30) NOT NULL,
            description NVARCHAR(MAX) NULL,
            media_url   NVARCHAR(MAX) NULL,
            resolved    BIT NOT NULL DEFAULT 0,
            resolution  NVARCHAR(MAX) NULL,
            created_at  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        );
        CREATE INDEX idx_incidents_order ON dbo.incidents(order_id);
    END

    -- Чат заказчика и исполнителя
    IF OBJECT_ID(N'dbo.messages', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.messages (
            id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            order_id        BIGINT NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
            sender_id       BIGINT NOT NULL REFERENCES dbo.users(id),
            message_type    VARCHAR(20) NOT NULL DEFAULT 'text',
            text            NVARCHAR(MAX) NULL,
            media_url       NVARCHAR(MAX) NULL,
            media_metadata  NVARCHAR(MAX) NULL, -- [TEST SIMPLIFICATION] JSONB → NVARCHAR
            read_at         DATETIME2 NULL,
            created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT ck_messages_type CHECK (message_type IN ('text','image','video','system')),
            CONSTRAINT ck_messages_content CHECK (
                (message_type = 'text' AND text IS NOT NULL) OR
                (message_type IN ('image','video') AND media_url IS NOT NULL) OR
                (message_type = 'system')
            )
        );
        CREATE INDEX idx_messages_order ON dbo.messages(order_id, created_at);
    END

    -- Контакты пользователей
    IF OBJECT_ID(N'dbo.user_contacts', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.user_contacts (
            id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            user_id         BIGINT NOT NULL REFERENCES dbo.users(id) ON DELETE CASCADE,
            contact_id      BIGINT NOT NULL REFERENCES dbo.users(id) ON DELETE CASCADE,
            is_favorite     BIT NOT NULL DEFAULT 1,
            is_mutual       BIT NOT NULL DEFAULT 0,
            note            NVARCHAR(MAX) NULL,
            created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            updated_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            CONSTRAINT uq_contacts UNIQUE (user_id, contact_id),
            CONSTRAINT ck_no_self_contact CHECK (user_id <> contact_id)
        );
        CREATE INDEX idx_contacts_user    ON dbo.user_contacts(user_id);
        CREATE INDEX idx_contacts_contact ON dbo.user_contacts(contact_id);
    END

    COMMIT TRANSACTION;
    PRINT 'Tables created OK.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

-- ============================================================
-- ФУНКЦИИ И ТРИГГЕРЫ
-- ============================================================

-- ---------- set_updated_at ----------
CREATE OR ALTER FUNCTION dbo.fn_set_updated_at()
RETURNS DATETIME2
AS
BEGIN
    RETURN SYSUTCDATETIME();
END
GO

-- Триггер на users
CREATE OR ALTER TRIGGER dbo.trg_users_updated
ON dbo.users AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE u
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.users u
    INNER JOIN inserted i ON i.id = u.id;
END
GO

-- Триггер на orders
CREATE OR ALTER TRIGGER dbo.trg_orders_updated
ON dbo.orders AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE o
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.orders o
    INNER JOIN inserted i ON i.id = o.id;
END
GO

-- Триггер на sitter_profiles
CREATE OR ALTER TRIGGER dbo.trg_sitter_profiles_updated
ON dbo.sitter_profiles AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.sitter_profiles s
    INNER JOIN inserted i ON i.user_id = s.user_id;
END
GO

-- Триггер на owner_profiles
CREATE OR ALTER TRIGGER dbo.trg_owner_profiles_updated
ON dbo.owner_profiles AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE op
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.owner_profiles op
    INNER JOIN inserted i ON i.user_id = op.user_id;
END
GO

-- Триггер на user_contacts
CREATE OR ALTER TRIGGER dbo.trg_user_contacts_updated
ON dbo.user_contacts AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE uc
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.user_contacts uc
    INNER JOIN inserted i ON i.id = uc.id;
END
GO

-- ---------- refresh_user_ratings ----------
CREATE OR ALTER TRIGGER dbo.trg_refresh_ratings
ON dbo.reviews AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Собираем все затронутые reviewee_id
    DECLARE @affected TABLE (reviewee_id BIGINT PRIMARY KEY);

    INSERT INTO @affected (reviewee_id)
    SELECT reviewee_id FROM inserted
    UNION
    SELECT reviewee_id FROM deleted;

    -- Пересчитываем рейтинги для каждого затронутого пользователя
    UPDATE u
    SET
        rating_as_sitter = ISNULL((
            SELECT CAST(ROUND(AVG(CAST(r.rating AS DECIMAL(10,2))), 2) AS DECIMAL(3,2))
            FROM dbo.reviews r
            WHERE r.reviewee_id = u.id AND r.reviewer_role = 'owner'
        ), 0),
        reviews_as_sitter = (
            SELECT COUNT(*) FROM dbo.reviews r
            WHERE r.reviewee_id = u.id AND r.reviewer_role = 'owner'
        ),
        rating_as_owner = ISNULL((
            SELECT CAST(ROUND(AVG(CAST(r.rating AS DECIMAL(10,2))), 2) AS DECIMAL(3,2))
            FROM dbo.reviews r
            WHERE r.reviewee_id = u.id AND r.reviewer_role = 'sitter'
        ), 0),
        reviews_as_owner = (
            SELECT COUNT(*) FROM dbo.reviews r
            WHERE r.reviewee_id = u.id AND r.reviewer_role = 'sitter'
        ),
        updated_at = SYSUTCDATETIME()
    FROM dbo.users u
    INNER JOIN @affected a ON a.reviewee_id = u.id;
END
GO

-- ---------- can_book_boarding ----------
-- [TEST SIMPLIFICATION] daterange && → сравнение дат
CREATE OR ALTER FUNCTION dbo.can_book_boarding
(
    @p_sitter_id   BIGINT,
    @p_check_in    DATE,
    @p_check_out   DATE,
    @p_new_pets    INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @v_capacity INT;
    DECLARE @v_blocked  BIT = 0;
    DECLARE @v_peak     INT = 0;

    SELECT @v_capacity = max_pets
    FROM dbo.sitter_profiles WITH (UPDLOCK)
    WHERE user_id = @p_sitter_id;

    IF @v_capacity IS NULL
        RETURN 0;

    -- Проверка availability_blocks
    IF EXISTS (
        SELECT 1 FROM dbo.availability_blocks
        WHERE sitter_id = @p_sitter_id
          AND start_date < @p_check_out
          AND end_date   > @p_check_in
    )
        RETURN 0;

    -- Считаем пиковую загрузку через оконную функцию
    ;WITH events AS (
        SELECT bo.check_in AS day, bo.pets_count AS delta
        FROM dbo.boarding_orders bo
        JOIN dbo.orders o ON o.id = bo.order_id
        WHERE o.sitter_id = @p_sitter_id
          AND o.status IN ('accepted','in_progress')
          AND bo.check_in < @p_check_out
          AND bo.check_out > @p_check_in
        UNION ALL
        SELECT bo.check_out, -bo.pets_count
        FROM dbo.boarding_orders bo
        JOIN dbo.orders o ON o.id = bo.order_id
        WHERE o.sitter_id = @p_sitter_id
          AND o.status IN ('accepted','in_progress')
          AND bo.check_in < @p_check_out
          AND bo.check_out > @p_check_in
        UNION ALL SELECT @p_check_in,  @p_new_pets
        UNION ALL SELECT @p_check_out, -@p_new_pets
    ),
    running AS (
        SELECT day,
               SUM(delta) OVER (ORDER BY day, delta DESC
                                ROWS UNBOUNDED PRECEDING) AS running_total
        FROM events
    )
    SELECT @v_peak = ISNULL(MAX(running_total), 0) FROM running;

    RETURN CASE WHEN @v_peak <= @v_capacity THEN 1 ELSE 0 END;
END
GO

PRINT 'Functions and triggers created OK.';
GO

