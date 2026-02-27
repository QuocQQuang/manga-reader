-- Add ThemePreference column to Users table
ALTER TABLE Users
ADD ThemePreference NVARCHAR(10) NOT NULL DEFAULT 'light';
go
-- Update existing user records to have the default theme
UPDATE Users
SET ThemePreference = 'light'
WHERE ThemePreference IS NULL; 