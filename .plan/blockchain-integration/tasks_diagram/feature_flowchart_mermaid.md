# Blockchain Integration - Flowcharts

## Payment Recording Flowchart

```mermaid
flowchart TD
    Start([User Records Payment]) --> Validate{Validate Payment Data}
    Validate -->|Invalid| Error1([Return Validation Error])
    Validate -->|Valid| SaveDB[Save Payment to Database]
    
    SaveDB --> GenerateHash[Generate Invoice Hash SHA-256]
    GenerateHash --> CheckCircuit{Circuit Breaker Status}
    
    CheckCircuit -->|Open| QueueTx[Queue Transaction]
    QueueTx --> ReturnSuccess1([Return Success - Queued])
    
    CheckCircuit -->|Closed| EstimateGas[Estimate Gas]
    EstimateGas --> SignTx[Sign Transaction with Wallet]
    SignTx --> SendTx[Send Transaction to Blockchain]
    
    SendTx --> CheckResult{Transaction Result}
    CheckResult -->|Network Error| Retry{Retry Count < 3?}
    Retry -->|Yes| Backoff[Wait with Exponential Backoff]
    Backoff --> EstimateGas
    Retry -->|No| OpenCircuit[Open Circuit Breaker]
    OpenCircuit --> QueueTx
    
    CheckResult -->|Success| SaveTxHash[Save Transaction Hash to DB]
    SaveTxHash --> StartPolling[Start Background Polling]
    StartPolling --> ReturnSuccess2([Return Success - Pending])
    
    CheckResult -->|Contract Error| LogError[Log Error]
    LogError --> ReturnError([Return Error to User])
    
    style Start fill:#e1f5ff
    style ReturnSuccess1 fill:#d4edda
    style ReturnSuccess2 fill:#d4edda
    style Error1 fill:#f8d7da
    style ReturnError fill:#f8d7da
```

## Report Verification Flowchart

```mermaid
flowchart TD
    Start([User Requests Report Verification]) --> LoadReport[Load Report from Database]
    LoadReport --> CheckExists{Report Exists?}
    
    CheckExists -->|No| Return404([Return 404 Not Found])
    CheckExists -->|Yes| Rehash[Re-hash Report Content]
    
    Rehash --> QueryContract[Query Blockchain Contract]
    QueryContract --> CheckConnection{Blockchain Available?}
    
    CheckConnection -->|No| ReturnError1([Return Error: Blockchain Unavailable])
    CheckConnection -->|Yes| GetRecord[Get Record from Contract]
    
    GetRecord --> CheckRecord{Record Exists on Blockchain?}
    CheckRecord -->|No| ReturnNotVerified([Return: Not Verified])
    
    CheckRecord -->|Yes| CompareHash{Hash Matches?}
    CompareHash -->|No| ReturnMismatch([Return: Hash Mismatch])
    CompareHash -->|Yes| UpdateDB[Update Verification Status in DB]
    
    UpdateDB --> ReturnVerified([Return: Verified with Details])
    
    style Start fill:#e1f5ff
    style ReturnVerified fill:#d4edda
    style ReturnNotVerified fill:#fff3cd
    style ReturnMismatch fill:#f8d7da
    style Return404 fill:#f8d7da
    style ReturnError1 fill:#f8d7da
```

## Transaction Status Polling Flowchart

```mermaid
flowchart TD
    Start([Background Job Started]) --> GetPending[Get Pending Transactions from DB]
    GetPending --> CheckEmpty{Any Pending?}
    
    CheckEmpty -->|No| Wait[Wait 30 Seconds]
    Wait --> GetPending
    
    CheckEmpty -->|Yes| LoopStart{For Each Transaction}
    LoopStart --> GetReceipt[Get Transaction Receipt from Blockchain]
    
    GetReceipt --> CheckReceipt{Receipt Found?}
    CheckReceipt -->|No| CheckAge{Transaction Age > 24h?}
    CheckAge -->|Yes| MarkFailed[Mark as Failed]
    CheckAge -->|No| NextTx[Next Transaction]
    
    CheckReceipt -->|Yes| CheckStatus{Status == 1?}
    CheckStatus -->|No| MarkFailed
    CheckStatus -->|Yes| UpdateConfirmed[Update Status = Confirmed]
    
    UpdateConfirmed --> UpdatePayment[Update Payment/Report Verified = true]
    UpdatePayment --> LogSuccess[Log Success]
    LogSuccess --> NextTx
    
    MarkFailed --> LogError[Log Error]
    LogError --> NextTx
    
    NextTx --> CheckMore{More Transactions?}
    CheckMore -->|Yes| LoopStart
    CheckMore -->|No| Wait
    
    style Start fill:#e1f5ff
    style UpdateConfirmed fill:#d4edda
    style MarkFailed fill:#f8d7da
```

## Batch Processing Flowchart

```mermaid
flowchart TD
    Start([Scheduler Triggered]) --> GetQueue[Get Queued Transactions]
    GetQueue --> CheckEmpty{Queue Empty?}
    
    CheckEmpty -->|Yes| End([End])
    CheckEmpty -->|No| GroupByType[Group by Transaction Type]
    
    GroupByType --> CreateBatch[Create Batch Transaction]
    CreateBatch --> EstimateGas[Estimate Gas for Batch]
    EstimateGas --> CheckGas{Gas Estimate OK?}
    
    CheckGas -->|Too High| SplitBatch[Split into Smaller Batches]
    SplitBatch --> CreateBatch
    
    CheckGas -->|OK| SignBatch[Sign Batch Transaction]
    SignBatch --> SendBatch[Send to Blockchain]
    
    SendBatch --> CheckResult{Result}
    CheckResult -->|Success| SaveBatchHash[Save Batch Transaction Hash]
    CheckResult -->|Error| LogError[Log Error]
    LogError --> Requeue[Re-queue Individual Transactions]
    Requeue --> End
    
    SaveBatchHash --> LinkTransactions[Link Individual Transactions to Batch]
    LinkTransactions --> StartPolling[Start Polling Batch Status]
    StartPolling --> End
    
    style Start fill:#e1f5ff
    style SaveBatchHash fill:#d4edda
    style LogError fill:#f8d7da
    style Requeue fill:#fff3cd
```

## Circuit Breaker State Flowchart

```mermaid
flowchart TD
    Start([Service Request]) --> CheckState{Circuit State}
    
    CheckState -->|Closed| Attempt[Attempt Operation]
    Attempt --> CheckResult{Result}
    
    CheckResult -->|Success| RecordSuccess[Record Success]
    RecordSuccess --> ResetCount[Reset Failure Count]
    ResetCount --> ReturnSuccess([Return Success])
    
    CheckResult -->|Failure| IncrementFail[Increment Failure Count]
    IncrementFail --> CheckThreshold{Failures >= Threshold?}
    
    CheckThreshold -->|No| ReturnError1([Return Error])
    CheckThreshold -->|Yes| OpenCircuit[Open Circuit]
    OpenCircuit --> StartTimer[Start Timer 60s]
    StartTimer --> ReturnError2([Return Error: Circuit Open])
    
    CheckState -->|Open| CheckTimer{Timer Expired?}
    CheckTimer -->|No| QueueRequest[Queue Request]
    QueueRequest --> ReturnError3([Return Error: Queued])
    
    CheckTimer -->|Yes| HalfOpen[Set to Half-Open]
    HalfOpen --> Attempt
    
    CheckState -->|Half-Open| Attempt2[Attempt Operation]
    Attempt2 --> CheckResult2{Result}
    
    CheckResult2 -->|Success| CloseCircuit[Close Circuit]
    CloseCircuit --> ReturnSuccess2([Return Success])
    
    CheckResult2 -->|Failure| OpenCircuit2[Open Circuit Again]
    OpenCircuit2 --> StartTimer2[Start Timer 60s]
    StartTimer2 --> ReturnError4([Return Error])
    
    style Start fill:#e1f5ff
    style ReturnSuccess fill:#d4edda
    style ReturnSuccess2 fill:#d4edda
    style ReturnError1 fill:#f8d7da
    style ReturnError2 fill:#f8d7da
    style ReturnError3 fill:#f8d7da
    style ReturnError4 fill:#f8d7da
    style OpenCircuit fill:#fff3cd
    style OpenCircuit2 fill:#fff3cd
```

## Public Verification Portal Flowchart

```mermaid
flowchart TD
    Start([Patient Accesses Portal]) --> InputForm[Enter Report ID & Hash]
    InputForm --> ValidateInput{Input Valid?}
    
    ValidateInput -->|No| ShowError1([Show Validation Error])
    ValidateInput -->|Yes| CheckRateLimit{Rate Limit OK?}
    
    CheckRateLimit -->|No| ShowError2([Show Rate Limit Error])
    CheckRateLimit -->|Yes| LoadReport[Load Report from Database]
    
    LoadReport --> CheckExists{Report Exists?}
    CheckExists -->|No| ShowError3([Show: Report Not Found])
    
    CheckExists -->|Yes| Rehash[Re-hash Report Content]
    Rehash --> CompareInput{Input Hash == Computed Hash?}
    
    CompareInput -->|No| ShowError4([Show: Hash Mismatch])
    CompareInput -->|Yes| QueryBlockchain[Query Blockchain Contract]
    
    QueryBlockchain --> CheckBlockchain{Blockchain Available?}
    CheckBlockchain -->|No| ShowError5([Show: Blockchain Unavailable])
    
    CheckBlockchain -->|Yes| GetRecord[Get Record from Contract]
    GetRecord --> CheckRecord{Record Exists?}
    
    CheckRecord -->|No| ShowNotVerified([Show: Not Verified on Blockchain])
    CheckRecord -->|Yes| CompareBlockchain{Blockchain Hash == Computed Hash?}
    
    CompareBlockchain -->|No| ShowMismatch([Show: Hash Mismatch on Blockchain])
    CompareBlockchain -->|Yes| ShowVerified([Show: Verified Successfully])
    
    ShowVerified --> DisplayDetails[Display: Transaction Hash, Block Number, Timestamp]
    DisplayDetails --> GenerateQR[Generate QR Code]
    GenerateQR --> End([End])
    
    style Start fill:#e1f5ff
    style ShowVerified fill:#d4edda
    style ShowNotVerified fill:#fff3cd
    style ShowError1 fill:#f8d7da
    style ShowError2 fill:#f8d7da
    style ShowError3 fill:#f8d7da
    style ShowError4 fill:#f8d7da
    style ShowError5 fill:#f8d7da
    style ShowMismatch fill:#f8d7da
```


