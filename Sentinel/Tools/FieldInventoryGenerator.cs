using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using System.Text;

namespace Sentinel.Tools;

/// <summary>
/// Utility to generate a complete inventory of all database fields
/// This ensures we only expose fields that ACTUALLY exist in the database
/// Run once to generate REAL_FIELDS_INVENTORY.md
/// </summary>
public class FieldInventoryGenerator
{
    private readonly ApplicationDbContext _context;

    public FieldInventoryGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates a comprehensive field inventory markdown document
    /// </summary>
    public string GenerateInventory()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Surveillance System - Field Inventory");
        sb.AppendLine();
        sb.AppendLine("**Generated:** " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("This document lists ALL fields available in the database.");
        sb.AppendLine("Use this as the authoritative source for report field metadata.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        var entityTypes = _context.Model.GetEntityTypes()
            .OrderBy(e => e.ClrType.Name)
            .ToList();

        foreach (var entityType in entityTypes)
        {
            sb.AppendLine($"## {entityType.ClrType.Name}");
            sb.AppendLine();
            sb.AppendLine($"**Table:** `{entityType.GetTableName()}`");
            sb.AppendLine();

            // Direct Properties
            var properties = entityType.GetProperties()
                .OrderBy(p => p.Name)
                .ToList();

            if (properties.Any())
            {
                sb.AppendLine("### Direct Properties");
                sb.AppendLine();
                sb.AppendLine("| Property Name | CLR Type | SQL Type | Nullable | Primary Key | Foreign Key |");
                sb.AppendLine("|---------------|----------|----------|----------|-------------|-------------|");

                foreach (var property in properties)
                {
                    var clrType = property.ClrType.Name;
                    var sqlType = property.GetColumnType();
                    var isNullable = property.IsNullable ? "Yes" : "No";
                    var isPrimaryKey = property.IsPrimaryKey() ? "?" : "";
                    var isForeignKey = property.IsForeignKey() ? "?" : "";

                    sb.AppendLine($"| `{property.Name}` | {clrType} | {sqlType} | {isNullable} | {isPrimaryKey} | {isForeignKey} |");
                }

                sb.AppendLine();
            }

            // Navigation Properties
            var navigations = entityType.GetNavigations()
                .OrderBy(n => n.Name)
                .ToList();

            if (navigations.Any())
            {
                sb.AppendLine("### Navigation Properties");
                sb.AppendLine();
                sb.AppendLine("| Navigation Name | Target Type | Relationship Type | Foreign Key |");
                sb.AppendLine("|-----------------|-------------|-------------------|-------------|");

                foreach (var nav in navigations)
                {
                    var targetType = nav.TargetEntityType.ClrType.Name;
                    var relationType = nav.IsCollection ? "Collection" : "Reference";
                    var foreignKey = nav.ForeignKey?.Properties.FirstOrDefault()?.Name ?? "";

                    sb.AppendLine($"| `{nav.Name}` | {targetType} | {relationType} | {foreignKey} |");
                }

                sb.AppendLine();
            }

            // Skip relationships
            var skipNavigations = entityType.GetSkipNavigations().ToList();
            if (skipNavigations.Any())
            {
                sb.AppendLine("### Many-to-Many Relationships");
                sb.AppendLine();
                sb.AppendLine("| Navigation Name | Target Type | Join Table |");
                sb.AppendLine("|-----------------|-------------|------------|");

                foreach (var skip in skipNavigations)
                {
                    var targetType = skip.TargetEntityType.ClrType.Name;
                    var joinTable = skip.JoinEntityType?.GetTableName() ?? "N/A";

                    sb.AppendLine($"| `{skip.Name}` | {targetType} | {joinTable} |");
                }

                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Summary Statistics
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"**Total Entities:** {entityTypes.Count}");
        sb.AppendLine($"**Total Tables:** {entityTypes.Count(e => e.GetTableName() != null)}");
        
        var totalProperties = entityTypes.Sum(e => e.GetProperties().Count());
        sb.AppendLine($"**Total Direct Properties:** {totalProperties}");
        
        var totalNavigations = entityTypes.Sum(e => e.GetNavigations().Count());
        sb.AppendLine($"**Total Navigation Properties:** {totalNavigations}");
        
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Generates field metadata for a specific entity (for report builder)
    /// </summary>
    public List<EntityFieldMetadata> GetFieldMetadataForEntity(string entityTypeName)
    {
        var entityType = _context.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == entityTypeName);

        if (entityType == null)
            return new List<EntityFieldMetadata>();

        var metadata = new List<EntityFieldMetadata>();

        // Add direct properties
        foreach (var property in entityType.GetProperties().OrderBy(p => p.Name))
        {
            metadata.Add(new EntityFieldMetadata
            {
                EntityType = entityTypeName,
                FieldPath = property.Name,
                DisplayName = property.Name,
                DataType = property.ClrType.Name,
                IsNullable = property.IsNullable,
                IsNavigationProperty = false,
                IsPrimaryKey = property.IsPrimaryKey(),
                IsForeignKey = property.IsForeignKey(),
                Category = DetermineCategory(property.Name)
            });
        }

        // Add navigation properties (one level deep only for now)
        foreach (var nav in entityType.GetNavigations().OrderBy(n => n.Name))
        {
            var targetType = nav.TargetEntityType.ClrType.Name;
            var isCollection = nav.IsCollection;

            metadata.Add(new EntityFieldMetadata
            {
                EntityType = entityTypeName,
                FieldPath = nav.Name,
                DisplayName = nav.Name,
                DataType = isCollection ? $"Collection<{targetType}>" : targetType,
                IsNullable = true,
                IsNavigationProperty = true,
                IsCollection = isCollection,
                Category = "Navigation Properties"
            });
        }

        return metadata;
    }

    private string DetermineCategory(string propertyName)
    {
        // Categorize fields for better organization in report builder
        if (propertyName.Contains("Date") || propertyName.Contains("Time"))
            return "Dates & Times";
        
        if (propertyName.Contains("Id") && propertyName != "Id")
            return "Foreign Keys";
        
        if (propertyName.Contains("Jurisdiction"))
            return "Jurisdiction";
        
        if (propertyName.Contains("Address") || propertyName.Contains("Street") || 
            propertyName.Contains("City") || propertyName.Contains("State") || 
            propertyName.Contains("Postcode"))
            return "Address";
        
        if (propertyName.Contains("Phone") || propertyName.Contains("Email") || 
            propertyName.Contains("Contact"))
            return "Contact Information";
        
        if (propertyName == "Id")
            return "System";
        
        if (propertyName.Contains("Created") || propertyName.Contains("Modified"))
            return "Audit";

        return "General";
    }
}

/// <summary>
/// Metadata about a field for report building
/// </summary>
public class EntityFieldMetadata
{
    public string EntityType { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsNavigationProperty { get; set; }
    public bool IsCollection { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFilterable { get; set; } = true;
    public bool IsGroupable { get; set; } = true;
    public bool IsAggregatable { get; set; } = true;
}
