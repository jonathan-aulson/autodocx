# ⚡ K6 Performance Testing Framework

TypeScript-based K6 framework for running **stress**, **spike**, and **soak** load tests with clean architecture and environment-based configuration.

---

## 🚀 Quick Start

```bash
# Build the K6 TypeScript scripts
npm run k6:build

# Run tests by environment
npm run k6:test:dev     # Development
npm run k6:test:qa      # QA
npm run k6:test:stage   # Staging
```

---

## 📁 Project Structure

```
k6/
├── src/
│   ├── scenarios/     # Test scenarios (stress/spike/soak)
│   ├── config/        # Shared configuration logic
│   └── utils/         # Common utility functions
├── env/               # Environment-specific .env files
└── reports/           # Generated test reports (HTML & JSON)
```

---

## 🧪 Test Types & Commands

### ✅ Stress Tests

```bash
npm run k6:stress:powerbill:dev      # Statement generation
npm run k6:stress:forecast:dev       # Forecast setup
npm run k6:stress:mass-edit:qa       # Mass editing
npm run k6:test:all:stress           # Run all stress scenarios
```

### ⚠️ Spike Tests

```bash
npm run k6:spike:tab-switch:qa       # Tab switching
npm run k6:spike:navigation:qa       # Navigation flow
npm run k6:test:all:spike            # Run all spike scenarios
```

### 🛡 Soak Tests

```bash
npm run k6:soak:extended:stage       # Extended editing session
npm run k6:soak:system:stage         # Full system load
npm run k6:test:all:soak             # Run all soak scenarios
```

---

## ⚙️ Configuration

Environment variables are defined in `env/*.env` files:

```env
BASE_URL=https://your-app.com
SCENARIO=stress                      # stress | spike | soak
TEST_TYPE=powerbill-statement       # See available test types
LOAD_LEVEL=low                       # very-low | low | medium | high | peak
MAX_USERS=100
```

---

## 🔧 Development Commands

```bash
npm run build           # Compile TypeScript
npm run build:watch     # Watch for file changes and rebuild
npm run type-check      # Perform TypeScript type checking
npm run lint            # Lint the codebase
```

---

## 📊 Reports

After running tests, reports are generated in the `reports/` directory:

- HTML: `reports/*.html`
- JSON: `reports/*.json`

You can integrate these reports into CI dashboards or visualize them using external tools.

---

## 📌 Notes

- All tests are environment-aware and scenario-driven.
- Load levels and users can be configured via `.env` files.
- Easily extend the framework with new scenarios in `src/scenarios`.
