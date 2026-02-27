-- Add UpdatedAt column to Pages table
ALTER TABLE Pages ADD UpdatedAt DATETIME2;

-- Set default value for existing records
UPDATE Pages SET UpdatedAt = GETDATE();

-- Make UpdatedAt column NOT NULL after populating with values
ALTER TABLE Pages ALTER COLUMN UpdatedAt DATETIME2 NOT NULL;

-- Add a default constraint for new records
ALTER TABLE Pages ADD CONSTRAINT DF_Pages_UpdatedAt DEFAULT GETDATE() FOR UpdatedAt; 