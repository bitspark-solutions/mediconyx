# Blockchain Integration - Sequence Diagrams

## Payment Transaction Recording Sequence

```mermaid
sequenceDiagram
    participant User
    participant PaymentController
    participant PaymentService
    participant BlockchainService
    participant Ethereum
    participant Database
    participant BackgroundJob

    User->>PaymentController: POST /api/payments
    PaymentController->>PaymentService: CreatePayment()
    PaymentService->>Database: Save Payment
    Database-->>PaymentService: Payment Created
    
    PaymentService->>BlockchainService: RecordPaymentTransactionAsync()
    BlockchainService->>Database: Load Payment Details
    Database-->>BlockchainService: Payment Data
    
    BlockchainService->>BlockchainService: Generate Invoice Hash (SHA-256)
    BlockchainService->>Ethereum: Estimate Gas
    Ethereum-->>BlockchainService: Gas Estimate
    
    BlockchainService->>BlockchainService: Sign Transaction (Wallet)
    BlockchainService->>Ethereum: Send Transaction
    Ethereum-->>BlockchainService: Transaction Hash
    
    BlockchainService->>Database: Save Transaction Hash
    Database-->>BlockchainService: Saved
    
    BlockchainService-->>PaymentService: Transaction Hash
    PaymentService-->>PaymentController: Payment Response (with Tx Hash)
    PaymentController-->>User: 201 Created
    
    Note over BackgroundJob: Background Process
    BackgroundJob->>Ethereum: Poll Transaction Status
    Ethereum-->>BackgroundJob: Transaction Receipt
    
    alt Transaction Confirmed
        BackgroundJob->>Database: Update Status = Confirmed
        BackgroundJob->>Database: Update Payment.BlockchainVerified = true
    else Transaction Failed
        BackgroundJob->>Database: Update Status = Failed
        BackgroundJob->>Database: Log Error
    end
```

## Medical Report Hash Storage Sequence

```mermaid
sequenceDiagram
    participant Doctor
    participant ReportController
    participant ReportService
    participant BlockchainService
    participant Ethereum
    participant Database
    participant BackgroundJob

    Doctor->>ReportController: POST /api/reports
    ReportController->>ReportService: GenerateReport()
    ReportService->>Database: Save Medical Report
    Database-->>ReportService: Report Created
    
    ReportService->>ReportService: Generate Document Hash (SHA-256)
    ReportService->>BlockchainService: StoreReportHashAsync()
    
    BlockchainService->>Database: Load Report Details
    Database-->>BlockchainService: Report Data
    
    BlockchainService->>Ethereum: Estimate Gas
    Ethereum-->>BlockchainService: Gas Estimate
    
    BlockchainService->>BlockchainService: Sign Transaction (Wallet)
    BlockchainService->>Ethereum: Call storeReportHash()
    Ethereum-->>BlockchainService: Transaction Hash
    
    BlockchainService->>Database: Save Transaction Hash
    Database-->>BlockchainService: Saved
    
    BlockchainService-->>ReportService: Transaction Hash
    ReportService-->>ReportController: Report Response (with Tx Hash)
    ReportController-->>Doctor: 201 Created
    
    Note over BackgroundJob: Background Process
    BackgroundJob->>Ethereum: Poll Transaction Status
    Ethereum-->>BackgroundJob: Transaction Receipt
    
    alt Transaction Confirmed
        BackgroundJob->>Database: Update Status = Confirmed
        BackgroundJob->>Database: Update Report.BlockchainVerified = true
    else Transaction Failed
        BackgroundJob->>Database: Update Status = Failed
        BackgroundJob->>Database: Log Error
    end
```

## Payment Verification Sequence

```mermaid
sequenceDiagram
    participant User
    participant BlockchainController
    participant BlockchainService
    participant Ethereum
    participant Database

    User->>BlockchainController: POST /api/blockchain/verify-payment
    Note over BlockchainController: { paymentId: 123, transactionHash: "0x..." }
    
    BlockchainController->>BlockchainService: VerifyPaymentTransactionAsync()
    
    BlockchainService->>Ethereum: Get Transaction Receipt
    Ethereum-->>BlockchainService: Receipt (status, blockNumber)
    
    alt Transaction Not Found or Failed
        BlockchainService-->>BlockchainController: Verified = false
        BlockchainController-->>User: 200 OK { verified: false }
    else Transaction Confirmed
        BlockchainService->>Ethereum: Query PaymentContract.verifyPayment()
        Ethereum-->>BlockchainService: Payment Data from Contract
        
        BlockchainService->>Database: Load Payment from DB
        Database-->>BlockchainService: Payment Data
        
        BlockchainService->>BlockchainService: Compare DB vs Blockchain Data
        
        alt Data Matches
            BlockchainService->>Database: Update BlockchainVerified = true
            BlockchainService-->>BlockchainController: Verified = true
            BlockchainController-->>User: 200 OK { verified: true, details }
        else Data Mismatch
            BlockchainService-->>BlockchainController: Verified = false (mismatch)
            BlockchainController-->>User: 200 OK { verified: false, error: "Data mismatch" }
        end
    end
```

## Public Report Verification Sequence

```mermaid
sequenceDiagram
    participant Patient
    participant PublicController
    participant BlockchainService
    participant Ethereum
    participant Database

    Patient->>PublicController: GET /api/public/verify-report/456?hash=0xabcd...
    Note over PublicController: No Authentication Required
    
    PublicController->>BlockchainService: VerifyReportHashAsync()
    
    BlockchainService->>Database: Load Report by ID
    Database-->>BlockchainService: Report Data
    
    alt Report Not Found
        BlockchainService-->>PublicController: Error: Report not found
        PublicController-->>Patient: 404 Not Found
    else Report Found
        BlockchainService->>BlockchainService: Re-hash Report Content
        Note over BlockchainService: Compare with provided hash
        
        BlockchainService->>Ethereum: Query MedicalRecordContract.verifyRecord()
        Note over Ethereum: verifyRecord(reportId, documentHash)
        Ethereum-->>BlockchainService: Verification Result (bool)
        
        alt Hash Matches Blockchain
            BlockchainService->>Ethereum: Get Record Details
            Ethereum-->>BlockchainService: Record Data (timestamp, blockNumber)
            
            BlockchainService-->>PublicController: Verified = true
            PublicController-->>Patient: 200 OK { verified: true, details }
        else Hash Mismatch
            BlockchainService-->>PublicController: Verified = false
            PublicController-->>Patient: 200 OK { verified: false, message: "Hash mismatch" }
        end
    end
```

## Batch Transaction Processing Sequence

```mermaid
sequenceDiagram
    participant Scheduler
    participant BatchProcessor
    participant TransactionQueue
    participant BlockchainService
    participant Ethereum
    participant Database

    Note over Scheduler: Runs Hourly/Daily
    
    Scheduler->>BatchProcessor: Process Batch
    
    BatchProcessor->>TransactionQueue: Get Pending Transactions
    TransactionQueue-->>BatchProcessor: List of Pending Transactions
    
    BatchProcessor->>BatchProcessor: Group by Transaction Type
    BatchProcessor->>BatchProcessor: Create Batch Transaction
    
    BatchProcessor->>BlockchainService: Send Batch Transaction
    BlockchainService->>Ethereum: Estimate Gas for Batch
    Ethereum-->>BlockchainService: Gas Estimate
    
    BlockchainService->>BlockchainService: Sign Batch Transaction
    BlockchainService->>Ethereum: Send Batch Transaction
    Ethereum-->>BlockchainService: Transaction Hash
    
    BlockchainService->>Database: Save Batch Transaction Hash
    Database-->>BlockchainService: Saved
    
    Note over BatchProcessor: Process Individual Results
    loop For Each Transaction in Batch
        BatchProcessor->>Database: Update Individual Transaction
        Note over Database: Link to Batch Transaction Hash
    end
    
    Note over Scheduler: Background Polling
    Scheduler->>Ethereum: Poll Batch Transaction Status
    Ethereum-->>Scheduler: Transaction Receipt
    
    alt Batch Confirmed
        Scheduler->>Database: Update All Transactions = Confirmed
    else Batch Failed
        Scheduler->>Database: Update All Transactions = Failed
        Scheduler->>TransactionQueue: Re-queue Individual Transactions
    end
```

## Error Handling & Retry Sequence

```mermaid
sequenceDiagram
    participant Service
    participant BlockchainService
    participant CircuitBreaker
    participant Ethereum
    participant RetryQueue

    Service->>BlockchainService: Record Transaction
    
    BlockchainService->>CircuitBreaker: Check Circuit State
    
    alt Circuit Open
        CircuitBreaker-->>BlockchainService: Circuit Open
        BlockchainService->>RetryQueue: Queue Transaction
        BlockchainService-->>Service: Queued (will retry later)
    else Circuit Closed
        BlockchainService->>Ethereum: Send Transaction
        
        alt Success
            Ethereum-->>BlockchainService: Transaction Hash
            CircuitBreaker->>CircuitBreaker: Record Success
            BlockchainService-->>Service: Success
        else Network Error
            Ethereum-->>BlockchainService: Network Error
            CircuitBreaker->>CircuitBreaker: Record Failure
            
            alt Failure Count < Threshold
                BlockchainService->>BlockchainService: Retry with Backoff
                Note over BlockchainService: Exponential Backoff (1s, 2s, 4s)
                BlockchainService->>Ethereum: Retry Transaction
            else Failure Count >= Threshold
                CircuitBreaker->>CircuitBreaker: Open Circuit
                BlockchainService->>RetryQueue: Queue Transaction
                BlockchainService-->>Service: Queued (circuit open)
            end
        end
    end
    
    Note over RetryQueue: Background Retry Process
    RetryQueue->>CircuitBreaker: Check Circuit (Periodic)
    
    alt Circuit Half-Open
        RetryQueue->>Ethereum: Retry Queued Transaction
        alt Success
            CircuitBreaker->>CircuitBreaker: Close Circuit
        else Failure
            CircuitBreaker->>CircuitBreaker: Keep Circuit Open
        end
    end
```


