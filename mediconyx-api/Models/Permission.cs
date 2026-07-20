using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

/// <summary>
/// A single permission flag. Flat list — checked via [HasPermission("admission.create")] on controllers.
/// Permissions are GLOBAL (not tenant-scoped) so the seed matrix applies to all tenants.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    /// <summary>
    /// Stable dotted name used in code & [HasPermission] attributes.
    /// Examples: "admission.create", "admission.discharge", "bed.assign",
    ///           "user.manage", "doctor.profile.edit", "telemedicine.start".
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Coarse category for UI grouping. E.g. "admissions", "billing", "users".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Join table — which permissions does each role have by default.
/// Seeded once on first migration. Per-tenant overrides come later if needed.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

/// <summary>
/// Catalogue of permission names + which role gets them by default.
/// Keep this list stable — referenced by [HasPermission] attributes and by tests.
/// </summary>
public static class PermissionCatalog
{
    public const string ADMISSION_CREATE       = "admission.create";
    public const string ADMISSION_DISCHARGE    = "admission.discharge";
    public const string ADMISSION_TRANSFER     = "admission.transfer";
    public const string ADMISSION_VIEW         = "admission.view";
    public const string BED_ASSIGN             = "bed.assign";
    public const string BED_MANAGE             = "bed.manage";
    public const string WARD_MANAGE            = "ward.manage";
    public const string PATIENT_CREATE         = "patient.create";
    public const string PATIENT_VIEW           = "patient.view";
    public const string PATIENT_EDIT           = "patient.edit";
    public const string APPOINTMENT_CREATE     = "appointment.create";
    public const string APPOINTMENT_VIEW       = "appointment.view";
    public const string APPOINTMENT_CANCEL     = "appointment.cancel";
    public const string DOCTOR_PROFILE_EDIT    = "doctor.profile.edit";
    public const string DOCTOR_PROFILE_VIEW    = "doctor.profile.view";
    public const string PRESCRIPTION_CREATE    = "prescription.create";
    public const string MEDICAL_NOTE_CREATE    = "medical_note.create";
    public const string VITAL_SIGN_RECORD      = "vital_sign.record";
    public const string FILE_UPLOAD            = "file.upload";
    public const string INVOICE_VIEW           = "invoice.view";
    public const string INVOICE_CREATE         = "invoice.create";
    public const string PAYMENT_RECORD         = "payment.record";
    public const string USER_MANAGE            = "user.manage";
    public const string USER_VIEW              = "user.view";
    public const string USER_EDIT_OWN          = "user.edit_own";
    public const string TENANT_SETTINGS_EDIT   = "tenant.settings.edit";
    public const string TENANT_SETTINGS_VIEW   = "tenant.settings.view";
    public const string DEPARTMENT_MANAGE      = "department.manage";
    public const string SERVICE_MANAGE         = "service.manage";
    public const string TELEMEDICINE_START     = "telemedicine.start";
    public const string TELEMEDICINE_JOIN      = "telemedicine.join";
    public const string SALES_PIPELINE_VIEW    = "sales.pipeline.view";
    public const string SALES_REQUEST_CLAIM    = "sales.request.claim";
    public const string AUDIT_LOG_VIEW         = "audit.log.view";

    /// <summary>Full list of all known permission names — used by seeder.</summary>
    public static readonly IReadOnlyList<(string Name, string Category, string? Description)> All = new List<(string, string, string?)>
    {
        // Admissions / IPD
        (ADMISSION_CREATE,      "admissions", "Admit a patient to a bed"),
        (ADMISSION_DISCHARGE,   "admissions", "Discharge an admitted patient"),
        (ADMISSION_TRANSFER,    "admissions", "Transfer a patient between beds"),
        (ADMISSION_VIEW,        "admissions", "View admissions and discharge summaries"),
        (BED_ASSIGN,            "admissions", "Assign a patient to a bed"),
        (BED_MANAGE,            "admissions", "Create/update beds and change bed status"),
        (WARD_MANAGE,           "admissions", "Create/update wards (ICU, PICU, DayCare, etc.)"),

        // Patients
        (PATIENT_CREATE,        "patients",   "Register a new patient"),
        (PATIENT_VIEW,          "patients",   "View patient records"),
        (PATIENT_EDIT,          "patients",   "Edit patient records"),

        // Appointments
        (APPOINTMENT_CREATE,    "appointments", "Book an appointment"),
        (APPOINTMENT_VIEW,      "appointments", "View appointments"),
        (APPOINTMENT_CANCEL,    "appointments", "Cancel an appointment"),

        // Doctors
        (DOCTOR_PROFILE_EDIT,   "doctors",    "Edit a doctor profile"),
        (DOCTOR_PROFILE_VIEW,   "doctors",    "View doctor profiles (public + internal)"),

        // Clinical
        (PRESCRIPTION_CREATE,   "clinical",   "Create a prescription"),
        (MEDICAL_NOTE_CREATE,   "clinical",   "Write a medical note"),
        (VITAL_SIGN_RECORD,     "clinical",   "Record vital signs"),
        (FILE_UPLOAD,           "clinical",   "Upload patient file attachments"),

        // Billing
        (INVOICE_VIEW,          "billing",    "View invoices"),
        (INVOICE_CREATE,        "billing",    "Create an invoice"),
        (PAYMENT_RECORD,        "billing",    "Record a payment"),

        // Users & Access
        (USER_MANAGE,           "users",      "Create/edit/deactivate users in own tenant"),
        (USER_VIEW,             "users",      "List and view users in own tenant"),
        (USER_EDIT_OWN,         "users",      "Edit own profile / change own password"),

        // Tenant
        (TENANT_SETTINGS_EDIT,  "tenant",     "Edit hospital/tenant settings, logo, modules"),
        (TENANT_SETTINGS_VIEW,  "tenant",     "View hospital/tenant settings"),

        // Departments & services
        (DEPARTMENT_MANAGE,     "operations", "Create/edit departments"),
        (SERVICE_MANAGE,        "operations", "Create/edit hospital services"),

        // Telemedicine
        (TELEMEDICINE_START,    "telemedicine", "Start a telemedicine session as a doctor"),
        (TELEMEDICINE_JOIN,     "telemedicine", "Join a telemedicine session as a patient"),

        // Sales (Master / Sales only)
        (SALES_PIPELINE_VIEW,   "sales",      "View the sales pipeline"),
        (SALES_REQUEST_CLAIM,   "sales",      "Claim a sales request"),

        // Audit
        (AUDIT_LOG_VIEW,        "audit",      "View audit logs"),
    };

    /// <summary>
    /// Default role -> permission matrix. Seeded on first run.
    /// Hospital admin gets everything in their tenant. Doctor/Patient/Nurse/Receptionist get narrower sets.
    /// </summary>
    public static readonly IReadOnlyDictionary<UserRole, IReadOnlyList<string>> DefaultRolePermissions =
        new Dictionary<UserRole, IReadOnlyList<string>>
    {
        // Master / Sales see the platform-wide stuff
        [UserRole.Master] = new[]
        {
            USER_VIEW, USER_MANAGE, USER_EDIT_OWN,
            TENANT_SETTINGS_VIEW, TENANT_SETTINGS_EDIT,
            SALES_PIPELINE_VIEW, SALES_REQUEST_CLAIM,
            AUDIT_LOG_VIEW,
            PATIENT_VIEW,
        },
        [UserRole.Sales] = new[]
        {
            SALES_PIPELINE_VIEW, SALES_REQUEST_CLAIM,
            USER_EDIT_OWN,
        },

        // Hospital admin — owns everything inside their tenant
        [UserRole.Admin] = new[]
        {
            USER_VIEW, USER_MANAGE, USER_EDIT_OWN,
            TENANT_SETTINGS_VIEW, TENANT_SETTINGS_EDIT,
            DEPARTMENT_MANAGE, SERVICE_MANAGE,
            WARD_MANAGE, BED_MANAGE, BED_ASSIGN,
            ADMISSION_CREATE, ADMISSION_DISCHARGE, ADMISSION_TRANSFER, ADMISSION_VIEW,
            PATIENT_CREATE, PATIENT_VIEW, PATIENT_EDIT,
            APPOINTMENT_CREATE, APPOINTMENT_VIEW, APPOINTMENT_CANCEL,
            DOCTOR_PROFILE_VIEW, DOCTOR_PROFILE_EDIT,
            PRESCRIPTION_CREATE, MEDICAL_NOTE_CREATE, VITAL_SIGN_RECORD, FILE_UPLOAD,
            INVOICE_VIEW, INVOICE_CREATE, PAYMENT_RECORD,
            AUDIT_LOG_VIEW,
        },

        // Doctor — clinical + view
        [UserRole.Doctor] = new[]
        {
            USER_EDIT_OWN,
            PATIENT_VIEW,
            APPOINTMENT_CREATE, APPOINTMENT_VIEW, APPOINTMENT_CANCEL,
            DOCTOR_PROFILE_VIEW, DOCTOR_PROFILE_EDIT,
            PRESCRIPTION_CREATE, MEDICAL_NOTE_CREATE, FILE_UPLOAD,
            ADMISSION_CREATE, ADMISSION_DISCHARGE, ADMISSION_TRANSFER, ADMISSION_VIEW,
            BED_ASSIGN, BED_MANAGE,
            VITAL_SIGN_RECORD,
            INVOICE_VIEW,
            TELEMEDICINE_START,
        },

        // Nurse — assists, records vitals, supports admissions
        [UserRole.Nurse] = new[]
        {
            USER_EDIT_OWN,
            PATIENT_VIEW,
            APPOINTMENT_VIEW,
            VITAL_SIGN_RECORD,
            FILE_UPLOAD,
            ADMISSION_VIEW,
            BED_ASSIGN,
        },

        // Receptionist — front desk, registers patients, books appointments
        [UserRole.Receptionist] = new[]
        {
            USER_EDIT_OWN,
            PATIENT_CREATE, PATIENT_VIEW, PATIENT_EDIT,
            APPOINTMENT_CREATE, APPOINTMENT_VIEW, APPOINTMENT_CANCEL,
            ADMISSION_CREATE, ADMISSION_VIEW, BED_ASSIGN,
        },

        // Accountant — billing only
        [UserRole.Accountant] = new[]
        {
            USER_EDIT_OWN,
            PATIENT_VIEW,
            INVOICE_VIEW, INVOICE_CREATE, PAYMENT_RECORD,
        },

        // Patient — own data + telemedicine
        [UserRole.Patient] = new[]
        {
            USER_EDIT_OWN,
            PATIENT_VIEW, // own only — enforced at controller level
            APPOINTMENT_CREATE, APPOINTMENT_VIEW, APPOINTMENT_CANCEL,
            DOCTOR_PROFILE_VIEW,
            FILE_UPLOAD,
            TELEMEDICINE_JOIN,
        },
    };
}
