-- ============================================================
-- Local Development Database Setup
-- Poll & Vote Survey Application
-- ============================================================

-- Create PollDB database
CREATE DATABASE IF NOT EXISTS PollDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE PollDB;

-- Create polls table
CREATE TABLE IF NOT EXISTS Polls (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(255) NOT NULL UNIQUE,
    Question VARCHAR(500) NOT NULL,
    QuestionType VARCHAR(50) NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Active',
    ExpireAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL,
    INDEX idx_code (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create options table
CREATE TABLE IF NOT EXISTS Options (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PollId INT NOT NULL,
    Text VARCHAR(255) NOT NULL,
    FOREIGN KEY (PollId) REFERENCES Polls(Id) ON DELETE CASCADE,
    INDEX idx_poll_id (PollId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================

-- Create VoteDB database
CREATE DATABASE IF NOT EXISTS VoteDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE VoteDB;

-- Create votes table
CREATE TABLE IF NOT EXISTS Votes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PollCode VARCHAR(255) NOT NULL,
    OptionId INT NOT NULL,
    VoteValue VARCHAR(500),
    VoterToken VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    INDEX idx_poll_code (PollCode),
    INDEX idx_voter_token (VoterToken)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- Verification Queries
-- ============================================================

-- Verify databases created
SHOW DATABASES LIKE '%DB';

-- Show PollDB tables
USE PollDB;
SHOW TABLES;

-- Show VoteDB tables
USE VoteDB;
SHOW TABLES;
