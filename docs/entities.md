RaceDay — Entity Attributes

Detailed field-level breakdown of each entity matching the ERD.



**Role**

| Field | Type | Key |



| RoleID | int | PK |

| RoleName | varchar(50) | — |



**User (table name: Participant)**

| Field | Type | Key |



| ParticipantID | int | PK |

| FullName | varchar(100) | — |

| Email | varchar(100) | Unique |

| PasswordHash | varchar(255) | — |

| ContactNumber | varchar(20) | — |

| RoleID | int | FK → Role |

| DateRegistered | datetime2 | — |



**Event**

| Field | Type | Key |

|---|---|---|

| EventID | int | PK |

| EventName | varchar(150) | — |

| EventDate | date | — |

| Location | varchar(150) | — |

| Description | varchar(MAX) | — |

| EventType | varchar | — (run / walk / cycle) |

| Distance | decimal | — |

| OrganiserID | int | FK → Participant |

| CreatedDate | datetime2 | — |



**Category**

| Field | Type | Key |

|---|---|---|

| CategoryID | int | PK |

| EventID | int | FK → Event |

| CategoryName | varchar(100) | — |

| Distance | decimal(6,2) | — |

| MaxParticipants | int | — |

| EntryFee | decimal(8,2) | — |



**Enrolment**

| Field | Type | Key |

|---|---|---|

| EnrolmentID | int | PK |

| ParticipantID | int | FK → Participant |

| CategoryID | int | FK → Category |

| EnrolmentDate | datetime2 | — |

| Status | varchar(20) | — |



**Result**

| Field | Type | Key |

|---|---|---|

| ResultID | int | PK |

| EnrolmentID | int | FK → Enrolment |

| FinishTime | time(0) | — |

| Position | int | — |

| DateRecorded | datetime2 | — |

