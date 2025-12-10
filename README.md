# 🤖 LLM SQL Agent

[![Deploy to Production](https://github.com/zhukovskyy/llm-sql-agent/actions/workflows/deploy.yml/badge.svg)](https://github.com/zhukovskyy/llm-sql-agent/actions/workflows/deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

AI-powered SQL query agent that converts natural language questions into secure SQL queries. Features intelligent retry logic, comprehensive security validation, and real-time database interaction. Built with .NET 8, OpenAI GPT-4, and includes sandbox protection against dangerous operations. Perfect for business analysts and data exploration without SQL knowledge.

## ✨ Features

- 🧠 **Natural Language to SQL** - Convert plain English queries to SQL
- 🔄 **Intelligent Retry Logic** - Automatically learns from validation errors (up to 4 attempts)
- 🛡️ **Security Sandbox** - Blocks dangerous operations (DROP, DELETE, UPDATE without WHERE, etc.)
- 📊 **Real-time Execution** - Direct database querying with result visualization
- 🎯 **Multi-level Query Testing** - From simple SELECT to complex JOINs and aggregations
- 🌐 **Modern Web UI** - Beautiful, responsive interface with example queries
- 🔄 **Database Initialization** - One-click setup with test data
- 🚀 **CI/CD Ready** - Automatic deployment via GitHub Actions

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- SQL Server database
- OpenAI API key

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/zhukovskyy/llm-sql-agent.git
   cd llm-sql-agent
   ```

2. **Configure settings**
   
   Copy the example settings file:
   ```bash
   cd DatabaseDemo
   cp appsettings.example.json appsettings.json
   ```

   Edit `appsettings.json` with your credentials:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;"
     },
     "OpenAI": {
       "ApiKey": "sk-your-openai-api-key"
     }
   }
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Open in browser**
   ```
   http://localhost:5165
   ```

## 📊 Database Schema

The project includes a test database with three tables:

### employees
Personnel information with RFID tracking
- `id` - Unique employee ID
- `full_name` - Name and surname
- `role` - Job role (Driver, Technician, Manager, etc.)
- `department` - HR, Logistics, Warehouse, Maintenance
- `rfid_tag` - RFID tag for check-in/out

### inventory_items
Warehouse inventory tracking
- `id` - Unique item ID
- `item_name` - Product/item name
- `stock_quantity` - Current stock count
- `last_updated_by` - Employee name
- `last_update_rfid` - RFID of person who updated

### vehicle_maintenance_logs
Fleet maintenance operations
- `id` - Unique log ID
- `vehicle_plate` - Truck/van plate number
- `issue` - Reported problem
- `maintenance_date` - Date of maintenance
- `technician_name` - Employee who performed work
- `technician_rfid` - RFID at time of maintenance

## 🧪 Test Query Levels

### Level 1 - Simple Single-Table Queries
```
"List all employees working in the Logistics department"
"Show all inventory items that have less than 20 units in stock"
```

### Level 2 - Moderate Multi-Table Queries
```
"Show me the names of employees who last updated any inventory item"
"Which technicians performed maintenance in the last 7 days?"
```

### Level 3 - Complex Multi-Relation + Business Logic
```
"List all employees whose RFID tag was used in either inventory or maintenance"
"Show employees who updated inventory AND performed vehicle maintenance"
```

### Level 4 - Aggregation & Analytics
```
"Show each employee with total inventory updates and maintenance operations"
"Which vehicles require the most maintenance? Show count by vehicle plate"
```

## 🛡️ Security Features

- ✅ Blocks dangerous SQL operations (DROP, TRUNCATE, ALTER, CREATE, DELETE, UPDATE)
- ✅ Validates UPDATE/DELETE must have WHERE clause
- ✅ Detects SQL injection patterns
- ✅ Prevents multiple statement execution
- ✅ Only allows SELECT queries for data analysis
- ✅ Word boundary validation to avoid false positives

## 🏗️ Architecture

```
├── Services/
│   ├── LlmAgent.cs          # OpenAI integration with retry logic
│   ├── SqlSandbox.cs        # Security validation layer
│   ├── SqlExecutor.cs       # Database query execution
│   └── DatabaseInitializer.cs # Test data generation
├── Models/
│   ├── ChatRequest.cs       # API request models
│   ├── ChatResponse.cs      # API response models
│   └── OpenAIModels.cs      # OpenAI API models
└── wwwroot/
    └── index.html           # Modern web UI
```

## 🔧 Configuration

### Connection String Format
```
Server=tcp:YOUR_SERVER,PORT;Initial Catalog=DATABASE;User ID=USER;Password=PASSWORD;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;
```

### OpenAI Settings
- Default model: `gpt-4o-mini`
- Max tokens: 1000
- Temperature: 0 (for consistent SQL generation)

## 📦 Dependencies

- ASP.NET Core 8.0
- Microsoft.Data.SqlClient
- System.Text.Json
- OpenAI API

## 🚢 Deployment

### Using Visual Studio Publish Profile
```bash
dotnet publish -c Release
```

### Web Deploy (IIS)
```powershell
$password = Read-Host -AsSecureString -Prompt "Enter password"
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
dotnet publish /p:PublishProfile=YourProfile /p:Configuration=Release /p:Password=$PlainPassword
```

## 📝 API Endpoints

- `GET /health` - Health check and database connectivity
- `GET /schema` - Retrieve current database schema
- `POST /chat` - Submit natural language query
- `POST /api/init-database` - Initialize database with test data

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔗 Links

- [Live Demo](http://bai.a95.biz:80/)
- [GitHub Repository](https://github.com/zhukovskyy/llm-sql-agent)

## ⚠️ Security Notice

**Never commit `appsettings.json` with real credentials!** Always use environment variables or secure secret management in production.

## 👨‍💻 Author

Developed by [zhukovskyy](https://github.com/zhukovskyy)

## 🙏 Acknowledgments

- OpenAI for GPT-4 API
- .NET Team for ASP.NET Core
- Microsoft for SQL Server Client
