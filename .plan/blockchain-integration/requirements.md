# Blockchain Integration - Requirements

## Feature Overview

The Blockchain Integration module provides tamper-proof record keeping and payment transparency for the Mediconyx Hospital Management System. This integration enables cryptographic verification of medical reports and immutable payment transaction logging on the blockchain, addressing critical healthcare industry pain points around data security, billing disputes, and record authenticity.

## Functional Requirements

### FR-001: Payment Transaction Recording
- **Description**: Record payment transactions on blockchain for transparency and auditability
- **Acceptance Criteria**:
  - When a payment is recorded in the system, a transaction is created on the blockchain
  - Payment transaction includes: invoice ID, patient address (or hash), amount, timestamp, invoice hash
  - Transaction hash is stored in the database for verification
  - Failed blockchain transactions are retried with exponential backoff
  - Payment can be verified by querying blockchain using transaction hash
  - System continues to function even if blockchain is temporarily unavailable (async processing)

### FR-002: Medical Report Hash Storage
- **Description**: Store cryptographic hashes of medical reports on blockchain
- **Acceptance Criteria**:
  - When a medical report is generated, its content is hashed (SHA-256)
  - Report hash is stored on blockchain with metadata (report ID, patient ID, timestamp)
  - Original report remains in database; only hash is on blockchain
  - Report can be verified by re-hashing and comparing with blockchain hash
  - Verification returns true/false and blockchain transaction details

### FR-003: Blockchain Transaction Verification
- **Description**: Verify blockchain transactions and report hashes
- **Acceptance Criteria**:
  - API endpoint to verify payment transaction by hash
  - API endpoint to verify medical report by report ID
  - Verification queries blockchain and compares stored data
  - Returns verification status, transaction details, and timestamp
  - Public verification portal (no authentication required) for report verification

### FR-004: Smart Contract Deployment
- **Description**: Deploy and manage smart contracts for payments and medical records
- **Acceptance Criteria**:
  - Smart contracts deployed to development network (Ganache)
  - Smart contracts deployed to testnet (Sepolia/Goerli) for staging
  - Contract addresses stored in configuration
  - Contract ABI stored and accessible for interaction
  - Contract upgrade mechanism (for future enhancements)

### FR-005: Blockchain Service Integration
- **Description**: .NET service layer for blockchain interactions
- **Acceptance Criteria**:
  - `IBlockchainService` interface with async methods
  - `BlockchainService` implementation using Nethereum
  - Connection to Ethereum node (Ganache/testnet/mainnet)
  - Transaction signing with hospital wallet
  - Error handling and retry logic
  - Transaction status polling
  - Caching of recent transactions in Redis

### FR-006: Batch Transaction Processing
- **Description**: Batch multiple transactions to reduce gas costs
- **Acceptance Criteria**:
  - Queue system for pending blockchain transactions
  - Batch transactions hourly/daily based on configuration
  - Process batches as single blockchain transaction
  - Handle batch failures with individual retry
  - Log batch processing statistics

### FR-007: Public Verification Portal
- **Description**: Public-facing portal for verifying medical reports
- **Acceptance Criteria**:
  - Public endpoint (no authentication) for report verification
  - User enters report ID and document hash
  - System queries blockchain and returns verification result
  - Display verification status, timestamp, and blockchain transaction hash
  - QR code generation for easy verification

### FR-008: Blockchain Health Monitoring
- **Description**: Monitor blockchain connection and transaction status
- **Acceptance Criteria**:
  - Health check endpoint for blockchain connectivity
  - Monitor pending transactions
  - Alert on failed transactions
  - Track gas prices and transaction costs
  - Dashboard showing blockchain statistics

## Non-Functional Requirements

### NFR-001: Performance
- **Requirement**: Blockchain operations must not block main application flow
- **Details**:
  - All blockchain operations must be asynchronous
  - Payment recording: < 5 seconds (including blockchain transaction)
  - Report hash storage: < 3 seconds
  - Verification: < 2 seconds
  - Use background job processing for non-critical operations
  - Implement circuit breaker pattern for blockchain failures

### NFR-002: Cost Optimization
- **Requirement**: Minimize blockchain transaction costs
- **Details**:
  - Batch transactions when possible
  - Use gas price optimization
  - Implement transaction queuing for off-peak processing
  - Monitor and alert on high gas costs
  - Consider Layer 2 solutions (Polygon, Arbitrum) for production

### NFR-003: Security
- **Requirement**: Secure blockchain wallet and key management
- **Details**:
  - Private keys stored in Azure Key Vault or AWS Secrets Manager (production)
  - Environment variables for development (never commit)
  - Wallet address stored in database (not private key)
  - Implement transaction signing with secure key storage
  - Audit all blockchain operations
  - Rate limiting on verification endpoints

### NFR-004: Reliability
- **Requirement**: System must function even if blockchain is unavailable
- **Details**:
  - Queue transactions when blockchain is down
  - Retry failed transactions with exponential backoff
  - Circuit breaker to prevent cascading failures
  - Fallback to database-only mode (with warning)
  - Health checks and monitoring

### NFR-005: Compliance
- **Requirement**: Blockchain integration must comply with healthcare regulations
- **Details**:
  - No PHI (Protected Health Information) stored on blockchain
  - Only hashes and metadata stored on-chain
  - Original data remains in encrypted database
  - Audit trail for all blockchain operations
  - Data retention policies

## User Stories

### US-001: As a Hospital Admin, I want payment transactions recorded on blockchain
**Priority**: High  
**Acceptance Criteria**:
- When I record a payment, it's automatically sent to blockchain
- I can see the blockchain transaction hash in the payment record
- I can verify the payment on blockchain using the transaction hash
- Payment recording doesn't slow down the billing process

### US-002: As a Doctor, I want medical reports to be tamper-proof
**Priority**: High  
**Acceptance Criteria**:
- When I generate a medical report, its hash is stored on blockchain
- Report cannot be modified without detection
- I can verify report authenticity using the report ID
- Verification is instant (< 2 seconds)

### US-003: As a Patient, I want to verify my medical report authenticity
**Priority**: Medium  
**Acceptance Criteria**:
- I can access a public verification portal
- I enter my report ID and document hash
- System verifies the report against blockchain
- I see verification status and timestamp
- No login required for verification

### US-004: As a System Admin, I want to monitor blockchain operations
**Priority**: Medium  
**Acceptance Criteria**:
- I can see blockchain connection status
- I can view pending transactions
- I receive alerts on failed transactions
- I can see gas costs and transaction statistics
- I can retry failed transactions manually

### US-005: As a Developer, I want blockchain integration to be testable
**Priority**: High  
**Acceptance Criteria**:
- Local blockchain (Ganache) for development
- Testnet for staging environment
- Mock blockchain service for unit tests
- Integration tests with test blockchain
- Clear documentation for setup

## Tech Stack Decisions

### Blockchain Platform
- **Ethereum** (Primary choice for MVP)
  - **Development**: Ganache (local blockchain)
  - **Staging**: Sepolia Testnet
  - **Production**: Ethereum Mainnet or Layer 2 (Polygon, Arbitrum)
  - **Rationale**: Simpler setup, large community, good .NET support via Nethereum

- **Hyperledger Fabric** (Future consideration)
  - **When**: Enterprise customers requiring private blockchain
  - **Rationale**: Private network, faster transactions, better for enterprise
  - **Migration Path**: Documented for Phase 2

### Backend (.NET)
- **Nethereum 4.20.0** - .NET library for Ethereum integration
- **Nethereum.Web3 4.20.0** - Web3 provider for Ethereum
- **Nethereum.Contracts 4.20.0** - Smart contract interaction
- **Nethereum.Signer 4.20.0** - Transaction signing
- **Polly 8.4.0** - Retry and circuit breaker patterns
- **Hangfire 1.8.17** (optional) - Background job processing

### Frontend (Next.js)
- **ethers 6.13.0** - Ethereum JavaScript library
- **wagmi 2.10.0** - React hooks for Ethereum
- **@tanstack/react-query 5.56.0** - Server state management
- **viem 2.10.0** - TypeScript Ethereum library (alternative to ethers)

### Smart Contracts
- **Solidity 0.8.28** - Smart contract language
- **Hardhat 2.22.0** - Development environment
- **@openzeppelin/contracts 5.0.0** - Secure contract libraries
- **@nomicfoundation/hardhat-verify** - Contract verification

### Infrastructure
- **Ganache 7.9.2** - Local Ethereum blockchain (Docker)
- **Infura / Alchemy** - Ethereum node providers (production)
- **Redis** - Transaction queue and caching

## Integration Points

### Database
- **Payment Table**: Add `BlockchainTransactionHash`, `BlockchainVerified`, `BlockchainVerificationDate` fields
- **MedicalReport Table**: Add `DocumentHash`, `BlockchainTransactionHash`, `BlockchainVerified` fields
- **BlockchainTransaction Table**: New table for tracking blockchain operations

### External Services
- **Ethereum Node**: Ganache (dev), Infura/Alchemy (prod)
- **Wallet Service**: Hospital wallet for signing transactions
- **Redis**: Transaction queue and caching

### API Endpoints
- `POST /api/blockchain/verify-payment` - Verify payment transaction
- `POST /api/blockchain/verify-report` - Verify medical report
- `GET /api/blockchain/health` - Blockchain health check
- `GET /api/public/verify-report/{reportId}` - Public report verification

## Constraints & Assumptions

### Constraints
- No PHI stored on blockchain (only hashes)
- Gas costs must be minimized (batch transactions)
- Must work with existing Payment and MedicalReport models
- Blockchain operations must not block main application
- Private keys must be stored securely (never in code)

### Assumptions
- Hospital has Ethereum wallet with sufficient funds for gas
- Ethereum network is accessible (with retry logic for failures)
- Gas prices are acceptable for business model
- Users understand blockchain transaction delays
- Smart contracts are audited before production deployment

## Success Criteria

1. ✅ Payment transactions recorded on blockchain within 5 seconds
2. ✅ Medical report hashes stored on blockchain within 3 seconds
3. ✅ Verification endpoints respond within 2 seconds
4. ✅ System functions normally when blockchain is temporarily unavailable
5. ✅ Zero PHI stored on blockchain
6. ✅ Smart contracts deployed and verified on testnet
7. ✅ Public verification portal functional
8. ✅ All blockchain operations logged and auditable
9. ✅ Gas costs optimized (batched transactions)
10. ✅ Comprehensive test coverage (80%+)

## Phase Approach

### Phase 1: MVP (Months 1-2)
- Local blockchain (Ganache) integration
- Basic payment transaction recording
- Basic medical report hash storage
- Simple verification endpoints
- Database schema updates

### Phase 2: Production Ready (Months 3-4)
- Testnet deployment
- Batch transaction processing
- Public verification portal
- Monitoring and alerting
- Gas cost optimization

### Phase 3: Scale (Months 5+)
- Mainnet or Layer 2 deployment
- Advanced batching strategies
- Multi-wallet support
- Hyperledger Fabric evaluation
- Performance optimization


