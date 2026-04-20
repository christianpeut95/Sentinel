using Sentinel.Models.Timeline;
using System.Text.RegularExpressions;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for parsing natural language text and extracting entities
    /// </summary>
    public interface INaturalLanguageParserService
    {
        /// <summary>
        /// Parse narrative text and extract entities
        /// </summary>
        List<ExtractedEntity> ExtractEntities(string narrativeText);

        /// <summary>
        /// Detect keywords that indicate relationships
        /// </summary>
        List<EntityRelationship> DetectRelationships(string narrativeText, List<ExtractedEntity> entities);

        /// <summary>
        /// Detect uncertainty markers in the text
        /// </summary>
        List<string> DetectUncertaintyMarkers(string narrativeText);

        /// <summary>
        /// Detect protective measures mentioned
        /// </summary>
        List<string> DetectProtectiveMeasures(string narrativeText);

        /// <summary>
        /// Detect if text indicates a memory gap
        /// </summary>
        bool IsMemoryGap(string narrativeText);

        /// <summary>
        /// Detect corrections in the text
        /// </summary>
        TimelineCorrection? DetectCorrection(string currentText, string? previousText);
    }

    public class NaturalLanguageParserService : INaturalLanguageParserService
    {
        // Common keywords for entity detection
        private static readonly string[] PersonKeywords = { "with", "saw", "met", "and" };
        private static readonly string[] LocationKeywords = { "at", "to", "in", "from" };
        private static readonly string[] TransportKeywords = { "bus", "train", "car", "taxi", "uber", "walked", "drove", "flew" };
        private static readonly string[] EventKeywords = { "festival", "concert", "party", "wedding", "meeting", "match", "game" };
        
        // Convention names that might indicate locations
        private static readonly string[] ConventionNames = { "work", "home", "school", "gym", "church", "shops", "cinema", "pool", "park" };

        // Uncertainty markers
        private static readonly string[] UncertaintyPhrases = { "i think", "maybe", "probably", "around", "approximately", "about", "roughly", "not sure", "can't remember", "unsure" };

        // Protective measures
        private static readonly string[] ProtectiveMeasurePhrases = { "wearing mask", "wore mask", "masked", "outdoors", "outside", "stayed in car", "social distancing", "kept distance" };

        // Memory gap indicators
        private static readonly string[] MemoryGapPhrases = { "can't remember", "don't remember", "forgot", "not sure what", "unsure what", "don't recall" };

        // Correction indicators
        private static readonly string[] CorrectionPhrases = { "actually", "actually no", "wait no", "correction", "I mean", "sorry", "no wait" };

        public List<ExtractedEntity> ExtractEntities(string narrativeText)
        {
            if (string.IsNullOrWhiteSpace(narrativeText))
                return new List<ExtractedEntity>();

            var entities = new List<ExtractedEntity>();
            var lowerText = narrativeText.ToLowerInvariant();

            // Extract times (e.g., "3pm", "15:00", "around 3")
            entities.AddRange(ExtractTimes(narrativeText, lowerText));

            // Extract locations (convention names and general locations)
            entities.AddRange(ExtractLocations(narrativeText, lowerText));

            // Extract transport methods
            entities.AddRange(ExtractTransport(narrativeText, lowerText));

            // Extract events
            entities.AddRange(ExtractEvents(narrativeText, lowerText));

            // Extract people (this is trickier - look for proper nouns after keywords)
            entities.AddRange(ExtractPeople(narrativeText, lowerText));

            // Extract duration indicators
            entities.AddRange(ExtractDurations(narrativeText, lowerText));

            return entities;
        }

        private List<ExtractedEntity> ExtractTimes(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();

            // Pattern for times like "3pm", "15:00", "3:30pm"
            var timePattern = @"\b(\d{1,2})(?::(\d{2}))?\s*(am|pm|AM|PM)?\b";
            var matches = Regex.Matches(originalText, timePattern);

            foreach (Match match in matches)
            {
                entities.Add(new ExtractedEntity
                {
                    EntityType = EntityType.DateTime,
                    RawText = match.Value,
                    NormalizedValue = match.Value,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Confidence = ConfidenceLevel.High
                });
            }

            // Relative time phrases
            var relativeTimePatterns = new[] { "morning", "afternoon", "evening", "night", "lunchtime", "dinnertime" };
            foreach (var phrase in relativeTimePatterns)
            {
                int index = lowerText.IndexOf(phrase);
                if (index >= 0)
                {
                    entities.Add(new ExtractedEntity
                    {
                        EntityType = EntityType.DateTime,
                        RawText = originalText.Substring(index, phrase.Length),
                        NormalizedValue = phrase,
                        StartPosition = index,
                        EndPosition = index + phrase.Length,
                        Confidence = ConfidenceLevel.Medium
                    });
                }
            }

            return entities;
        }

        private List<ExtractedEntity> ExtractLocations(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();

            // Check for convention names
            foreach (var convention in ConventionNames)
            {
                int index = lowerText.IndexOf(convention);
                while (index >= 0)
                {
                    // Check if it's preceded by "the" (e.g., "the gym", "the shops")
                    int actualStart = index;
                    if (index >= 4 && lowerText.Substring(index - 4, 4) == "the ")
                    {
                        actualStart = index - 4;
                    }

                    entities.Add(new ExtractedEntity
                    {
                        EntityType = EntityType.Location,
                        RawText = originalText.Substring(actualStart, index - actualStart + convention.Length),
                        NormalizedValue = convention,
                        StartPosition = actualStart,
                        EndPosition = index + convention.Length,
                        Confidence = ConfidenceLevel.High,
                        Metadata = new Dictionary<string, object> { { "IsConvention", true } }
                    });

                    index = lowerText.IndexOf(convention, index + 1);
                }
            }

            // Look for proper nouns after location keywords
            foreach (var keyword in LocationKeywords)
            {
                var pattern = $@"\b{keyword}\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)";
                var matches = Regex.Matches(originalText, pattern);

                foreach (Match match in matches)
                {
                    var locationText = match.Groups[1].Value;
                    entities.Add(new ExtractedEntity
                    {
                        EntityType = EntityType.Location,
                        RawText = locationText,
                        NormalizedValue = locationText,
                        StartPosition = match.Groups[1].Index,
                        EndPosition = match.Groups[1].Index + locationText.Length,
                        Confidence = ConfidenceLevel.Medium
                    });
                }
            }

            return entities;
        }

        private List<ExtractedEntity> ExtractTransport(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();

            foreach (var transport in TransportKeywords)
            {
                int index = lowerText.IndexOf(transport);
                while (index >= 0)
                {
                    // Check for route numbers (e.g., "bus 557")
                    var routePattern = $@"{transport}\s+(\d+)";
                    var match = Regex.Match(originalText.Substring(index), routePattern);

                    if (match.Success)
                    {
                        entities.Add(new ExtractedEntity
                        {
                            EntityType = EntityType.Transport,
                            RawText = match.Value,
                            NormalizedValue = match.Value,
                            StartPosition = index,
                            EndPosition = index + match.Length,
                            Confidence = ConfidenceLevel.High
                        });
                    }
                    else
                    {
                        entities.Add(new ExtractedEntity
                        {
                            EntityType = EntityType.Transport,
                            RawText = originalText.Substring(index, transport.Length),
                            NormalizedValue = transport,
                            StartPosition = index,
                            EndPosition = index + transport.Length,
                            Confidence = ConfidenceLevel.Medium
                        });
                    }

                    index = lowerText.IndexOf(transport, index + 1);
                }
            }

            return entities;
        }

        private List<ExtractedEntity> ExtractEvents(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();

            foreach (var eventType in EventKeywords)
            {
                int index = lowerText.IndexOf(eventType);
                while (index >= 0)
                {
                    // Look for "event at location" pattern
                    var atPattern = $@"{eventType}\s+at\s+([A-Za-z\s]+)";
                    var match = Regex.Match(originalText.Substring(Math.Max(0, index - 10)), atPattern);

                    if (match.Success)
                    {
                        entities.Add(new ExtractedEntity
                        {
                            EntityType = EntityType.Event,
                            RawText = match.Value,
                            NormalizedValue = eventType,
                            StartPosition = index,
                            EndPosition = index + match.Length,
                            Confidence = ConfidenceLevel.High
                        });
                    }
                    else
                    {
                        entities.Add(new ExtractedEntity
                        {
                            EntityType = EntityType.Event,
                            RawText = originalText.Substring(index, eventType.Length),
                            NormalizedValue = eventType,
                            StartPosition = index,
                            EndPosition = index + eventType.Length,
                            Confidence = ConfidenceLevel.Medium
                        });
                    }

                    index = lowerText.IndexOf(eventType, index + 1);
                }
            }

            return entities;
        }

        private List<ExtractedEntity> ExtractPeople(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();

            foreach (var keyword in PersonKeywords)
            {
                // Look for patterns like "with mum", "saw John", "and Sarah"
                var pattern = $@"\b{keyword}\s+([a-z]+(?:\s+and\s+[a-z]+)*)";
                var matches = Regex.Matches(lowerText, pattern);

                foreach (Match match in matches)
                {
                    var personText = match.Groups[1].Value;
                    var actualIndex = match.Groups[1].Index;

                    entities.Add(new ExtractedEntity
                    {
                        EntityType = EntityType.Person,
                        RawText = personText,
                        NormalizedValue = personText,
                        StartPosition = actualIndex,
                        EndPosition = actualIndex + personText.Length,
                        Confidence = ConfidenceLevel.Medium
                    });
                }
            }

            return entities;
        }

        private List<ExtractedEntity> ExtractDurations(string originalText, string lowerText)
        {
            var entities = new List<ExtractedEntity>();
            
            var durationPhrases = new[] 
            { 
                "all day", "all afternoon", "all morning", "all evening",
                "couple hours", "few hours", "several hours",
                "quick visit", "briefly", "for a bit",
                "overnight", "all night"
            };

            foreach (var phrase in durationPhrases)
            {
                int index = lowerText.IndexOf(phrase);
                if (index >= 0)
                {
                    entities.Add(new ExtractedEntity
                    {
                        EntityType = EntityType.Duration,
                        RawText = originalText.Substring(index, phrase.Length),
                        NormalizedValue = phrase,
                        StartPosition = index,
                        EndPosition = index + phrase.Length,
                        Confidence = ConfidenceLevel.High
                    });
                }
            }

            return entities;
        }

        public List<EntityRelationship> DetectRelationships(string narrativeText, List<ExtractedEntity> entities)
        {
            var relationships = new List<EntityRelationship>();
            var lowerText = narrativeText.ToLowerInvariant();

            // Detect "with" relationships (person accompaniment)
            var withMatches = Regex.Matches(lowerText, @"with\s+(\w+)");
            foreach (Match match in withMatches)
            {
                var person = entities.FirstOrDefault(e => 
                    e.EntityType == EntityType.Person && 
                    e.StartPosition == match.Groups[1].Index);
                
                if (person != null)
                {
                    // Find preceding location or activity
                    var precedingEntity = entities
                        .Where(e => e.EndPosition < person.StartPosition)
                        .OrderByDescending(e => e.EndPosition)
                        .FirstOrDefault();

                    if (precedingEntity != null)
                    {
                        relationships.Add(new EntityRelationship
                        {
                            ParentEntityId = precedingEntity.Id,
                            ChildEntityId = person.Id,
                            RelationType = RelationshipType.With
                        });
                    }
                }
            }

            // Detect "at" relationships (location)
            var atMatches = Regex.Matches(lowerText, @"at\s+");
            foreach (Match match in atMatches)
            {
                var location = entities.FirstOrDefault(e => 
                    e.EntityType == EntityType.Location && 
                    e.StartPosition >= match.Index);
                
                if (location != null)
                {
                    var activity = entities
                        .Where(e => e.EndPosition < match.Index)
                        .OrderByDescending(e => e.EndPosition)
                        .FirstOrDefault();

                    if (activity != null)
                    {
                        relationships.Add(new EntityRelationship
                        {
                            ParentEntityId = activity.Id,
                            ChildEntityId = location.Id,
                            RelationType = RelationshipType.At
                        });
                    }
                }
            }

            return relationships;
        }

        public List<string> DetectUncertaintyMarkers(string narrativeText)
        {
            var markers = new List<string>();
            var lowerText = narrativeText.ToLowerInvariant();

            foreach (var phrase in UncertaintyPhrases)
            {
                if (lowerText.Contains(phrase))
                {
                    markers.Add(phrase);
                }
            }

            return markers;
        }

        public List<string> DetectProtectiveMeasures(string narrativeText)
        {
            var measures = new List<string>();
            var lowerText = narrativeText.ToLowerInvariant();

            foreach (var phrase in ProtectiveMeasurePhrases)
            {
                if (lowerText.Contains(phrase))
                {
                    measures.Add(phrase);
                }
            }

            return measures;
        }

        public bool IsMemoryGap(string narrativeText)
        {
            var lowerText = narrativeText.ToLowerInvariant();
            return MemoryGapPhrases.Any(phrase => lowerText.Contains(phrase));
        }

        public TimelineCorrection? DetectCorrection(string currentText, string? previousText)
        {
            if (string.IsNullOrWhiteSpace(previousText) || string.IsNullOrWhiteSpace(currentText))
                return null;

            var lowerCurrent = currentText.ToLowerInvariant();
            var hasCorrection = CorrectionPhrases.Any(phrase => lowerCurrent.Contains(phrase));

            if (hasCorrection)
            {
                return new TimelineCorrection
                {
                    CorrectedAt = DateTime.UtcNow,
                    OriginalText = previousText,
                    CorrectedText = currentText,
                    Reason = "Inline correction detected"
                };
            }

            return null;
        }
    }
}
