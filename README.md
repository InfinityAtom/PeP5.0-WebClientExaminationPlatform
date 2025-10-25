# 🎓 PeP 5.0 — Programming Examination Platform

[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-blue.svg?style=flat-square)](https://blazor.net/)
[![Node.js](https://img.shields.io/badge/Node.js-18+-green.svg?style=flat-square)](https://nodejs.org/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-UI-orange.svg?style=flat-square)](https://mudblazor.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

A comprehensive, full-stack AI-powered programming examination platform designed for educational institutions. Features a secure, locked-down IDE environment with complete exam lifecycle management, from teacher preparation to student evaluation.

## ✨ **Core Technologies**

- **Frontend**: .NET 9 Blazor Server + MudBlazor UI + Entity Framework Core
- **Backend**: Node.js (Express) + SQLite Database
- **Security**: JWT Authentication + Role-based Authorization
- **Language Support**: Java (JDK required for compilation and execution)
- **AI Integration**: OpenAI Chat Completions for intelligent exam generation and grading

## 🚀 **Key Features**

### 🎯 **Examination Environment**
- **AI-Powered Exam Generation**: Single-click creation of realistic programming exams with CSV datasets
- **Secure IDE Interface**: Locked-down coding environment with Ace Editor integration
- **Real-time Code Execution**: Compile and run Java programs with instant feedback
- **Advanced Security**: Client-side protections against cheating (copy/paste blocking, fullscreen enforcement, dev tools prevention)
- **Intelligent Evaluation**: AI-first grading with GPT-4 integration and heuristic fallbacks

### 👨‍🏫 **Teacher Management Portal**
- **📚 Class Management**: Create and organize classes with student enrollment
- **🏫 Room & Seatmap Configuration**: Visual room layouts with computer assignments
- **📅 Session Scheduling**: Complete exam session lifecycle with booking management
- **📝 Practice Test Creation**: IDE-based and MCQ practice tests for student preparation
- **📊 Comprehensive Analytics**: Grade tracking, submission analysis, and performance insights
- **⚙️ Computer Configuration**: Per-desk management with hostname, IP, and hardware details

### 👨‍🎓 **Student Experience**
- **📖 Exam Booking System**: Reserve seats for scheduled examination sessions
- **🎯 Practice Environment**: Access to practice tests and preparation materials  
- **📱 Responsive Dashboard**: Track upcoming exams, grades, and class progress
- **🔒 Secure Authentication**: Role-based access with JWT token security

### 🛡️ **Security & Integrity**
- **Multi-layered Protection**: 
  - Fullscreen enforcement with overlay guidance
  - Context menu and right-click blocking
  - Copy/paste/selection restrictions  
  - Keyboard shortcut interdiction (F12, Ctrl+R, etc.)
  - Visual watermarks and timing indicators
- **Exam Environment Controls**:
  - Console overlay with color-coded output
  - CSV data viewer with virtualization
  - Run/Reset/Submit workflow with confirmations
  - Time tracking and session management

### 📈 **Advanced Management Features**
- **Smart Session Handling**: 
  - Conflict detection for booking overlaps
  - Force delete capabilities with cascade warnings
  - Real-time status tracking and updates
- **Flexible Assessment Tools**:
  - Multiple exam types (Java, general programming)
  - AI-generated vs manual content options
  - Fallback exam system for offline scenarios
- **Comprehensive Reporting**:
  - Per-task evaluation breakdown
  - Final grade formatting (`10(ten)` standard)
  - Time spent analysis and performance metrics

## 📁 **Project Structure**

```
📦 V5Old.sln                                    # Visual Studio Solution
└── 🗂️ AIExamIDE/
    ├── 🐳 docker-compose.yml                   # Container orchestration
    ├── 🖥️ client/                               # Blazor Server Application  
    │   ├── 🐳 Dockerfile                        # Multi-stage .NET 9 container
    │   ├── ⚙️ Program.cs                        # Application entry point & DI setup
    │   ├── 📄 AIExamIDE.csproj                  # Project configuration
    │   ├── 📦 package.json                      # Tailwind CSS build scripts
    │   │
    │   ├── 🗂️ Backend/                          # 🆕 Backend Infrastructure Layer
    │   │   ├── 🔐 Auth/                         # JWT Authentication System
    │   │   │   ├── JwtOptions.cs               # JWT configuration options
    │   │   │   ├── JwtTokenService.cs          # Token generation & validation
    │   │   │   └── UserContextExtensions.cs    # User context helpers
    │   │   ├── 📄 Contracts/                    # API Contracts & DTOs
    │   │   │   ├── DtoMapping.cs               # Entity-to-DTO mapping
    │   │   │   ├── Requests.cs                 # Request models
    │   │   │   └── UpdateComputerRequest.cs    # Computer update contract
    │   │   ├── 🗄️ Data/                        # Database Layer
    │   │   │   ├── AppDbContext.cs             # EF Core DbContext
    │   │   │   ├── Entities.cs                 # Database entities
    │   │   │   └── SchemaInitializer.cs        # Database initialization
    │   │   ├── 🔧 Services/
    │   │   │   └── AppRepository.cs            # Data access repository
    │   │   └── 🖨️ SeatmapPrintTemplate.cs       # Seatmap printing template
    │   │
    │   ├── 🧩 Components/                       # Blazor UI Components
    │   │   ├── 📄 _Imports.razor               # Global component imports
    │   │   ├── 🏠 App.razor                    # Root application component  
    │   │   ├── 🛣️ Routes.razor                 # Application routing
    │   │   │
    │   │   ├── 💬 Dialogs/                     # 🆕 Specialized Dialog Components
    │   │   │   ├── AddFileDialog.razor         # File creation dialog
    │   │   │   ├── ClassEditDialog.razor       # Class management dialog
    │   │   │   ├── DeleteFileDialog.razor      # File deletion confirmation
    │   │   │   ├── DeleteSessionDialog.razor   # Session deletion with force option
    │   │   │   └── RenameFileDialog.razor      # File renaming dialog
    │   │   │
    │   │   ├── 🎨 Layout/                      # Application Layout
    │   │   │   ├── MainLayout.razor            # Main application layout
    │   │   │   ├── MainLayout.razor.css        # Layout-specific styles
    │   │   │   └── NavMenu.razor               # Navigation menu
    │   │   │
    │   │   └── 📃 Pages/                       # Application Pages
    │   │       ├── 🏠 Home.razor               # Main IDE interface
    │   │       ├── 📊 Results.razor            # Evaluation results display
    │   │       ├── 🏠 PortalHome.razor         # 🆕 Portal landing page
    │   │       │
    │   │       ├── 🔐 Auth/                    # 🆕 Authentication Pages
    │   │       │   ├── Login.razor             # User login page
    │   │       │   └── Register.razor          # User registration page
    │   │       │
    │   │       ├── 👨‍🏫 Teacher/                  # 🆕 Teacher Management Portal
    │   │       │   ├── Dashboard.razor         # Teacher dashboard overview
    │   │       │   ├── Classes.razor           # Class management interface
    │   │       │   ├── Sessions.razor          # Exam session management
    │   │       │   ├── SessionBookings.razor   # Booking management per session
    │   │       │   ├── Rooms.razor             # Room configuration interface
    │   │       │   ├── ComputerConfiguration.razor # Seatmap & computer setup
    │   │       │   ├── Submissions.razor       # Submission review & grading
    │   │       │   ├── PracticeTests.razor     # Practice test management
    │   │       │   ├── CreatePracticeTest.razor # Practice test creation
    │   │       │   ├── EditPracticeTest.razor  # Practice test editing
    │   │       │   ├── Practice.razor          # Practice test preview
    │   │       │   ├── FallbackExam.razor      # Fallback exam configuration
    │   │       │   └── JsonPreviewDialog.razor # JSON data preview dialog
    │   │       │
    │   │       └── 👨‍🎓 Student/                  # 🆕 Student Portal
    │   │           ├── Dashboard.razor         # Student dashboard
    │   │           ├── Classes.razor           # Enrolled classes view  
    │   │           ├── Exams.razor             # Available exams listing
    │   │           ├── Book.razor              # Exam booking interface
    │   │           ├── PracticeTests.razor     # Available practice tests
    │   │           └── Practice.razor          # Practice test interface
    │   │
    │   ├── 📄 Models/                          # 🆕 Enhanced Data Models
    │   │   ├── ApiModels.cs                   # API communication models
    │   │   ├── ExamModels.cs                  # Exam-specific models  
    │   │   └── TeacherStudentModels.cs        # User & academic models
    │   │
    │   ├── 🔧 Services/                        # Application Services
    │   │   ├── ApiClient.cs                   # HTTP API communication
    │   │   ├── AuthState.cs                   # 🆕 Authentication state management
    │   │   ├── ExamState.cs                   # Exam session state
    │   │   ├── LocalStorage.cs                # 🆕 Browser storage service
    │   │   └── CsvService.cs                  # 🆕 CSV processing utilities
    │   │
    │   └── 🌐 wwwroot/                        # Static Web Assets
    │       ├── 🎨 css/app.css                 # Compiled Tailwind CSS
    │       ├── 🖼️ images/                     # Application images
    │       └── 📜 js/                         # Client-side JavaScript
    │           ├── fullscreen.js              # Fullscreen enforcement
    │           ├── guard.js                   # Security & anti-cheat measures  
    │           ├── telemetry.js               # Usage analytics
    │           └── tablock.js                 # Tab management utilities
    │
    └── 🖥️ server/                             # Node.js Backend Service
        ├── 🐳 Dockerfile                      # Node.js container configuration
        ├── 📦 package.json                    # Node.js dependencies & scripts
        ├── ⚙️ server.js                       # Express server & API endpoints
        ├── 🗄️ db.js                          # Database connection & queries
        ├── 📧 email.js                       # Email notification service
        ├── 📂 repo.js                        # Repository pattern implementation
        ├── 🗂️ data/db.json                   # SQLite database file
        ├── 📁 workspace/                      # Temporary exam workspace
        │   ├── src/                          # Student source code files
        │   └── data/                         # CSV datasets for exams
        └── 📥 submissions/                    # Permanent submission storage
            └── submission-{timestamp}/        # Individual submission folders
```

### 🆕 **Major Additions in V5.0**

#### 🔐 **Authentication & Authorization System**
- JWT-based authentication with role management (Teacher/Student)
- Secure login/registration with password hashing
- Protected routes and role-based access control

#### 🗄️ **Database Integration** 
- Full Entity Framework Core implementation with SQLite
- Comprehensive data models for users, classes, sessions, bookings
- Automated database migrations and schema initialization

#### 👨‍🏫 **Complete Teacher Management Portal**
- **Class Management**: Create, edit, and manage student classes
- **Session Scheduling**: Full exam lifecycle from creation to evaluation  
- **Room Configuration**: Visual seatmap editor with computer assignments
- **Practice Test System**: Create IDE and MCQ-based practice assessments
- **Analytics Dashboard**: Performance tracking and submission analysis

#### 👨‍🎓 **Student Portal & Booking System**
- **Exam Booking**: Reserve seats for scheduled examination sessions
- **Class Enrollment**: Join classes and track academic progress
- **Practice Environment**: Access to preparation materials and mock tests
- **Personal Dashboard**: Unified view of exams, grades, and activities

#### 💬 **Enhanced UI Components**
- Specialized dialog components for complex workflows
- Improved session management with force-delete capabilities
- Advanced seatmap printing and layout management
- Responsive design with MudBlazor component library

## 🛠️ **System Requirements**

### **Required Dependencies**
| Component | Version | Purpose |
|-----------|---------|---------|
| 🪟 **Operating System** | Windows, macOS, Linux | Cross-platform support |
| 🔷 **.NET SDK** | 9.0+ | Blazor Server application runtime |
| 🟢 **Node.js** | 18+ | Backend API server |
| ☕ **Java JDK** | 17+ | Code compilation and execution |
| 🗄️ **SQLite** | Latest | Local database storage |

### **Optional Components**
| Component | Purpose |
|-----------|---------|
| 🐳 **Docker Desktop** | Container deployment option |
| 🤖 **OpenAI API Key** | AI-powered exam generation and grading |

## ⚡ **Quick Start Guide**

### **🚀 Method 1: Local Development Setup**

#### **Step 1: Start the Backend Server**
```powershell
# Navigate to server directory
cd AIExamIDE/server

# Install Node.js dependencies  
npm install

# Set OpenAI API key (optional - fallback exams used if not set)
$env:OPENAI_API_KEY = "your-openai-api-key-here"

# Start the development server
node server.js
# Alternative: Use nodemon for auto-restart
# npx nodemon server.js
```

#### **Step 2: Launch the Frontend Application**
```powershell
# Open new terminal and navigate to client directory
cd AIExamIDE/client

# Build Tailwind CSS (first-time setup)
npx tailwindcss -i ./src/input.css -o ./wwwroot/css/app.css --watch

# In another terminal, start the Blazor application
dotnet run
```

#### **Step 3: Access the Application**
| Service | URL | Description |
|---------|-----|-------------|
| 🖥️ **Frontend** | `http://localhost:5183` | Main application interface |
| 🔧 **Backend API** | `http://localhost:3000` | REST API endpoints |

> **Note**: The exact frontend port may vary. Check the terminal output from `dotnet run` for the actual URL.

### **🐳 Method 2: Docker Deployment**

```powershell
# Navigate to the AIExamIDE directory
cd AIExamIDE

# Set environment variables (create .env file or export)
$env:OPENAI_API_KEY = "your-openai-api-key-here"

# Build and start both containers
docker compose up --build
```

#### **Docker Service URLs**
| Service | URL | Description |
|---------|-----|-------------|
| 🖥️ **Frontend** | `http://localhost:5000` | Blazor Server application |
| 🔧 **Backend** | `http://localhost:3000` | Node.js API server |

#### **Docker Configuration Notes**
- 📁 **Persistent Storage**: Named volume `examdata` preserves workspace between restarts
- 🤖 **AI Integration**: Set `OPENAI_API_KEY` for GPT-powered features, otherwise fallback exams are used
- 🗄️ **Database**: SQLite database persisted in container volumes

---

## 🔄 **System Workflow**

### **📚 Exam Lifecycle**

#### **1️⃣ Teacher Preparation**
```
🏫 Room Setup → 👥 Class Creation → 📅 Session Scheduling → 📝 Practice Tests
```
- Configure examination rooms with seatmap layouts
- Create and manage student classes  
- Schedule exam sessions with specific dates/times
- Prepare practice materials for student preparation

#### **2️⃣ Student Registration & Booking** 
```
🔐 Authentication → 📚 Class Enrollment → 🎯 Exam Booking → 📍 Seat Assignment  
```
- Register and authenticate with role-based access
- Join assigned classes via teacher invitation
- Browse and book available examination sessions
- Receive assigned seat and exam details

#### **3️⃣ Examination Process**
```
🤖 Exam Generation → 💻 Coding Environment → 🏃 Code Execution → 📤 Submission
```

**3a. AI-Powered Exam Generation**
- Frontend triggers `POST /exam` on first access
- Single-flight protection prevents duplicate generation
- localStorage caching ensures session persistence across refreshes
- Realistic CSV datasets generated with relational data
**3b. Secure Coding Environment** 
- Files displayed in interactive tree structure
- Ace Editor with syntax highlighting and tabbed interface
- CSV files open in virtualized data viewer
- Auto-opening console with color-coded output on first run

**3c. Real-time Code Execution**
- `POST /run` compiles and executes Java programs
- Instant feedback with compilation errors and runtime output
- Sandboxed execution environment for security

**3d. Submission & Confirmation**
- Confirmation dialog prevents accidental submissions
- Files sent via `POST /submit` and archived permanently  
- Automatic timestamp and student identification

#### **4️⃣ Evaluation & Results**
```
🤖 AI Grading → 📊 Heuristic Analysis → 📈 Final Scoring → 📄 Results Display
```
- `POST /evaluate` triggers comprehensive assessment
- Primary: GPT-4 based evaluation with strict JSON contracts
- Fallback: Structured heuristic analysis for reliability
- Final grade in standard format (e.g., `10(ten)`)
- Detailed per-task breakdown and performance metrics

### **👨‍🏫 Teacher Management Flow**
```
📊 Dashboard Overview → 👥 Student Management → 📅 Session Control → 📝 Grade Review
```
- Real-time analytics on exam performance and participation
- Comprehensive student and class management tools
- Session monitoring with booking oversight
- Detailed submission review and manual grade adjustment capabilities

---

## 🛡️ **Security & Exam Integrity**

### **🔒 Multi-Layer Security Architecture**

#### **Client-Side Protection (`wwwroot/js/guard.js`, `wwwroot/js/fullscreen.js`)**
- **Input Blocking**: Comprehensive copy/cut/paste, text selection, and drag/drop prevention
- **Navigation Controls**: Intercepts F12, Ctrl+R, F5, browser navigation, and zoom shortcuts  
- **Fullscreen Enforcement**: High z-index overlay with app dimming when fullscreen is exited
- **Session Protection**: Beforeunload confirmation to prevent accidental exits
- **Context Blocking**: Right-click context menu and developer tools prevention

#### **Server-Side Authentication & Authorization**
- **🔐 JWT Token System**: Secure authentication with role-based access control
- **👤 User Roles**: Teacher and Student roles with granular permission sets
- **🛡️ Protected Routes**: API endpoint protection with middleware validation
- **⚡ Rate Limiting**: Basic rate limits on sensitive examination endpoints
- **🔄 Session Management**: Secure token refresh and logout handling

#### **Database Security**
- **🗄️ Entity Framework Protection**: Parameterized queries and SQL injection prevention
- **🔒 Data Encryption**: Secure password hashing with BCrypt
- **🚪 Access Control**: Database operations restricted by user roles and ownership

#### **Examination Environment Controls**
- **⏱️ Time Tracking**: Server-side session timing with client-side indicators
- **📱 Device Binding**: Seat assignment and computer-based access control  
- **🎯 Submission Integrity**: Cryptographic timestamp and user verification
- **🔍 Anti-Cheat Measures**: Multiple behavioral and technical deterrents

> **⚠️ Security Notice**: Client-side controls serve as deterrents and user experience enhancements. For production deployment, implement additional server-side monitoring, proctoring solutions, and network-level controls as needed.

---

## ⚙️ **Configuration & Environment Setup**

### **🔧 Frontend Configuration (`AIExamIDE/client/Program.cs`)**
```csharp
// API Communication
services.AddScoped<ApiClient>(sp => new ApiClient { BaseAddress = "http://localhost:3000" });

// Authentication & State Management  
services.AddScoped<AuthState>();
services.AddScoped<ExamState>();
services.AddScoped<LocalStorage>();

// Database & Repository
services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
services.AddScoped<AppRepository>();

// JWT Authentication
services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
services.AddScoped<JwtTokenService>();
```

### **🌐 Backend Configuration (`AIExamIDE/server/server.js`)**
| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `3000` | Express server port |
| `OPENAI_API_KEY` | *(optional)* | GPT-4 integration for AI-powered exam generation |
| `DB_PATH` | `./data/db.json` | SQLite database file location |
| `WORKSPACE_ROOT` | `./workspace` | Temporary exam workspace directory |
| `SUBMISSIONS_ROOT` | `./submissions` | Permanent submission storage |

### **🐳 Docker Environment (`docker-compose.yml`)**
```yaml
# Service Ports
frontend: localhost:5000
backend:  localhost:3000

# Required Environment Variables
OPENAI_API_KEY: "your-api-key-here"  # Optional - fallbacks available

# Named Volumes  
examdata: /app/server/workspace      # Persistent workspace
dbdata:   /app/server/data          # Database storage
```

### **🗄️ Database Configuration**
```json
// Connection Strings (appsettings.json)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Jwt": {
    "Secret": "your-jwt-secret-key-min-32-chars",
    "Issuer": "PeP-ExamPlatform",
    "Audience": "PeP-Users",
    "TokenLifetime": "24:00:00"
  }
}
```

### **🛠️ Development Environment Variables**
Create `.env` file in `AIExamIDE/` directory:
```env
# OpenAI Integration (Optional)
OPENAI_API_KEY=your_openai_api_key_here

# JWT Security (Required for Auth)
JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters_long

# Database Configuration
DB_CONNECTION_STRING=Data Source=./data/app.db

# Server Configuration  
NODE_ENV=development
PORT=3000
CORS_ORIGIN=http://localhost:5183
```

---

## 🔧 **Development & Troubleshooting**

### **🐛 Common Issues & Solutions**

#### **Frontend Connection Issues**
```powershell
# Check if backend is running
curl http://localhost:3000/health

# Verify frontend can reach API
# Check browser console for CORS or connection errors
```

#### **Exam Generation Problems**
```javascript
// Clear exam cache if multiple exams are generated
localStorage.removeItem('examCacheV1');
location.reload();

// Check Network tab for duplicate POST /exam calls
// Should only see one request per session
```

#### **Build & Runtime Errors**
```powershell
# Stop all running processes
Get-Process -Name "dotnet" | Stop-Process
Get-Process -Name "node" | Stop-Process

# Clean and rebuild
dotnet clean
dotnet build
dotnet run

# Database issues - reset and recreate
rm .\data\app.db
dotnet ef database update
```

#### **Authentication Issues**
```powershell
# Check JWT configuration in appsettings.json
# Ensure JWT secret is at least 32 characters
# Verify user roles in database

# Reset authentication state
localStorage.removeItem('authToken');
sessionStorage.clear();
```

### **📝 Development Workflow**
```powershell
# 1. Start backend with hot reload
cd AIExamIDE/server
npx nodemon server.js

# 2. Start frontend with CSS watch
cd AIExamIDE/client  
npx tailwindcss -i ./src/input.css -o ./wwwroot/css/app.css --watch

# 3. Run Blazor with hot reload (new terminal)
dotnet watch run

# 4. Optional: Database management
dotnet ef migrations add "MigrationName"
dotnet ef database update
```

### **🧪 Testing & Debugging**
```powershell
# View database contents
dotnet ef dbcontext info
# Use SQLite browser to inspect data

# API testing with curl
curl -X POST http://localhost:3000/exam -H "Content-Type: application/json"

# Frontend debugging
# Enable Blazor debugging in browser: Shift+Alt+D
```

---

## 🔌 **API Reference**

### **🎯 Core Examination APIs**
| Method | Endpoint | Description | Response |
|--------|----------|-------------|-----------|
| `POST` | `/exam` | AI-powered exam generation | `{ exam, files, examId }` |
| `POST` | `/run` | Compile & execute Java code | `{ output, error, success }` |
| `POST` | `/reset` | Clear workspace & regenerate | `{ message, success }` |
| `POST` | `/submit` | Store submission permanently | `{ submissionId, timestamp }` |
| `POST` | `/evaluate` | AI/heuristic evaluation | `{ evaluation, final_grade, timestamp }` |

### **🔐 Authentication & User Management**
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/auth/login` | User authentication | ❌ |
| `POST` | `/auth/register` | User registration | ❌ |
| `GET` | `/auth/me` | Current user profile | ✅ |
| `POST` | `/auth/refresh` | Token refresh | ✅ |
| `POST` | `/auth/logout` | Session termination | ✅ |

### **👨‍🏫 Teacher Management Portal**

#### **📚 Class & Student Management**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/teacher/classes` | List teacher's classes |
| `POST` | `/api/teacher/classes` | Create new class |
| `PUT` | `/api/teacher/classes/{id}` | Update class details |
| `DELETE` | `/api/teacher/classes/{id}` | Delete class |
| `GET` | `/api/teacher/classes/{id}/students` | List class students |
| `POST` | `/api/teacher/classes/{id}/students` | Add student to class |
| `DELETE` | `/api/teacher/classes/{id}/students/{studentId}` | Remove student |

#### **📅 Session & Booking Management**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/teacher/sessions` | List teacher's exam sessions |
| `POST` | `/api/teacher/sessions` | Create exam session |
| `PUT` | `/api/teacher/sessions/{id}` | Update session details |
| `DELETE` | `/api/teacher/sessions/{id}` | Delete session (conflicts if bookings exist) |
| `DELETE` | `/api/teacher/sessions/{id}?force=true` | **Force delete** session with bookings |
| `GET` | `/api/teacher/sessions/{id}/bookings` | List session bookings |

#### **🏫 Room & Computer Configuration**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/teacher/rooms` | List all examination rooms |
| `GET` | `/api/teacher/rooms/{id}` | Get room with seatmap |
| `PUT` | `/api/teacher/rooms/{id}` | Update room & seatmap |
| `GET` | `/api/teacher/rooms/{id}/print` | Generate printable seatmap |
| `PUT` | `/api/teacher/computers/{deskId}` | Update individual computer config |

#### **📝 Practice Test Management**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/teacher/practice-tests` | List teacher's practice tests |
| `POST` | `/api/teacher/practice-tests` | Create practice test |
| `PUT` | `/api/teacher/practice-tests/{id}` | Update practice test |
| `DELETE` | `/api/teacher/practice-tests/{id}` | Delete practice test |

#### **📊 Analytics & Reporting**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/teacher/dashboard` | Teacher analytics overview |
| `GET` | `/api/teacher/submissions` | Review student submissions |
| `GET` | `/api/teacher/submissions/{id}` | Detailed submission review |
| `PUT` | `/api/teacher/submissions/{id}/grade` | Manual grade override |

### **👨‍🎓 Student Portal APIs**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/student/dashboard` | Student dashboard data |
| `GET` | `/api/student/classes` | Enrolled classes |
| `GET` | `/api/student/exams` | Available exam sessions |
| `POST` | `/api/student/exams/{id}/book` | Book exam seat |
| `DELETE` | `/api/student/bookings/{id}` | Cancel booking |
| `GET` | `/api/student/practice-tests` | Available practice tests |
| `POST` | `/api/student/practice-tests/{id}/submit` | Submit practice attempt |

### **⚠️ Advanced API Behaviors**

#### **🗑️ Force Delete Sessions**
Examination sessions with student bookings require special handling:

**Standard Delete (Protected)**
```http
DELETE /api/teacher/sessions/{id}
```
```json
// Response: 409 Conflict
{
  "error": "Session has active bookings",
  "bookingCount": 3,
  "message": "Add ?force=true to delete session with bookings"
}
```

**Force Delete (Cascade)**
```http
DELETE /api/teacher/sessions/{id}?force=true
```
```json
// Response: 200 OK
{
  "success": true,
  "deletedBookings": 3,
  "forceUsed": true,
  "message": "Session deleted along with 3 booking(s)"
}
```

> **⚠️ Cascade Warning**: Force deletion immediately removes student reservations and associated submission data.

#### **🔐 Authentication Flow**
All authenticated endpoints require JWT tokens in the Authorization header:
```http
Authorization: Bearer <jwt-token>
```

**Role-Based Access**:
- **Teacher routes** (`/api/teacher/*`): Require `teacher` role
- **Student routes** (`/api/student/*`): Require `student` role  
- **Admin routes** (`/api/admin/*`): Require `admin` role (future)

---

## 🗓️ **Roadmap & Future Enhancements**

### **🚀 Version 5.1 - Enhanced Security & Monitoring**
- [ ] **Advanced Proctoring**: Webcam integration and behavior monitoring  
- [ ] **Real-time Analytics**: Live exam monitoring dashboard for teachers
- [ ] **Enhanced Anti-Cheat**: Advanced detection algorithms and network monitoring
- [ ] **Audit Logging**: Comprehensive activity tracking and compliance reporting

### **📊 Version 5.2 - Advanced Assessment Features**
- [ ] **Multi-Language Support**: Python, C++, JavaScript exam types
- [ ] **Advanced Question Types**: Code review, debugging, algorithm design
- [ ] **Plagiarism Detection**: Code similarity analysis and cross-submission comparison  
- [ ] **Adaptive Difficulty**: AI-powered difficulty adjustment based on student performance

### **🔧 Version 5.3 - Platform Improvements**
- [ ] **Microservices Architecture**: Containerized service separation
- [ ] **Horizontal Scaling**: Load balancing and distributed processing
- [ ] **Advanced Database**: PostgreSQL migration with performance optimization
- [ ] **Cloud Integration**: AWS/Azure deployment with managed services

### **🎯 Version 6.0 - Enterprise Features**  
- [ ] **LMS Integration**: Canvas, Blackboard, Moodle connectivity
- [ ] **SSO Authentication**: SAML, OAuth2, Active Directory integration
- [ ] **Multi-tenant Architecture**: Institution isolation and branding
- [ ] **Advanced Analytics**: Machine learning insights and predictive analytics

### **✅ Recently Completed (V5.0)**
- [x] **Complete Authentication System** with JWT and role-based access
- [x] **Teacher Management Portal** with full class and session management
- [x] **Student Booking System** with seat assignment and scheduling
- [x] **Database Integration** with Entity Framework Core and SQLite
- [x] **Practice Test System** with MCQ and IDE-based assessments
- [x] **Enhanced UI/UX** with MudBlazor and responsive design
- [x] **Room Management** with visual seatmap configuration
- [x] **Advanced Session Controls** with force-delete and booking management

## 🎉 **What's New in V5.0**

### **🆕 Major New Features**
This version represents a complete transformation from a simple IDE tool to a comprehensive examination management platform:

#### **🔐 Authentication & User Management**
- **JWT-based Security**: Complete authentication system with role-based access control
- **User Registration**: Self-service registration with teacher/student role assignment  
- **Session Management**: Secure token handling with refresh capabilities

#### **👨‍🏫 Complete Teacher Portal**
- **Class Management**: Create, manage, and organize student classes
- **Session Scheduling**: Full exam lifecycle from creation to completion
- **Room Configuration**: Visual seatmap editor with drag-and-drop desk management
- **Practice Test Creation**: IDE and MCQ-based practice assessment tools
- **Analytics Dashboard**: Real-time performance tracking and insights
- **Submission Review**: Detailed evaluation with manual grade override capabilities

#### **👨‍🎓 Student Experience Portal**  
- **Exam Booking System**: Browse and reserve seats for examination sessions
- **Class Enrollment**: Join classes via teacher invitation or enrollment codes
- **Practice Environment**: Access preparation materials and mock assessments
- **Personal Dashboard**: Unified view of exams, grades, and progress

#### **🗄️ Enterprise-Grade Database System**
- **Entity Framework Core**: Full ORM integration with automatic migrations
- **SQLite Database**: Lightweight, embedded database for development and small deployments
- **Data Models**: Comprehensive entities for users, classes, sessions, bookings, submissions
- **Repository Pattern**: Clean data access with separation of concerns

#### **🎨 Enhanced User Interface**
- **MudBlazor Integration**: Modern, accessible component library
- **Responsive Design**: Mobile-friendly layouts and touch interactions
- **Specialized Dialogs**: Context-aware dialogs for complex workflows
- **Advanced Forms**: Validation, auto-save, and user experience improvements

### **🔧 Technical Improvements**
- **Modular Architecture**: Clean separation between frontend, backend, and data layers
- **Configuration Management**: Environment-based settings with secure secret handling
- **Error Handling**: Comprehensive error boundaries with user-friendly messaging
- **Performance Optimization**: Lazy loading, virtualization, and efficient data queries
- **Security Enhancements**: Multi-layer protection with client and server-side controls

### **📊 Backward Compatibility**
- **Existing Exam Engine**: All previous exam generation and evaluation features preserved
- **API Compatibility**: Core examination APIs remain unchanged for existing integrations
- **Data Migration**: Automatic upgrade path from previous versions
- **Configuration**: Existing Docker and environment configurations supported

---

## 📄 **License**

This project is licensed under the **MIT License**. See the [`LICENSE`](LICENSE) file for full details.

### **Third-Party Licenses**
- **MudBlazor**: MIT License
- **Entity Framework Core**: MIT License  
- **Blazor**: MIT License
- **Ace Editor**: BSD License
- **Tailwind CSS**: MIT License

---

## 🤝 **Contributing**

We welcome contributions! Please see our contributing guidelines:

1. **Fork the repository** and create a feature branch
2. **Make your changes** with appropriate tests and documentation  
3. **Submit a pull request** with a clear description of changes
4. **Ensure all tests pass** and code follows project standards

### **Development Setup**
```powershell
# Clone and setup development environment
git clone https://github.com/InfinityAtom/PeP5.0-WebClientExaminationPlatform.git
cd PeP5.0-WebClientExaminationPlatform

# Follow the Quick Start Guide above
```

---

## 📞 **Support & Contact**

- **📚 Documentation**: Comprehensive guides in the `/docs` directory
- **🐛 Issue Tracking**: Report bugs via GitHub Issues
- **💬 Discussions**: Join community discussions for feature requests and support
- **📧 Contact**: For enterprise support and custom implementations

**Made with ❤️ for educational excellence**
