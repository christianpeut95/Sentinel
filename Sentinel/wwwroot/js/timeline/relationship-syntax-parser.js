/**
 * Relationship Syntax Parser
 * Parses inline relationship syntax like: @person @location >transport @time.
 * 
 * Syntax:
 * @name  = with person (WITH relationship) or at location (AT_LOCATION relationship)
 * @time = at time (AT_TIME relationship)  
 * >transport = via transport (VIA relationship)
 * .  = relationship boundary (end of group)
 * #GroupName = group reference
 * #GroupName(...entities...) = inline group creation
 * 
 * Example: "went to ..sushi train @john @mary @1PM. then to ..park @mary."
 * Example: "visited hospital #Family(@John @Mary @Sue) @2PM."
 */

class RelationshipSyntaxParser {
    constructor() {
        this.operatorPatterns = {
            '@': { type: 'at', relationshipType: 2 },        // AT_LOCATION, AT_TIME, or WITH (person)
            '>': { type: 'via', relationshipType: 3 }        // VIA (transport)
        };
    }

    /**
     * Parse text to find relationship groups
     * @param {string} text - Full text to parse
     * @returns {Array<RelationshipGroup>} Array of relationship groups with position info
     */
    parse(text) {
        const groups = [];

        // Find all relationship group boundaries (terminated by period or double newline)
        // BUT: Don't split on periods that immediately follow entity markers (@. >. ..)
        // This prevents "@." (incomplete location) from being treated as a sentence boundary
        // Split while preserving position information
        const splitRegex = /(?<![@>.])\.\s|\.$/g;
        let lastIndex = 0;
        let match;
        const segmentsWithPositions = [];

        // Collect all segments with their start/end positions
        while ((match = splitRegex.exec(text)) !== null) {
            const segmentText = text.substring(lastIndex, match.index + 1); // Include the period
            const trimmedText = segmentText.trim();
            if (trimmedText) {
                // Calculate how much whitespace was trimmed from the start
                const leadingWhitespace = segmentText.indexOf(trimmedText);
                const adjustedStart = lastIndex + leadingWhitespace;

                segmentsWithPositions.push({
                    text: trimmedText,
                    startPosition: adjustedStart,
                    endPosition: match.index + 1
                });
            }
            lastIndex = match.index + match[0].length;
        }

        // Don't forget the last segment (after the last delimiter)
        if (lastIndex < text.length) {
            const segmentText = text.substring(lastIndex);
            const trimmedText = segmentText.trim();
            if (trimmedText) {
                // Calculate how much whitespace was trimmed from the start
                const leadingWhitespace = segmentText.indexOf(trimmedText);
                const adjustedStart = lastIndex + leadingWhitespace;
                // Calculate actual end position (adjustedStart + trimmed text length)
                const adjustedEnd = adjustedStart + trimmedText.length;

                segmentsWithPositions.push({
                    text: trimmedText,
                    startPosition: adjustedStart,
                    endPosition: adjustedEnd
                });
            }
        }

        for (let segmentInfo of segmentsWithPositions) {
            const group = this.parseSegment(segmentInfo.text);
            if (group && group.entities.length > 1) {
                // Add position information to the group
                group.startPosition = segmentInfo.startPosition;
                group.endPosition = segmentInfo.endPosition;

                // Adjust entity positions to be absolute (relative to full text, not segment)
                group.entities.forEach(entity => {
                    if (entity.position !== undefined) {
                        entity.position += segmentInfo.startPosition;
                    }
                });

                groups.push(group);
            }
        }

        return groups;
    }

    /**
     * Parse a single segment (text between periods)
     * @param {string} segment - Segment text
     * @returns {RelationshipGroup|null} Relationship group or null
     */
    parseSegment(segment) {
        // Find all entity markers: ..entityName, @name, @location, >transport, #groupName, @#GroupName
        // Operators (@/>) may optionally have a space before the entity name
        // Special case: @#GroupName for group references
        // Updated to support multi-word entity names (e.g., "Hoyts Norwood", "McDonald's West Perth")
        // Pattern captures from operator until next operator or sentence boundary
        // Updated to stop at + prefix operator (e.g., "with +@Sunny")
        const entityPattern = /(\.\.[^@>\s.]+|@\s*#\w+|[@>]\s*[^@>.+]+?(?=\s*[+@>.]|$)|#\w+)/g;
        const matches = [...segment.matchAll(entityPattern)];

        if (matches.length === 0) return null;

        const entities = [];
        let baseEntity = null;

        for (let match of matches) {
            const fullMatch = match[0];
            const position = match.index;

            if (fullMatch.startsWith('..')) {
                // Base entity (location, event, etc.)
                const entityText = fullMatch.substring(2).trim();
                baseEntity = {
                    marker: '..',
                    text: entityText,
                    position: position,
                    role: 'primary'
                };
                entities.push(baseEntity);
            } else if (fullMatch.startsWith('@')) {
                // Person, Location, or Time (@ handles all)
                const value = fullMatch.substring(1).trim();
                // Detect if it's a time pattern (contains AM/PM or numbers with colon)
                const isTime = /\d{1,2}:\d{2}|AM|PM|\d+PM|\d+AM/i.test(value);
                // We can't distinguish person vs location here - that's determined during entity resolution
                // For now, treat non-time @ entities as location by default (will be corrected when entity type is known)
                entities.push({
                    marker: '@',
                    text: value,
                    position: position,
                    role: isTime ? 'at_time' : 'at_location', // Default to location, corrected during resolution
                    relationshipType: isTime ? 5 : 2 // Will be overridden if entity is Person
                });
            } else if (fullMatch.startsWith('>')) {
                // Transport (VIA relationship)
                const transportName = fullMatch.substring(1).trim();
                entities.push({
                    marker: '>',
                    text: transportName,
                    position: position,
                    role: 'via',
                    relationshipType: 3
                });
            } else if (fullMatch.startsWith('#')) {
                // Group reference
                const groupName = fullMatch.substring(1).trim();
                entities.push({
                    marker: '#',
                    text: groupName,
                    position: position,
                    role: 'group',
                    isGroupReference: true
                });
            }
        }
        
        if (entities.length === 0) return null;
        
        return {
            segment: segment,
            entities: entities,
            baseEntity: baseEntity
        };
    }

    /**
     * Create relationships between entities in a group
     * @param {RelationshipGroup} group - Relationship group
     * @param {Array} resolvedEntities - Array of actual entity objects with IDs
     * @returns {Array<Relationship>} Array of relationship objects
     */
    createRelationships(group, resolvedEntities) {
        if (!group || !resolvedEntities || resolvedEntities.length < 2) {
            return [];
        }

        const relationships = [];

        // Smart primary entity selection:
        // 1. First, try to find explicitly marked primary (.. or @ operator)
        let primaryEntity = resolvedEntities.find(e => e.isPrimary);

        // 2. If no explicit primary, fall back to Location or Event entity
        //    This allows "went to McDonald's +John" to work without @ operator
        if (!primaryEntity) {
            primaryEntity = resolvedEntities.find(e => e.entityType === 2 || e.entityType === 3); // Location or Event
        }

        // 3. Final fallback: use first entity
        if (!primaryEntity) {
            primaryEntity = resolvedEntities[0];
        }

        // Create relationships from all other entities to primary
        // Exclude temporal/duration entities (DateTime, Duration) as they are metadata, not relationship targets
        const nonTemporalEntities = resolvedEntities.filter(e => 
            e.id !== primaryEntity.id && e.entityType !== 5 && e.entityType !== 6
        );

        for (let entity of nonTemporalEntities) {
            const relationship = {
                id: this.generateId(),
                primaryEntityId: primaryEntity.id,
                relatedEntityId: entity.id,
                relationType: entity.relationshipType || this.inferRelationshipType(entity.entityType),
                timeEntityId: null,
                allTimeEntityIds: [], // Store all time entities for this relationship
                durationEntityId: null // Store duration entity for this relationship
            };

            relationships.push(relationship);
        }

        // Special case: If we have a Location/Event with ONLY DateTime/Duration entities (no other relationships)
        // Create a self-relationship to hold the temporal metadata
        // This handles cases like "@Coles @2pm" where we want to track the time for the location
        if (relationships.length === 0 && (primaryEntity.entityType === 2 || primaryEntity.entityType === 3)) {
            const hasTemporalEntities = resolvedEntities.some(e => e.entityType === 5 || e.entityType === 6);
            if (hasTemporalEntities) {
                // Create a self-relationship for the location/event
                const selfRelationship = {
                    id: this.generateId(),
                    primaryEntityId: primaryEntity.id,
                    relatedEntityId: primaryEntity.id, // Self-reference
                    relationType: primaryEntity.entityType === 2 ? 2 : 4, // AT_LOCATION or AT_EVENT
                    timeEntityId: null,
                    allTimeEntityIds: [],
                    durationEntityId: null
                };
                relationships.push(selfRelationship);
            }
        }

        // Link ALL time entities to relationships (not just the first one)
        const timeEntities = resolvedEntities.filter(e => e.entityType === 5); // DateTime
        if (timeEntities.length > 0) {
            relationships.forEach(rel => {
                // Don't link time entity to itself
                const relevantTimeEntities = timeEntities.filter(t => t.id !== rel.relatedEntityId);

                if (relevantTimeEntities.length > 0) {
                    // Set timeEntityId to first time for backward compatibility
                    rel.timeEntityId = relevantTimeEntities[0].id;
                    // Store all time entity IDs for duration calculations
                    rel.allTimeEntityIds = relevantTimeEntities.map(t => t.id);
                }
            });
        }

        // Link Duration entities to relationships
        // Duration should apply to all relationships in the group (e.g., "at Coles with John for 20 minutes")
        const durationEntities = resolvedEntities.filter(e => e.entityType === 6); // Duration
        if (durationEntities.length > 0) {
            relationships.forEach(rel => {
                // Don't link duration entity to itself
                const relevantDurationEntities = durationEntities.filter(d => d.id !== rel.relatedEntityId);

                if (relevantDurationEntities.length > 0) {
                    // Store duration entity ID for duration calculations
                    // For now, use the first duration if multiple exist
                    rel.durationEntityId = relevantDurationEntities[0].id;
                }
            });
        }

        return relationships;
    }

    /**
     * Infer relationship type from entity type
     * @param {number} entityType - EntityType enum value
     * @returns {number} Relationship type
     */
    inferRelationshipType(entityType) {
        // EntityType: 1=Person, 2=Location, 3=Event, 4=Transport, 5=DateTime, 6=Duration, 7=Activity
        // RelationType: 1=WITH, 2=AT_LOCATION, 3=VIA, 4=AT_EVENT, 5=AT_TIME, 6=FOR_DURATION, 7=CO_OCCURRED, 8=SEQUENCE, 9=MET, 10=ACTIVITY
        switch(entityType) {
            case 1: return 1;  // Person → WITH
            case 2: return 2;  // Location → AT_LOCATION
            case 3: return 4;  // Event → AT_EVENT
            case 4: return 3;  // Transport → VIA
            case 5: return 5;  // DateTime → AT_TIME
            case 6: return 6;  // Duration → FOR_DURATION
            case 7: return 10; // Activity → ACTIVITY
            default: return 7; // CO_OCCURRED (fallback)
        }
    }

    /**
     * Generate unique ID
     * @returns {string} Unique ID
     */
    generateId() {
        return 'rel_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    /**
     * Extract group definitions from text
     * Format: #GroupName(..name1, ..name2, ..name3)
     * @param {string} text - Text to parse
     * @returns {Array<GroupDefinition>} Array of group definitions
     */
    parseGroupDefinitions(text) {
        const groupDefPattern = /#(\w+)\s*\(([^)]+)\)/g;
        const groups = [];
        
        for (let match of text.matchAll(groupDefPattern)) {
            const groupName = match[1];
            const memberText = match[2];
            
            // Extract member names (handles ..name or plain name)
            const memberPattern = /(?:\.\.)([^,\s]+)|([^,\s]+)/g;
            const members = [];
            
            for (let memberMatch of memberText.matchAll(memberPattern)) {
                const name = memberMatch[1] || memberMatch[2];
                if (name) {
                    members.push(name.trim());
                }
            }
            
            groups.push({
                name: groupName,
                members: members,
                fullText: match[0],
                position: match.index
            });
        }
        
        return groups;
    }

    /**
     * Find group references in text
     * Format: +#GroupName or #GroupName
     * @param {string} text - Text to parse
     * @returns {Array<GroupReference>} Array of group references
     */
    parseGroupReferences(text) {
        const groupRefPattern = /([+@>]?)#(\w+)(?!\()/g;
        const references = [];
        
        for (let match of text.matchAll(groupRefPattern)) {
            const operator = match[1] || '';
            const groupName = match[2];
            
            references.push({
                name: groupName,
                operator: operator,
                fullText: match[0],
                position: match.index
            });
        }
        
        return references;
    }
}

// Export for use in other modules
if (typeof window !== 'undefined') {
    window.RelationshipSyntaxParser = RelationshipSyntaxParser;
}
