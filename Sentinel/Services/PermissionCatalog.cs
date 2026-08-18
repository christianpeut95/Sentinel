using Sentinel.Models;

namespace Sentinel.Services;

/// <summary>
/// The complete, deliberately explicit set of authorisation boundaries supported by Sentinel.
/// Add a definition here when a new permission is enforced in the application.
/// </summary>
public static class PermissionCatalog
{
    public sealed record Definition(
        PermissionModule Module,
        PermissionAction Action,
        string Description);

    public static readonly Definition[] Definitions =
    [
        new(PermissionModule.Patient, PermissionAction.View, "View patient records"),
        new(PermissionModule.Patient, PermissionAction.Create, "Create patient records"),
        new(PermissionModule.Patient, PermissionAction.Edit, "Edit patient records"),
        new(PermissionModule.Patient, PermissionAction.Delete, "Delete patient records"),
        new(PermissionModule.Patient, PermissionAction.Search, "Search patient records"),
        new(PermissionModule.Patient, PermissionAction.Merge, "Merge patient records"),

        new(PermissionModule.Case, PermissionAction.View, "View case and contact records"),
        new(PermissionModule.Case, PermissionAction.Create, "Create case and contact records"),
        new(PermissionModule.Case, PermissionAction.Edit, "Edit case and contact records"),
        new(PermissionModule.Case, PermissionAction.Delete, "Delete case and contact records"),
        new(PermissionModule.Case, PermissionAction.Search, "Search case and contact records"),

        new(PermissionModule.Settings, PermissionAction.View, "View settings"),
        new(PermissionModule.Settings, PermissionAction.Create, "Create settings lookup records"),
        new(PermissionModule.Settings, PermissionAction.Edit, "Edit settings and case definitions"),
        new(PermissionModule.Settings, PermissionAction.ManagePermissions, "Manage system and disease access permissions"),
        new(PermissionModule.Settings, PermissionAction.ManageCustomFields, "Manage custom fields"),
        new(PermissionModule.Settings, PermissionAction.ManageCustomLookups, "Manage custom lookup tables"),
        new(PermissionModule.Settings, PermissionAction.ManageSystemLookups, "Manage system lookup tables"),
        new(PermissionModule.Settings, PermissionAction.ManageOrganization, "Manage organisation and system settings"),

        new(PermissionModule.Audit, PermissionAction.View, "View audit history"),

        new(PermissionModule.User, PermissionAction.View, "View user accounts"),
        new(PermissionModule.User, PermissionAction.Create, "Create user accounts"),
        new(PermissionModule.User, PermissionAction.Edit, "Edit user accounts"),
        new(PermissionModule.User, PermissionAction.Delete, "Delete user accounts"),
        new(PermissionModule.User, PermissionAction.ManagePermissions, "Manage user and role permissions"),

        new(PermissionModule.Report, PermissionAction.View, "View reports"),
        new(PermissionModule.Report, PermissionAction.Create, "Create reports"),
        new(PermissionModule.Report, PermissionAction.Edit, "Edit reports"),

        new(PermissionModule.Laboratory, PermissionAction.View, "View laboratory results"),
        new(PermissionModule.Laboratory, PermissionAction.Create, "Create laboratory results"),
        new(PermissionModule.Laboratory, PermissionAction.Edit, "Edit laboratory results"),
        new(PermissionModule.Laboratory, PermissionAction.Delete, "Delete laboratory results"),

        new(PermissionModule.HL7, PermissionAction.View, "View HL7 messages, diagnostics, and monitoring"),
        new(PermissionModule.HL7, PermissionAction.Configure, "Configure HL7 laboratories and mappings"),
        new(PermissionModule.HL7, PermissionAction.Process, "Reprocess and review HL7 messages"),
        new(PermissionModule.HL7, PermissionAction.GenerateTestFiles, "Generate HL7 test messages"),

        new(PermissionModule.Task, PermissionAction.Create, "Create case tasks"),
        new(PermissionModule.Task, PermissionAction.View, "View case tasks"),

        new(PermissionModule.Outbreak, PermissionAction.View, "View outbreaks"),
        new(PermissionModule.Outbreak, PermissionAction.Create, "Create outbreaks"),
        new(PermissionModule.Outbreak, PermissionAction.Edit, "Edit outbreaks"),
        new(PermissionModule.Outbreak, PermissionAction.Delete, "Delete outbreaks"),
        new(PermissionModule.Outbreak, PermissionAction.Export, "Export outbreak line lists"),

        new(PermissionModule.Survey, PermissionAction.View, "View surveys"),
        new(PermissionModule.Survey, PermissionAction.Create, "Create surveys"),
        new(PermissionModule.Survey, PermissionAction.Edit, "Edit surveys and mappings"),
        new(PermissionModule.Survey, PermissionAction.Complete, "Complete surveys"),

        new(PermissionModule.Location, PermissionAction.View, "View locations"),
        new(PermissionModule.Location, PermissionAction.Create, "Create locations"),
        new(PermissionModule.Location, PermissionAction.Edit, "Edit locations"),
        new(PermissionModule.Location, PermissionAction.Delete, "Delete locations"),
        new(PermissionModule.Location, PermissionAction.Import, "Import jurisdictions and locations"),
        new(PermissionModule.Location, PermissionAction.Upload, "Upload jurisdiction boundary shapefiles"),
        new(PermissionModule.Location, PermissionAction.ImportPopulation, "Import jurisdiction population data"),

        new(PermissionModule.Event, PermissionAction.View, "View events"),
        new(PermissionModule.Event, PermissionAction.Create, "Create events"),
        new(PermissionModule.Event, PermissionAction.Edit, "Edit events"),
        new(PermissionModule.Event, PermissionAction.Delete, "Delete events"),

        new(PermissionModule.Exposure, PermissionAction.View, "View exposures"),
        new(PermissionModule.Exposure, PermissionAction.Create, "Create exposures"),
        new(PermissionModule.Exposure, PermissionAction.Edit, "Edit exposures"),
        new(PermissionModule.Exposure, PermissionAction.Delete, "Delete exposures"),

        new(PermissionModule.Contact, PermissionAction.Import, "Bulk import and create contacts"),

        new(PermissionModule.Occupation, PermissionAction.Import, "Import occupation classifications")
    ];
}
