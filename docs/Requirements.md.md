\# RaceDay: Entity Requirements



This document lists the core entities identified for the RaceDay system, based on the

functional requirements for Authentication, User Profile, Events, Categories, Event

Enrolments and Results.



**Core Entities:**

1\. Role — Defines the two system roles: Organiser and Participant. Drives role-based

&#x20;  access control across the API.



2\. User (implemented as `Participant` in the database) — A registered account, either an 

&#x20;  Organiser or a Participant, linked to a Role.



3\. Event — A race event created by an Organiser (e.g. a city run or cycle challenge),

&#x20;  capturing name, description, date, location, distance, and event type (run/walk/cycle).



4\. Category — An age-based or distance-based division within an Event (e.g. "Under 20",

&#x20;  "10km"), which Participants enrol into.



5\. Enrolment — The link between a Participant and a Category they've entered. Acts as

&#x20;  the junction table between Users and Categories.



6\. Result — A recorded finish time and finishing position for a completed Enrolment,

&#x20;  captured by the event's Organiser.



**Why these entities:**

Every entity here maps directly to a functional requirement in the brief, Role and User

support Authentication and User Profile; Event and Category support event/category

management; Enrolment supports the entry process; Result supports post-race capture and

personal history.

