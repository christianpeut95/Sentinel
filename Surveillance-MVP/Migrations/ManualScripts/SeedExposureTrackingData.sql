-- Seed Data for Exposure Tracking System
-- Run this after AddExposureTrackingSystem.sql

-- Seed LocationTypes
PRINT 'Seeding LocationTypes...';

IF NOT EXISTS (SELECT 1 FROM LocationTypes WHERE Name = 'Healthcare Facility')
BEGIN
    INSERT INTO LocationTypes (Name, Description, IsHighRisk, DisplayOrder, IsActive)
    VALUES
    ('Healthcare Facility', 'Hospitals, clinics, aged care facilities, medical centers', 1, 1, 1),
    ('Education', 'Schools, universities, childcare centers, training facilities', 0, 2, 1),
    ('Retail', 'Shopping centers, stores, markets, supermarkets', 0, 3, 1),
    ('Hospitality', 'Restaurants, cafes, hotels, bars, pubs', 0, 4, 1),
    ('Residential', 'Private homes, apartments, residential care facilities', 0, 5, 1),
    ('Public Space', 'Parks, beaches, playgrounds, streets, public areas', 0, 6, 1),
    ('Transport', 'Airports, train stations, buses, taxis, flights', 0, 7, 1),
    ('Religious', 'Churches, mosques, temples, synagogues, places of worship', 0, 8, 1),
    ('Workplace', 'Offices, factories, construction sites, warehouses', 0, 9, 1),
    ('Entertainment', 'Cinemas, theaters, clubs, entertainment venues', 0, 10, 1),
    ('Sports & Recreation', 'Gyms, sports facilities, recreation centers', 0, 11, 1),
    ('Other', 'Other location types not listed above', 0, 12, 1);
    
    PRINT 'LocationTypes seeded successfully.';
END
ELSE
BEGIN
    PRINT 'LocationTypes already exist - skipping seed.';
END

-- Seed EventTypes
PRINT 'Seeding EventTypes...';

IF NOT EXISTS (SELECT 1 FROM EventTypes WHERE Name = 'Party')
BEGIN
    INSERT INTO EventTypes (Name, Description, DisplayOrder, IsActive)
    VALUES
    ('Party', 'Birthday parties, celebrations, private parties', 1, 1),
    ('Wedding', 'Wedding ceremonies, receptions, anniversary celebrations', 2, 1),
    ('Funeral', 'Funeral services, wakes, memorial services', 3, 1),
    ('Conference', 'Conferences, seminars, workshops, symposiums', 4, 1),
    ('Concert', 'Concerts, live performances, music shows', 5, 1),
    ('Festival', 'Community festivals, fairs, cultural events', 6, 1),
    ('Religious Service', 'Religious services, worship gatherings, prayer meetings', 7, 1),
    ('Sports Event', 'Sports games, matches, tournaments, competitions', 8, 1),
    ('School Event', 'School assemblies, camps, excursions, graduations', 9, 1),
    ('Meeting', 'Business meetings, social gatherings, community meetings', 10, 1),
    ('Dining Event', 'Group dinners, banquets, meal gatherings', 11, 1),
    ('Training', 'Training sessions, courses, educational programs', 12, 1),
    ('Other', 'Other event types not listed above', 13, 1);
    
    PRINT 'EventTypes seeded successfully.';
END
ELSE
BEGIN
    PRINT 'EventTypes already exist - skipping seed.';
END

PRINT 'Exposure Tracking seed data completed successfully.';
GO
