-- RaceDay Database Script
-- PROG6212 - Part 1 Section C
-- ======================================================================================
CREATE DATABASE RaceDayDB;
USE RaceDayDB;

-- Role
CREATE TABLE Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);

-- Participant
CREATE TABLE Participant (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    ContactNumber VARCHAR(20),
    RoleID INT NOT NULL,
    DateRegistered DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (RoleID) REFERENCES Role(RoleID)
);

-- Event
CREATE TABLE EVENT (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    EventName VARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150),
    Description VARCHAR(MAX),
    EventType VARCHAR(10) NOT NULL,
    Distance DECIMAL(6,2),
    OrganiserID INT NOT NULL,
    CreatedDate DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (OrganiserID) REFERENCES Participant(ParticipantID)
);
 
-- Category
CREATE TABLE CATEGORY (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2),
    MaxParticipants INT,
    EntryFee DECIMAL(8,2) DEFAULT 0,
    FOREIGN KEY (EventID) REFERENCES EVENT(EventID) ON DELETE CASCADE
);

-- Enrolment
CREATE TABLE ENROLMENT (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 DEFAULT SYSDATETIME(),
    Status VARCHAR(20) DEFAULT 'Confirmed',
    FOREIGN KEY (ParticipantID) REFERENCES Participant(ParticipantID),
    FOREIGN KEY (CategoryID) REFERENCES CATEGORY(CategoryID),
    -- Prevents double booking
    UNIQUE (ParticipantID, CategoryID)
);

-- Result
CREATE TABLE RESULT (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME(0),
    Position INT,
    DateRecorded DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (EnrolmentID) REFERENCES ENROLMENT(EnrolmentID) ON DELETE CASCADE
); 

-- ======================================================================================
-- SEED DATA
-- ======================================================================================
-- Roles
INSERT INTO Role (RoleName) 
VALUES ('Organiser');

INSERT INTO Role (RoleName) 
VALUES ('Participant');

-- Participants
INSERT INTO Participant (FullName, Email, PasswordHash, ContactNumber, RoleID)
VALUES ('John Smith', 'john.smith@raceday.com', 'hash_a1b2c3d4e5', '082-111-2222', 1);

INSERT INTO Participant (FullName, Email, PasswordHash, ContactNumber, RoleID)
VALUES ('Sarah Jones', 'sarah.jones@raceday.com', 'hash_f6g7h8i9j0', '083-333-4444', 1);

INSERT INTO Participant (FullName, Email, PasswordHash, ContactNumber, RoleID)
VALUES ('Mike Brown', 'mike.brown@example.com', 'hash_k1l2m3n4o5', '084-555-6666', 2);

INSERT INTO Participant (FullName, Email, PasswordHash, ContactNumber, RoleID)
VALUES ('Emily Davis', 'emily.davis@example.com', 'hash_p6q7r8s9t0', '085-777-8888', 2);
 
-- Events
INSERT INTO EVENT (EventName, EventDate, Location, Description, EventType, Distance, OrganiserID)
VALUES ('Johannesburg City Run', '2026-11-15', 'Johannesburg, GP', 'Annual road run through the Johannesburg CBD.', 'Run', 10.00, 1);

INSERT INTO EVENT (EventName, EventDate, Location, Description, EventType, Distance, OrganiserID)
VALUES ('Cape Town Cycle Challenge', '2026-10-24', 'Cape Town, WC', 'Scenic road cycling event around the Cape Peninsula.', 'Cycle', 80.00, 2);

INSERT INTO EVENT (EventName, EventDate, Location, Description, EventType, Distance, OrganiserID)
VALUES ('Durban Beach Walk', '2026-09-30', 'Durban, KZN', 'Family-friendly walk along the Durban beachfront.', 'Walk', 10.00, 1);

 
-- Categories
INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (1, '5km Fun Run', 5.00, 200, 100.00);

INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (1, '10km Challenge', 10.00, 150, 150.00);

INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (2, '40km Road Cycle', 40.00, 100, 250.00);

INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (2, '80km Road Cycle', 80.00, 80, 350.00);

INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (3, '5km Walk', 5.00, 300, 50.00);

INSERT INTO CATEGORY (EventID, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES (3, '10km Walk', 10.00, 200, 75.00);

 
-- Enrolments
INSERT INTO ENROLMENT (ParticipantID, CategoryID, Status)
VALUES (3, 2, 'Confirmed');

INSERT INTO ENROLMENT (ParticipantID, CategoryID, Status) 
VALUES (3, 5, 'Confirmed');

INSERT INTO ENROLMENT (ParticipantID, CategoryID, Status) 
VALUES (4, 3, 'Confirmed');

INSERT INTO ENROLMENT (ParticipantID, CategoryID, Status) 
VALUES (4, 1, 'Pending');
 
-- Results
INSERT INTO RESULT (EnrolmentID, FinishTime, Position) 
VALUES (1, '00:52:30', 14);

INSERT INTO RESULT (EnrolmentID, FinishTime, Position) 
VALUES (2, '00:41:10', 8);

SELECT * FROM Role;
SELECT * FROM Participant;
SELECT * FROM EVENT;
SELECT * FROM CATEGORY;
SELECT * FROM ENROLMENT;
SELECT * FROM RESULT;