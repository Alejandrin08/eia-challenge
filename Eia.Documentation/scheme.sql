CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "ExtractionRuns" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ExtractionRuns" PRIMARY KEY AUTOINCREMENT,
    "ExtractedAt" TEXT NOT NULL,
    "RecordCount" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "ErrorMessage" TEXT NULL
);

CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);

CREATE TABLE "NuclearOutages" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NuclearOutages" PRIMARY KEY AUTOINCREMENT,
    "Period" TEXT NOT NULL,
    "CapacityMw" REAL NULL,
    "OutageMw" REAL NULL,
    "PercentOutage" REAL NULL,
    "ExtractionRunId" INTEGER NOT NULL,
    CONSTRAINT "FK_NuclearOutages_ExtractionRuns_ExtractionRunId" FOREIGN KEY ("ExtractionRunId") REFERENCES "ExtractionRuns" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_NuclearOutages_ExtractionRunId" ON "NuclearOutages" ("ExtractionRunId");

CREATE UNIQUE INDEX "IX_NuclearOutages_Period" ON "NuclearOutages" ("Period");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260324060145_InitialCreate', '9.0.0');

COMMIT;

