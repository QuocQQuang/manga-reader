-- Add UploadedByUserId column to Mangas table
ALTER TABLE Mangas
ADD UploadedByUserId INT NULL;

-- Add foreign key constraint
ALTER TABLE Mangas
ADD CONSTRAINT FK_Mangas_Users_UploadedByUserId
FOREIGN KEY (UploadedByUserId) REFERENCES Users(UserId)
ON DELETE SET NULL;
