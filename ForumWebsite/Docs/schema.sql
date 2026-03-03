-- ============================================================
-- ForumWebsite – actual SQL schema (SQL Server)
-- Applied by: dotnet ef database update
-- Migration:  20260227031956_InitialCreate
-- ============================================================

-- ── Users ─────────────────────────────────────────────────────────────────────
CREATE TABLE [Users] (
    [Id]           INT            NOT NULL IDENTITY,
    [Username]     NVARCHAR(50)   NOT NULL,
    [Email]        NVARCHAR(100)  NOT NULL,
    [PasswordHash] NVARCHAR(MAX)  NOT NULL,
    [Role]         NVARCHAR(20)   NOT NULL DEFAULT N'User',
    [CreatedAt]    DATETIME2      NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt]    DATETIME2      NULL,
    -- No DEFAULT: EF always sends the value explicitly (prevents bool/HasDefaultValue bug).
    [IsActive]     BIT            NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [UX_Users_Username] ON [Users] ([Username]);
CREATE UNIQUE INDEX [UX_Users_Email]    ON [Users] ([Email]);

-- ── Posts ─────────────────────────────────────────────────────────────────────
CREATE TABLE [Posts] (
    [Id]        INT            NOT NULL IDENTITY,
    [Title]     NVARCHAR(300)  NOT NULL,
    [Content]   NVARCHAR(MAX)  NOT NULL,
    [UserId]    INT            NOT NULL,
    [ViewCount] INT            NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2      NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] DATETIME2      NULL,
    [IsDeleted] BIT            NOT NULL DEFAULT CAST(0 AS BIT),
    CONSTRAINT [PK_Posts]          PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Posts_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Posts_UserId]    ON [Posts] ([UserId]);
CREATE INDEX [IX_Posts_CreatedAt] ON [Posts] ([CreatedAt]);
CREATE INDEX [IX_Posts_IsDeleted] ON [Posts] ([IsDeleted]);

-- ── Comments ──────────────────────────────────────────────────────────────────
CREATE TABLE [Comments] (
    [Id]        INT            NOT NULL IDENTITY,
    -- EF maps HasMaxLength(5000) to nvarchar(max) on SQL Server (limit above 4000).
    -- Enforcement is at application layer (FluentValidation: max 5000 chars).
    [Content]   NVARCHAR(MAX)  NOT NULL,
    [PostId]    INT            NOT NULL,
    [UserId]    INT            NOT NULL,
    [CreatedAt] DATETIME2      NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] DATETIME2      NULL,
    [IsDeleted] BIT            NOT NULL DEFAULT CAST(0 AS BIT),
    CONSTRAINT [PK_Comments]              PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Comments_Posts_PostId]
        FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Comments_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Comments_PostId] ON [Comments] ([PostId]);
CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
