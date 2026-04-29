# Blockchain Integration - Tasks

## Task Completion Tracking

Use `[ ]` for incomplete tasks and `[x]` for completed tasks. Apply to both top-level and nested sub-tasks.

---

## Phase 1: Foundation & Setup (Week 1-2)

### TASK-001: Set up Blockchain Development Environment
**Priority**: High  
**Effort**: 4h  
**Dependencies**: None

- [ ] Install Ganache CLI or Docker image
- [ ] Configure Ganache for local development
- [ ] Set up Hardhat project for smart contract development
- [ ] Configure Hardhat network for Ganache
- [ ] Test connection to local blockchain
- [ ] Document setup instructions

**Acceptance Criteria**:
- Ganache running on localhost:8545
- Hardhat can connect to Ganache
- Can deploy test contract successfully

---

### TASK-002: Develop Smart Contracts
**Priority**: High  
**Effort**: 8h  
**Dependencies**: TASK-001

- [ ] Create `PaymentContract.sol` with recordPayment and verifyPayment functions
- [ ] Create `MedicalRecordContract.sol` with storeReportHash and verifyRecord functions
- [ ] Add OpenZeppelin contracts for security
- [ ] Write unit tests for PaymentContract
- [ ] Write unit tests for MedicalRecordContract
- [ ] Compile contracts with Hardhat
- [ ] Deploy contracts to Ganache
- [ ] Verify contract deployment and functionality
- [ ] Document contract ABIs and addresses

**Acceptance Criteria**:
- Both contracts compile without errors
- Unit tests pass (100% coverage)
- Contracts deployed to Ganache
- Contract addresses documented

---

### TASK-003: Install and Configure Nethereum in .NET Project
**Priority**: High  
**Effort**: 3h  
**Dependencies**: TASK-002

- [ ] Install Nethereum NuGet packages:
  - [ ] Nethereum.Web3 (4.20.0)
  - [ ] Nethereum.Contracts (4.20.0)
  - [ ] Nethereum.Signer (4.20.0)
- [ ] Add blockchain configuration to `appsettings.json`
- [ ] Create `BlockchainSettings` configuration class
- [ ] Add wallet private key to user secrets (development)
- [ ] Test connection to Ganache from .NET
- [ ] Create `IBlockchainService` interface

**Acceptance Criteria**:
- Nethereum packages installed
- Can connect to Ganache from .NET
- Configuration loaded correctly
- Interface defined

---

### TASK-004: Create Database Schema for Blockchain Integration
**Priority**: High  
**Effort**: 3h  
**Dependencies**: None

- [ ] Create `BlockchainTransaction` entity model
- [ ] Add blockchain fields to `Payment` model:
  - [ ] `BlockchainTransactionHash`
  - [ ] `BlockchainVerified`
  - [ ] `BlockchainVerificationDate`
  - [ ] `InvoiceHash`
- [ ] Add blockchain fields to `MedicalReport` model:
  - [ ] `DocumentHash`
  - [ ] `BlockchainTransactionHash`
  - [ ] `BlockchainVerified`
  - [ ] `BlockchainVerificationDate`
- [ ] Create EF Core migration
- [ ] Update `ApplicationDbContext` with new entities
- [ ] Apply migration to database

**Acceptance Criteria**:
- Migration created successfully
- Database schema updated
- All foreign keys and indexes configured
- Migration applied without errors

---

## Phase 2: Core Blockchain Service Implementation (Week 3-4)

### TASK-005: Implement BlockchainService - Payment Transactions
**Priority**: High  
**Effort**: 8h  
**Dependencies**: TASK-003, TASK-004

- [ ] Implement `RecordPaymentTransactionAsync()` method
  - [ ] Load payment from database
  - [ ] Generate invoice hash (SHA-256)
  - [ ] Create contract function call
  - [ ] Estimate gas
  - [ ] Sign transaction with wallet
  - [ ] Send transaction to blockchain
  - [ ] Store transaction hash in database
  - [ ] Start background polling for confirmation
- [ ] Implement `VerifyPaymentTransactionAsync()` method
  - [ ] Get transaction receipt
  - [ ] Query contract for payment details
  - [ ] Compare stored data with blockchain data
  - [ ] Return verification result
- [ ] Add error handling and logging
- [ ] Write unit tests with mocked Web3

**Acceptance Criteria**:
- Payment transactions recorded on blockchain
- Transaction hash stored in database
- Verification returns correct result
- Unit tests pass (80%+ coverage)

---

### TASK-006: Implement BlockchainService - Medical Report Hashing
**Priority**: High  
**Effort**: 8h  
**Dependencies**: TASK-003, TASK-004

- [ ] Implement `StoreReportHashAsync()` method
  - [ ] Load report from database
  - [ ] Generate document hash (SHA-256 of report content)
  - [ ] Create contract function call
  - [ ] Estimate gas
  - [ ] Sign and send transaction
  - [ ] Store transaction hash in database
  - [ ] Start background polling
- [ ] Implement `VerifyReportHashAsync()` method
  - [ ] Re-hash report content
  - [ ] Query contract with report ID
  - [ ] Compare hashes
  - [ ] Return verification result
- [ ] Add error handling and logging
- [ ] Write unit tests with mocked Web3

**Acceptance Criteria**:
- Report hashes stored on blockchain
- Transaction hash stored in database
- Verification returns correct result
- Unit tests pass (80%+ coverage)

---

### TASK-007: Implement Transaction Status Polling
**Priority**: Medium  
**Effort**: 4h  
**Dependencies**: TASK-005, TASK-006

- [ ] Create background service for polling transaction status
- [ ] Implement polling logic:
  - [ ] Query transaction receipt
  - [ ] Update database on confirmation
  - [ ] Handle failed transactions
  - [ ] Retry logic for pending transactions
- [ ] Configure polling interval (30 seconds)
- [ ] Add circuit breaker for blockchain failures
- [ ] Log polling statistics

**Acceptance Criteria**:
- Transaction status updated automatically
- Failed transactions logged and flagged
- Circuit breaker prevents cascading failures
- Polling doesn't impact main application performance

---

### TASK-008: Implement Blockchain Health Check
**Priority**: Medium  
**Effort**: 2h  
**Dependencies**: TASK-003

- [ ] Implement `GetHealthStatusAsync()` method
  - [ ] Check blockchain connection
  - [ ] Get current block number
  - [ ] Get gas price
  - [ ] Get chain ID
  - [ ] Count pending transactions
- [ ] Create health check endpoint
- [ ] Add health check to application startup
- [ ] Write unit tests

**Acceptance Criteria**:
- Health check returns connection status
- Endpoint responds within 2 seconds
- Health status includes all required metrics

---

## Phase 3: API Endpoints (Week 5)

### TASK-009: Create BlockchainController
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-005, TASK-006, TASK-008

- [ ] Create `BlockchainController` class
- [ ] Implement `POST /api/blockchain/verify-payment` endpoint
  - [ ] Validate request
  - [ ] Call verification service
  - [ ] Return verification result
- [ ] Implement `POST /api/blockchain/verify-report` endpoint
  - [ ] Validate request
  - [ ] Call verification service
  - [ ] Return verification result
- [ ] Implement `GET /api/blockchain/health` endpoint
  - [ ] Require Admin role
  - [ ] Return health status
- [ ] Add Swagger documentation
- [ ] Write integration tests

**Acceptance Criteria**:
- All endpoints functional
- Proper authentication and authorization
- Swagger documentation complete
- Integration tests pass

---

### TASK-010: Create Public Verification Portal Endpoint
**Priority**: Medium  
**Effort**: 3h  
**Dependencies**: TASK-006

- [ ] Create `PublicController` class
- [ ] Implement `GET /api/public/verify-report/{reportId}` endpoint
  - [ ] No authentication required
  - [ ] Accept document hash as query parameter
  - [ ] Verify report against blockchain
  - [ ] Return verification result
- [ ] Add rate limiting (prevent abuse)
- [ ] Add CORS configuration for public access
- [ ] Write integration tests

**Acceptance Criteria**:
- Public endpoint accessible without authentication
- Verification works correctly
- Rate limiting prevents abuse
- CORS configured properly

---

### TASK-011: Integrate Blockchain into Payment Flow
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-005

- [ ] Update `PaymentController` or payment service
- [ ] After payment creation, call `BlockchainService.RecordPaymentTransactionAsync()`
- [ ] Handle blockchain errors gracefully (don't fail payment creation)
- [ ] Log blockchain operations
- [ ] Update payment response to include transaction hash (if available)
- [ ] Write integration tests

**Acceptance Criteria**:
- Payments automatically recorded on blockchain
- Payment creation doesn't fail if blockchain is unavailable
- Transaction hash included in payment response
- Integration tests pass

---

### TASK-012: Integrate Blockchain into Medical Report Flow
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-006

- [ ] Update `MedicalReportController` or report service
- [ ] After report generation, call `BlockchainService.StoreReportHashAsync()`
- [ ] Generate document hash before storing
- [ ] Handle blockchain errors gracefully
- [ ] Log blockchain operations
- [ ] Update report response to include transaction hash (if available)
- [ ] Write integration tests

**Acceptance Criteria**:
- Report hashes automatically stored on blockchain
- Report generation doesn't fail if blockchain is unavailable
- Transaction hash included in report response
- Integration tests pass

---

## Phase 4: Frontend Integration (Week 6)

### TASK-013: Install and Configure Ethereum Libraries in Next.js
**Priority**: High  
**Effort**: 2h  
**Dependencies**: None

- [ ] Install ethers.js (6.13.0) or viem (2.10.0)
- [ ] Create blockchain service utility
- [ ] Configure RPC URL from environment variables
- [ ] Test connection to blockchain
- [ ] Document setup

**Acceptance Criteria**:
- Libraries installed
- Can connect to blockchain from frontend
- Service utility created

---

### TASK-014: Create Public Report Verification UI
**Priority**: Medium  
**Effort**: 6h  
**Dependencies**: TASK-010, TASK-013

- [ ] Create verification page (`/verify-report`)
- [ ] Add form for report ID and document hash input
- [ ] Implement verification API call
- [ ] Display verification result:
  - [ ] Verification status (verified/not verified)
  - [ ] Transaction hash
  - [ ] Timestamp
  - [ ] Block number
- [ ] Add QR code generation for easy sharing
- [ ] Style with Tailwind CSS
- [ ] Add error handling
- [ ] Write component tests

**Acceptance Criteria**:
- Verification page functional
- Users can verify reports without login
- Results displayed clearly
- QR code generated correctly
- Tests pass

---

### TASK-015: Add Blockchain Status to Payment Details
**Priority**: Low  
**Effort**: 3h  
**Dependencies**: TASK-011, TASK-013

- [ ] Update payment details page
- [ ] Display blockchain transaction hash
- [ ] Show verification status
- [ ] Add "Verify on Blockchain" button
- [ ] Display verification result
- [ ] Style with Tailwind CSS

**Acceptance Criteria**:
- Blockchain info displayed on payment page
- Verification button works
- Status updates correctly

---

### TASK-016: Add Blockchain Status to Report Details
**Priority**: Low  
**Effort**: 3h  
**Dependencies**: TASK-012, TASK-013

- [ ] Update report details page
- [ ] Display blockchain transaction hash
- [ ] Show verification status
- [ ] Add "Verify on Blockchain" button
- [ ] Display verification result
- [ ] Style with Tailwind CSS

**Acceptance Criteria**:
- Blockchain info displayed on report page
- Verification button works
- Status updates correctly

---

### TASK-017: Create Blockchain Dashboard (Admin)
**Priority**: Low  
**Effort**: 6h  
**Dependencies**: TASK-008, TASK-013

- [ ] Create admin dashboard page (`/admin/blockchain`)
- [ ] Display blockchain health status
- [ ] Show pending transactions
- [ ] Display transaction statistics:
  - [ ] Total transactions
  - [ ] Success rate
  - [ ] Average gas cost
  - [ ] Pending count
- [ ] Add manual retry button for failed transactions
- [ ] Style with Tailwind CSS
- [ ] Add real-time updates (polling or WebSocket)

**Acceptance Criteria**:
- Dashboard displays all metrics
- Admin can monitor blockchain status
- Manual retry works
- Real-time updates functional

---

## Phase 5: Testing & Optimization (Week 7)

### TASK-018: Write Comprehensive Unit Tests
**Priority**: High  
**Effort**: 8h  
**Dependencies**: TASK-005, TASK-006

- [ ] Write unit tests for `BlockchainService`:
  - [ ] Test payment transaction recording
  - [ ] Test payment verification
  - [ ] Test report hash storage
  - [ ] Test report verification
  - [ ] Test error handling
  - [ ] Test retry logic
- [ ] Mock Web3 provider for tests
- [ ] Achieve 80%+ code coverage
- [ ] Run tests in CI/CD pipeline

**Acceptance Criteria**:
- All unit tests pass
- Code coverage >= 80%
- Tests run in CI/CD

---

### TASK-019: Write Integration Tests
**Priority**: High  
**Effort**: 6h  
**Dependencies**: TASK-009, TASK-010, TASK-011, TASK-012

- [ ] Write integration tests for blockchain endpoints:
  - [ ] Test payment verification endpoint
  - [ ] Test report verification endpoint
  - [ ] Test public verification endpoint
  - [ ] Test health check endpoint
- [ ] Use test blockchain (Ganache) for tests
- [ ] Test end-to-end payment flow with blockchain
- [ ] Test end-to-end report flow with blockchain
- [ ] Run tests in CI/CD pipeline

**Acceptance Criteria**:
- All integration tests pass
- End-to-end flows tested
- Tests run in CI/CD

---

### TASK-020: Implement Batch Transaction Processing
**Priority**: Medium  
**Effort**: 6h  
**Dependencies**: TASK-005, TASK-006

- [ ] Create transaction queue (Redis or in-memory)
- [ ] Implement batch processor:
  - [ ] Collect pending transactions
  - [ ] Batch into single transaction
  - [ ] Send batch to blockchain
  - [ ] Update individual records
- [ ] Configure batch interval (hourly/daily)
- [ ] Add batch processing statistics
- [ ] Write tests

**Acceptance Criteria**:
- Transactions batched correctly
- Gas costs reduced
- Individual records updated
- Tests pass

---

### TASK-021: Optimize Gas Costs
**Priority**: Medium  
**Effort**: 4h  
**Dependencies**: TASK-005, TASK-006

- [ ] Analyze gas usage
- [ ] Optimize contract functions
- [ ] Implement gas price estimation
- [ ] Add gas price monitoring
- [ ] Configure gas price limits
- [ ] Document optimization strategies

**Acceptance Criteria**:
- Gas costs reduced by 20%+
- Gas price monitoring functional
- Documentation complete

---

## Phase 6: Deployment & Documentation (Week 8)

### TASK-022: Deploy Contracts to Testnet
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-002

- [ ] Set up Infura or Alchemy account
- [ ] Configure Hardhat for Sepolia testnet
- [ ] Deploy contracts to testnet
- [ ] Verify contracts on Etherscan
- [ ] Update configuration with testnet addresses
- [ ] Test contracts on testnet
- [ ] Document deployment process

**Acceptance Criteria**:
- Contracts deployed to testnet
- Contracts verified on Etherscan
- Configuration updated
- Tests pass on testnet

---

### TASK-023: Configure Production Environment
**Priority**: High  
**Effort**: 4h  
**Dependencies**: TASK-022

- [ ] Set up production blockchain node (Infura/Alchemy)
- [ ] Configure wallet private key in Azure Key Vault or AWS Secrets Manager
- [ ] Update production configuration
- [ ] Set up monitoring and alerting
- [ ] Configure circuit breaker thresholds
- [ ] Document production setup

**Acceptance Criteria**:
- Production configuration complete
- Secrets stored securely
- Monitoring configured
- Documentation complete

---

### TASK-024: Create Documentation
**Priority**: Medium  
**Effort**: 4h  
**Dependencies**: All tasks

- [ ] Write blockchain integration guide
- [ ] Document smart contract deployment
- [ ] Create API documentation
- [ ] Write developer setup guide
- [ ] Document troubleshooting steps
- [ ] Create architecture diagrams

**Acceptance Criteria**:
- All documentation complete
- Diagrams included
- Setup guide tested

---

### TASK-025: Security Audit
**Priority**: High  
**Effort**: 8h  
**Dependencies**: All tasks

- [ ] Review smart contracts for vulnerabilities
- [ ] Audit private key management
- [ ] Review transaction signing logic
- [ ] Check input validation
- [ ] Review error handling
- [ ] Test for common attacks (reentrancy, overflow, etc.)
- [ ] Document security measures

**Acceptance Criteria**:
- No critical vulnerabilities found
- Security measures documented
- Audit report created

---

## Summary

**Total Estimated Effort**: ~120 hours (15 working days)

**Phase Breakdown**:
- Phase 1 (Foundation): 18h
- Phase 2 (Core Service): 22h
- Phase 3 (API): 15h
- Phase 4 (Frontend): 20h
- Phase 5 (Testing): 18h
- Phase 6 (Deployment): 20h

**Critical Path**: TASK-001 → TASK-002 → TASK-003 → TASK-004 → TASK-005 → TASK-006 → TASK-011 → TASK-012


