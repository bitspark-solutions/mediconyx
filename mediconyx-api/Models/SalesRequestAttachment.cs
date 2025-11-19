namespace mediconyx_api.Models;

public class SalesRequestAttachment
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty; // Will store fileId from storage service
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    
    // Document classification
    public DocumentType? DocumentType { get; set; }
    public bool IsMandatory { get; set; } = false;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public enum DocumentType
{
    HospitalLicense = 0,
    TaxCertificate = 1,
    BusinessRegistration = 2,
    DirectorIdProof = 3,
    BankAccountDetails = 4,
    SignedContract = 5,
    PaymentReceipt = 6,
    IntegrationRequirementsDoc = 7,
    DataMigrationTemplate = 8,
    Other = 9
}
