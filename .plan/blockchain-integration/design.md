# Blockchain Integration - Design Document

## Architecture Overview

The blockchain integration follows a **hybrid architecture** where critical data remains in PostgreSQL, while cryptographic hashes and transaction metadata are stored on the Ethereum blockchain. This approach ensures data privacy (no PHI on blockchain) while providing tamper-proof verification.

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Next.js UI (Frontend)                    │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Payment Recording UI                                  │ │
│  │  Report Verification Portal (Public)                 │ │
│  │  Blockchain Status Dashboard                          │ │
│  └──────────────────────────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │ REST API
┌───────────────────────────▼─────────────────────────────────┐
│              .NET 10 Web API (Backend)                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  PaymentController                                   │  │
│  │  MedicalReportController                             │  │
│  │  BlockchainController                                 │  │
│  └───────────────────────┬──────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────▼──────────────────────────────┐  │
│  │         BlockchainService (Nethereum)                │  │
│  │  - RecordPaymentTransaction()                        │  │
│  │  - StoreReportHash()                                 │  │
│  │  - VerifyPayment()                                   │  │
│  │  - VerifyReport()                                    │  │
│  └───────────────────────┬──────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────▼──────────────────────────────┐  │
│  │         Transaction Queue (Redis/Hangfire)          │  │
│  │  - Queue pending transactions                       │  │
│  │  - Batch processing                                │  │
│  │  - Retry logic                                      │  │
│  └───────────────────────┬──────────────────────────────┘  │
└──────────────────────────┼──────────────────────────────────┘
                           │ JSON-RPC / WebSocket
┌──────────────────────────▼──────────────────────────────────┐
│              Ethereum Blockchain Network                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  PaymentContract.sol                                │   │
│  │  - recordPayment()                                   │   │
│  │  - verifyPayment()                                   │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  MedicalRecordContract.sol                            │   │
│  │  - storeReportHash()                                  │   │
│  │  - verifyReport()                                     │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│              PostgreSQL Database                             │
│  - Payments (with blockchain hash)                          │
│  - MedicalReports (with document hash)                      │
│  - BlockchainTransactions (audit log)                       │
└──────────────────────────────────────────────────────────────┘
```

## Smart Contract Design

### PaymentContract.sol

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.28;

contract PaymentContract {
    struct Payment {
        uint256 invoiceId;
        address patient;  // Patient wallet address (or zero address if not available)
        uint256 amount;   // Amount in wei (or smallest currency unit)
        uint256 timestamp;
        string status;    // "completed", "pending", "refunded"
        bytes32 invoiceHash;  // SHA-256 hash of invoice document
    }
    
    mapping(uint256 => Payment) public payments;
    uint256 public paymentCount;
    
    event PaymentRecorded(
        uint256 indexed paymentId,
        uint256 indexed invoiceId,
        address indexed patient,
        uint256 amount,
        bytes32 invoiceHash
    );
    
    function recordPayment(
        uint256 invoiceId,
        address patient,
        uint256 amount,
        bytes32 invoiceHash
    ) public returns (uint256) {
        require(invoiceId > 0, "Invalid invoice ID");
        require(amount > 0, "Amount must be greater than zero");
        
        paymentCount++;
        uint256 paymentId = paymentCount;
        
        payments[paymentId] = Payment({
            invoiceId: invoiceId,
            patient: patient,
            amount: amount,
            timestamp: block.timestamp,
            status: "completed",
            invoiceHash: invoiceHash
        });
        
        emit PaymentRecorded(paymentId, invoiceId, patient, amount, invoiceHash);
        
        return paymentId;
    }
    
    function verifyPayment(uint256 paymentId) 
        public 
        view 
        returns (Payment memory) 
    {
        require(paymentId > 0 && paymentId <= paymentCount, "Invalid payment ID");
        return payments[paymentId];
    }
    
    function getPaymentCount() public view returns (uint256) {
        return paymentCount;
    }
}
```

### MedicalRecordContract.sol

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.28;

contract MedicalRecordContract {
    struct Record {
        uint256 recordId;
        bytes32 documentHash;  // SHA-256 hash of report content
        uint256 timestamp;
        address hospital;       // Hospital wallet address
        address patient;        // Patient wallet address (or zero)
    }
    
    mapping(uint256 => Record) public records;
    uint256 public recordCount;
    
    event RecordStored(
        uint256 indexed recordId,
        bytes32 indexed documentHash,
        address indexed hospital,
        address patient
    );
    
    function storeReportHash(
        uint256 recordId,
        bytes32 documentHash,
        address patient
    ) public returns (bool) {
        require(recordId > 0, "Invalid record ID");
        require(documentHash != bytes32(0), "Invalid document hash");
        
        // Check if record already exists
        require(records[recordId].recordId == 0, "Record already exists");
        
        records[recordId] = Record({
            recordId: recordId,
            documentHash: documentHash,
            timestamp: block.timestamp,
            hospital: msg.sender,
            patient: patient
        });
        
        recordCount++;
        
        emit RecordStored(recordId, documentHash, msg.sender, patient);
        
        return true;
    }
    
    function verifyRecord(uint256 recordId, bytes32 documentHash) 
        public 
        view 
        returns (bool) 
    {
        require(recordId > 0, "Invalid record ID");
        Record memory record = records[recordId];
        
        if (record.recordId == 0) {
            return false;  // Record doesn't exist
        }
        
        return record.documentHash == documentHash;
    }
    
    function getRecord(uint256 recordId) 
        public 
        view 
        returns (Record memory) 
    {
        require(recordId > 0, "Invalid record ID");
        return records[recordId];
    }
    
    function getRecordCount() public view returns (uint256) {
        return recordCount;
    }
}
```

## Data Models

### BlockchainTransaction Entity

```csharp
// mediconyx-api/Models/BlockchainTransaction.cs
namespace mediconyx_api.Models;

public class BlockchainTransaction
{
    public int Id { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public BlockchainTransactionType TransactionType { get; set; }
    public int? PaymentId { get; set; }
    public int? MedicalReportId { get; set; }
    public string ContractAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Confirmed, Failed
    public long? BlockNumber { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal GasUsed { get; set; }
    public decimal GasPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Payment? Payment { get; set; }
    public MedicalReport? MedicalReport { get; set; }
}

public enum BlockchainTransactionType
{
    Payment = 1,
    MedicalReport = 2
}
```

### Updated Payment Model

```csharp
// Add to existing Payment model
public class Payment
{
    // ... existing fields ...
    
    // Blockchain fields
    public string? BlockchainTransactionHash { get; set; }
    public bool BlockchainVerified { get; set; }
    public DateTime? BlockchainVerificationDate { get; set; }
    public string? InvoiceHash { get; set; } // SHA-256 hash of invoice
    
    // Navigation
    public BlockchainTransaction? BlockchainTransaction { get; set; }
}
```

### Updated MedicalReport Model

```csharp
// Add to existing MedicalReport model
public class MedicalReport
{
    // ... existing fields ...
    
    // Blockchain fields
    public string? DocumentHash { get; set; } // SHA-256 hash of report content
    public string? BlockchainTransactionHash { get; set; }
    public bool BlockchainVerified { get; set; }
    public DateTime? BlockchainVerificationDate { get; set; }
    
    // Navigation
    public BlockchainTransaction? BlockchainTransaction { get; set; }
}
```

## API Contracts

### POST /api/blockchain/verify-payment
**Description**: Verify a payment transaction on blockchain  
**Authentication**: Required (JWT)  
**Authorization**: Any authenticated user  
**Request Body**:
```json
{
  "paymentId": 123,
  "transactionHash": "0x1234..."
}
```
**Response**: `200 OK`
```json
{
  "verified": true,
  "transactionHash": "0x1234...",
  "blockNumber": 12345678,
  "confirmedAt": "2024-01-20T10:30:00Z",
  "payment": {
    "invoiceId": 123,
    "amount": "100.50",
    "timestamp": 1705747800
  }
}
```

### POST /api/blockchain/verify-report
**Description**: Verify a medical report hash on blockchain  
**Authentication**: Required (JWT)  
**Authorization**: Any authenticated user  
**Request Body**:
```json
{
  "reportId": 456,
  "documentHash": "0xabcd..."
}
```
**Response**: `200 OK`
```json
{
  "verified": true,
  "transactionHash": "0x5678...",
  "blockNumber": 12345679,
  "confirmedAt": "2024-01-20T10:35:00Z",
  "record": {
    "recordId": 456,
    "documentHash": "0xabcd...",
    "timestamp": 1705748100
  }
}
```

### GET /api/public/verify-report/{reportId}?hash={documentHash}
**Description**: Public endpoint to verify medical report (no authentication)  
**Authentication**: None  
**Query Parameters**:
- `reportId` (path): Medical report ID
- `hash` (query): SHA-256 hash of report document

**Response**: `200 OK`
```json
{
  "verified": true,
  "reportId": 456,
  "documentHash": "0xabcd...",
  "timestamp": 1705748100,
  "blockchainTransactionHash": "0x5678...",
  "blockNumber": 12345679,
  "message": "Report verified successfully"
}
```

### GET /api/blockchain/health
**Description**: Check blockchain connection health  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Response**: `200 OK`
```json
{
  "status": "healthy",
  "network": "ganache",
  "chainId": 1337,
  "blockNumber": 123456,
  "gasPrice": "20000000000",
  "pendingTransactions": 3,
  "lastSyncAt": "2024-01-20T10:40:00Z"
}
```

## Backend Implementation

### IBlockchainService Interface

```csharp
// mediconyx-api/Services/IBlockchainService.cs
namespace mediconyx_api.Services;

public interface IBlockchainService
{
    Task<string> RecordPaymentTransactionAsync(
        int paymentId, 
        decimal amount, 
        string invoiceHash, 
        CancellationToken cancellationToken = default);
    
    Task<bool> VerifyPaymentTransactionAsync(
        string transactionHash, 
        CancellationToken cancellationToken = default);
    
    Task<string> StoreReportHashAsync(
        int reportId, 
        string documentHash, 
        CancellationToken cancellationToken = default);
    
    Task<bool> VerifyReportHashAsync(
        int reportId, 
        string documentHash, 
        CancellationToken cancellationToken = default);
    
    Task<BlockchainHealthStatus> GetHealthStatusAsync(
        CancellationToken cancellationToken = default);
}
```

### BlockchainService Implementation (Nethereum)

```csharp
// mediconyx-api/Services/BlockchainService.cs
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Signer;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace mediconyx_api.Services;

public class BlockchainService : IBlockchainService
{
    private readonly IWeb3 _web3;
    private readonly string _walletPrivateKey;
    private readonly string _paymentContractAddress;
    private readonly string _medicalRecordContractAddress;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BlockchainService> _logger;
    
    public BlockchainService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<BlockchainService> logger)
    {
        var nodeUrl = configuration["BlockchainSettings:NodeUrl"] 
            ?? throw new InvalidOperationException("Blockchain node URL not configured");
        
        _walletPrivateKey = configuration["BlockchainSettings:WalletPrivateKey"] 
            ?? throw new InvalidOperationException("Wallet private key not configured");
        
        _paymentContractAddress = configuration["BlockchainSettings:PaymentContractAddress"] 
            ?? throw new InvalidOperationException("Payment contract address not configured");
        
        _medicalRecordContractAddress = configuration["BlockchainSettings:MedicalRecordContractAddress"] 
            ?? throw new InvalidOperationException("Medical record contract address not configured");
        
        _web3 = new Web3(nodeUrl);
        _context = context;
        _logger = logger;
    }
    
    public async Task<string> RecordPaymentTransactionAsync(
        int paymentId, 
        decimal amount, 
        string invoiceHash, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
            
            if (payment == null)
            {
                throw new ArgumentException($"Payment {paymentId} not found");
            }
            
            // Convert amount to wei (assuming amount is in BDT, convert to smallest unit)
            // For MVP, we'll use a simple conversion or store as-is
            var amountInWei = new HexBigInteger((long)(amount * 1000000000000000000)); // 18 decimals
            
            // Convert invoice hash to bytes32
            var invoiceHashBytes = ConvertHexStringToBytes32(invoiceHash);
            
            // Get contract instance
            var contract = _web3.Eth.GetContract(
                GetPaymentContractABI(), 
                _paymentContractAddress);
            
            var recordPaymentFunction = contract.GetFunction("recordPayment");
            
            // Create transaction
            var account = new Account(_walletPrivateKey);
            var transactionInput = recordPaymentFunction.CreateTransactionInput(
                account.Address,
                payment.Id,  // invoiceId
                account.Address,  // patient (or zero address)
                amountInWei,
                invoiceHashBytes
            );
            
            // Estimate gas
            var gasEstimate = await recordPaymentFunction.EstimateGasAsync(transactionInput);
            transactionInput.Gas = gasEstimate;
            
            // Send transaction
            var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(transactionInput);
            
            // Store transaction in database
            var blockchainTx = new BlockchainTransaction
            {
                TransactionHash = transactionHash,
                TransactionType = BlockchainTransactionType.Payment,
                PaymentId = paymentId,
                ContractAddress = _paymentContractAddress,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            
            _context.BlockchainTransactions.Add(blockchainTx);
            payment.BlockchainTransactionHash = transactionHash;
            await _context.SaveChangesAsync(cancellationToken);
            
            // Start background job to check transaction status
            _ = Task.Run(async () => await PollTransactionStatusAsync(transactionHash, cancellationToken));
            
            _logger.LogInformation(
                "Payment transaction recorded on blockchain: PaymentId={PaymentId}, TxHash={TxHash}",
                paymentId, transactionHash);
            
            return transactionHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error recording payment transaction: PaymentId={PaymentId}", 
                paymentId);
            throw;
        }
    }
    
    public async Task<bool> VerifyPaymentTransactionAsync(
        string transactionHash, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            if (receipt == null || receipt.Status.Value == 0)
            {
                return false;
            }
            
            // Get transaction details from contract
            var contract = _web3.Eth.GetContract(
                GetPaymentContractABI(), 
                _paymentContractAddress);
            
            var verifyFunction = contract.GetFunction("verifyPayment");
            // Extract payment ID from transaction logs and verify
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment transaction: TxHash={TxHash}", transactionHash);
            return false;
        }
    }
    
    public async Task<string> StoreReportHashAsync(
        int reportId, 
        string documentHash, 
        CancellationToken cancellationToken = default)
    {
        // Similar implementation to RecordPaymentTransactionAsync
        // but using MedicalRecordContract
        // ...
    }
    
    public async Task<bool> VerifyReportHashAsync(
        int reportId, 
        string documentHash, 
        CancellationToken cancellationToken = default)
    {
        // Implementation to verify report hash
        // ...
    }
    
    public async Task<BlockchainHealthStatus> GetHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
            
            return new BlockchainHealthStatus
            {
                Status = "healthy",
                Network = "ganache", // or detect from chainId
                ChainId = await GetChainIdAsync(),
                BlockNumber = (long)blockNumber.Value,
                GasPrice = gasPrice.Value.ToString(),
                LastSyncAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blockchain health");
            return new BlockchainHealthStatus
            {
                Status = "unhealthy",
                LastSyncAt = DateTime.UtcNow
            };
        }
    }
    
    private async Task PollTransactionStatusAsync(string transactionHash, CancellationToken cancellationToken)
    {
        // Poll transaction status and update database
        // ...
    }
    
    private string GetPaymentContractABI()
    {
        // Return contract ABI JSON
        // ...
    }
    
    private byte[] ConvertHexStringToBytes32(string hexString)
    {
        // Convert hex string to bytes32
        // ...
    }
    
    private async Task<long> GetChainIdAsync()
    {
        var chainId = await _web3.Eth.ChainId.SendRequestAsync();
        return (long)chainId.Value;
    }
}
```

## Frontend Implementation

### Blockchain Service (Next.js)

```typescript
// mediconyx-ui/lib/services/blockchain.ts
import { ethers } from 'ethers';

const PAYMENT_CONTRACT_ABI = [
  // Contract ABI here
];

const MEDICAL_RECORD_CONTRACT_ABI = [
  // Contract ABI here
];

export class BlockchainService {
  private provider: ethers.Provider;
  private paymentContract: ethers.Contract;
  private medicalRecordContract: ethers.Contract;
  
  constructor() {
    const rpcUrl = process.env.NEXT_PUBLIC_ETHEREUM_RPC_URL || 'http://localhost:8545';
    this.provider = new ethers.JsonRpcProvider(rpcUrl);
    
    const paymentContractAddress = process.env.NEXT_PUBLIC_PAYMENT_CONTRACT_ADDRESS!;
    const medicalRecordContractAddress = process.env.NEXT_PUBLIC_MEDICAL_RECORD_CONTRACT_ADDRESS!;
    
    this.paymentContract = new ethers.Contract(
      paymentContractAddress,
      PAYMENT_CONTRACT_ABI,
      this.provider
    );
    
    this.medicalRecordContract = new ethers.Contract(
      medicalRecordContractAddress,
      MEDICAL_RECORD_CONTRACT_ABI,
      this.provider
    );
  }
  
  async verifyPayment(transactionHash: string) {
    const receipt = await this.provider.getTransactionReceipt(transactionHash);
    return receipt !== null && receipt.status === 1;
  }
  
  async verifyReport(reportId: number, documentHash: string): Promise<boolean> {
    const result = await this.medicalRecordContract.verifyRecord(reportId, documentHash);
    return result;
  }
  
  async getReportDetails(reportId: number) {
    const record = await this.medicalRecordContract.getRecord(reportId);
    return {
      recordId: record.recordId.toString(),
      documentHash: record.documentHash,
      timestamp: new Date(Number(record.timestamp) * 1000),
      hospital: record.hospital,
      patient: record.patient
    };
  }
}
```

## Workflow Diagrams

### Payment Recording Flow

1. User records payment in system
2. System generates invoice hash (SHA-256 of invoice content)
3. System calls `BlockchainService.RecordPaymentTransactionAsync()`
4. Service creates blockchain transaction
5. Transaction sent to Ethereum network
6. Transaction hash stored in database
7. Background job polls for transaction confirmation
8. On confirmation, update payment record with verification status

### Report Verification Flow

1. User requests report verification
2. System retrieves report from database
3. System re-hashes report content (SHA-256)
4. System queries blockchain contract with report ID
5. Compare stored hash with blockchain hash
6. Return verification result

## Error Handling Strategy

### Transaction Failures
- **Network Errors**: Retry with exponential backoff (3 attempts)
- **Gas Estimation Failures**: Use default gas limit with buffer
- **Transaction Rejection**: Log error, queue for manual review
- **Contract Errors**: Return user-friendly error message

### Circuit Breaker Pattern
- Monitor blockchain connection health
- After 5 consecutive failures, open circuit
- Queue transactions when circuit is open
- Retry after 60 seconds (half-open state)
- Close circuit after successful transaction

## Security Measures

### Private Key Management
- **Development**: Environment variable (never commit)
- **Production**: Azure Key Vault or AWS Secrets Manager
- **Rotation**: Support for key rotation without downtime
- **Access Control**: Only blockchain service can access keys

### Transaction Security
- Validate all inputs before sending to blockchain
- Sanitize user inputs to prevent injection
- Rate limiting on verification endpoints
- Monitor for suspicious transaction patterns

### Data Privacy
- No PHI stored on blockchain
- Only hashes and metadata on-chain
- Original data encrypted in database
- Access logging for all blockchain operations

## Deployment Plan

### Development Environment
```yaml
# docker-compose.yml addition
blockchain-node:
  image: trufflesuite/ganache:latest
  container_name: mediconyx_ganache
  ports:
    - "8545:8545"
  command: >
    ganache
    --deterministic
    --accounts 10
    --defaultBalanceEther 100
    --networkId 1337
    --hostname 0.0.0.0
```

### Staging Environment
- Deploy contracts to Sepolia Testnet
- Use Infura or Alchemy for node access
- Test with testnet ETH
- Monitor gas costs

### Production Environment
- Option A: Ethereum Mainnet (higher costs, public transparency)
- Option B: Layer 2 (Polygon, Arbitrum) - lower costs
- Option C: Hyperledger Fabric (private network) - for enterprise

## Testing Strategy

### Unit Tests
- Test blockchain service methods with mocked Web3
- Test transaction creation logic
- Test hash generation and verification
- Test error handling and retry logic

### Integration Tests
- Test with local Ganache blockchain
- Test contract deployment
- Test transaction recording and verification
- Test batch processing

### E2E Tests
- Test payment recording flow end-to-end
- Test report verification flow
- Test public verification portal
- Use test blockchain network

## Performance Considerations

### Optimization Strategies
- Batch multiple transactions into single blockchain transaction
- Use event logs instead of contract calls for verification
- Cache contract ABIs and addresses
- Implement connection pooling for Web3 provider
- Use async/await for all blockchain operations

### Monitoring
- Track transaction confirmation times
- Monitor gas costs
- Alert on failed transactions
- Dashboard for blockchain statistics


