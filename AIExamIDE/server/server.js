const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { spawn, spawnSync } = require('child_process');
const axios = require('axios');
const crypto = require('crypto');
const telemetryStore = new Map(); // key: sessionId, value: { score, events: [] }
const runRate = new Map(); // sid -> lastTs
const app = express();
const PORT = process.env.PORT || 3000;

// Security Configuration
const ADMIN_SECRET_KEY = process.env.ADMIN_SECRET_KEY || crypto.randomBytes(32).toString('hex');
const SESSION_SECRET = process.env.SESSION_SECRET || crypto.randomBytes(32).toString('hex');

// Store generated exams temporarily (in production, use a database)
const examStore = new Map();
const sessionStore = new Map();

// OpenAI API Configuration
const OPENAI_API_KEY = 'sk-H3XCMlpSOcIbhtzkpRRmTSs5I5VmW5FYoYjK5bEXmWT3BlbkFJQlIjFcjiDT5o58e6QhVUPsjK6J8vaKw8byMQA-ohUA';
const OPENAI_API_URL = 'https://api.openai.com/v1/chat/completions';

// Middleware
app.use(cors());
app.use(express.json({ limit: '10mb' }));

function getSID(req){ return req.query.sid || req.headers['x-session-token'] || req.ip; }

// Security middleware for admin routes
function authenticateAdmin(req, res, next) {
    const authHeader = req.headers.authorization;
    const sessionToken = req.headers['x-session-token'];
    
    // Check for Bearer token or session token
    if (authHeader && authHeader.startsWith('Bearer ')) {
        const token = authHeader.substring(7);
        if (token === ADMIN_SECRET_KEY) {
            return next();
        }
    }
    
    // Check session token
    if (sessionToken && sessionStore.has(sessionToken)) {
        const session = sessionStore.get(sessionToken);
        if (session.expires > Date.now()) {
            return next();
        } else {
            sessionStore.delete(sessionToken);
        }
    }
    
    return res.status(401).json({ 
        error: 'Unauthorized access. Valid authentication required.',
        hint: 'Use /admin/login endpoint to get session token'
    });
}
app.use('/telemetry', express.text({ type: '*/*', limit: '256kb' }));

// Rate limiting for admin endpoints
const adminRateLimit = new Map();
function rateLimitAdmin(req, res, next) {
    const ip = req.ip || req.connection.remoteAddress;
    const now = Date.now();
    const windowMs = 15 * 60 * 1000; // 15 minutes
    const maxAttempts = 10;
    
    if (!adminRateLimit.has(ip)) {
        adminRateLimit.set(ip, { count: 1, resetTime: now + windowMs });
        return next();
    }
    
    const rateData = adminRateLimit.get(ip);
    if (now > rateData.resetTime) {
        rateData.count = 1;
        rateData.resetTime = now + windowMs;
        return next();
    }
    
    if (rateData.count >= maxAttempts) {
        return res.status(429).json({ 
            error: 'Too many requests. Please try again later.',
            retryAfter: Math.ceil((rateData.resetTime - now) / 1000)
        });
    }
    
    rateData.count++;
    next();
}

// Directories
const WORKSPACE_DIR = path.join(__dirname, 'workspace');
const SUBMISSIONS_DIR = path.join(__dirname, 'submissions');

// Ensure directories exist
fs.mkdirSync(WORKSPACE_DIR, { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'src'), { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'data'), { recursive: true });
fs.mkdirSync(SUBMISSIONS_DIR, { recursive: true });

// Enhanced GPT-4o Prompt for multiple CSV files with relationships
const EXAM_GENERATION_PROMPT = `Generate a Java programming exam in STRICT JSON format. You must return ONLY valid JSON with no extra text.

CRITICAL: The JSON must be perfectly formatted with proper quotes, commas, and brackets. No trailing commas allowed.

Required JSON structure:
{
  "exam": {
    "domain": "string",
    "csv_files": [
      {
        "filename": "string",
        "content": [
          {"field1": "value1", "field2": "value2"}
        ]
      }
    ],
    "tasks": [
      {"id": 1, "description": "string"},
      {"id": 2, "description": "string"},
      {"id": 3, "description": "string"},
      {"id": 4, "description": "string"}
    ],
    "overview": "string"
  }
}

Requirements:
1. Domain: Choose ONE unique domain from: "Aerospace Manufacturing", "Smart City Infrastructure", "Precision Agriculture", "Autonomous Vehicle Systems", "Quantum Computing Research", "Biotechnology R&D", "Renewable Energy Grid Management", "Supply Chain Optimization", "Robotics Process Automation", "Cybersecurity Threat Intelligence", "Satellite Communication Networks", "Genomic Data Analysis", "Augmented Reality Training", "Industrial IoT Solutions", "Financial Algorithmic Trading", "Pharmaceutical Drug Discovery", "Maritime Logistics", "Waste Management Solutions", "Geospatial Intelligence", "Elderly Care Monitoring", "Legal Tech Platforms", "Environmental Impact Assessment", "Digital Forensics", "Space Exploration Systems", "Microgrid Energy Management", "AI-Powered Personalization", "Blockchain Supply Chain Tracking", "Virtual Reality Healthcare", "Edge Computing Networks", "Sustainable Urban Planning", "Neuroscience Research Platforms", "Advanced Materials Science", "Predictive Maintenance Solutions", "Humanoid Robotics Development", "Personalized Medicine Delivery", "Deep Learning for Drug Discovery", "Cognitive Computing Systems", "Digital Twin Technology", "Hyper-automation Services", "Ethical AI Development", "Carbon Capture Technologies", "Oceanic Data Analytics", "Asteroid Mining Operations", "Bioinformatics for Gene Editing", "Quantum Cryptography", "Swarm Robotics Applications", "Precision Fermentation", "Exoskeleton Technology", "Haptic Feedback Systems", "Neuromorphic Computing", "AI Ethics & Governance", "Explainable AI (XAI)", "Federated Learning Platforms", "Data Observability Solutions", "MLOps Platforms", "Natural Language Generation (NLG)", "Computer Vision for Quality Control", "Predictive Analytics for HR", "Anomaly Detection Systems", "Recommendation Engine Development", "Decentralized Finance (DeFi)", "Non-Fungible Tokens (NFTs) Management", "Digital Identity Solutions (Blockchain)", "Metaverse Development", "Tokenization of Real-World Assets", "Personalized Nutrition Planning", "Digital Therapeutics", "Remote Patient Monitoring", "Medical Imaging Analysis", "Gene Therapy Development", "Regenerative Medicine", "Telemedicine Platforms", "Clinical Trial Management Systems", "Autonomous Drones for Inspection", "Collaborative Robotics (Cobots)", "Automated Warehousing Systems", "Surgical Robotics", "Satellite Data Analytics", "Space Debris Tracking", "Hypersonic Technology Development", "Defense AI Systems", "Planetary Resource Exploration", "Carbon Footprint Tracking", "Waste-to-Energy Solutions", "Water Resource Management", "Environmental Monitoring Sensors", "Geothermal Energy Systems", "Ocean Cleanup Technologies", "Additive Manufacturing (3D Printing)", "Industrial Cybersecurity", "Smart Factory Solutions", "Quality Control Automation", "Adaptive Learning Platforms", "Gamified Education", "Vocational Training Simulators", "Corporate Learning Management", "Skill-based Credentialing", "RegTech (Regulatory Technology)", "InsurTech (Insurance Technology)", "Legal Document Automation"

2. CSV Files: Create either 1 or 2 CSV files (randomly choose):
   - If 1 CSV: Create 15 rows of realistic data
   - If 2 CSVs: Create a main entity CSV (12-15 rows) and a related entity CSV (18-25 rows) with a foreign key relationship
   
   Examples of relationships:
   - customers.csv (CustomerID) + orders.csv (CustomerID, OrderID)
   - products.csv (ProductID) + reviews.csv (ProductID, ReviewID)
   - students.csv (StudentID) + enrollments.csv (StudentID, CourseID)
   - employees.csv (EmployeeID) + projects.csv (EmployeeID, ProjectID)

3. Tasks: Create exactly 4 detailed task descriptions (each 4-6 sentences). Tasks must:
   - Be comprehensive and detailed without giving implementation hints
   - Require working with data from both CSV files if there are two
   - Mention working ONLY in the src/ folder for file operations
   - Include complex data analysis and relationships
   - CRITICAL: ALL OUTPUT MUST BE DISPLAYED ON SCREEN ONLY - NO FILE SAVING OF PARSED DATA
   - NEVER ask students to save parsed data, analysis results, or reports to files
   - ALL results, reports, and analysis must be printed to console/screen only
   - Task 1: Load and filter data with complex conditions across files - DISPLAY results on screen
   - Task 2: Analyze relationships and DISPLAY comprehensive reports on screen only
   - Task 3: Implement advanced sorting with multiple criteria and error handling - PRINT results
   - Task 4: Create complex data structures and statistical analysis - SHOW results on screen

4. Overview: 3-4 sentences explaining the comprehensive business context.

IMPORTANT: Return ONLY the JSON object. No markdown, no code blocks, no extra text. Ensure all strings are properly quoted and no trailing commas exist.`;

// Function to clean and fix JSON
function cleanAndFixJSON(jsonString) {
    try {
        // Remove any markdown formatting
        let cleaned = jsonString.replace(/```json\s*/g, '').replace(/```\s*/g, '').trim();
        
        // Remove any text before the first {
        const firstBrace = cleaned.indexOf('{');
        if (firstBrace > 0) {
            cleaned = cleaned.substring(firstBrace);
        }
        
        // Remove any text after the last }
        const lastBrace = cleaned.lastIndexOf('}');
        if (lastBrace >= 0) {
            cleaned = cleaned.substring(0, lastBrace + 1);
        }
        
        // Fix common JSON issues
        cleaned = cleaned
            // Remove trailing commas before closing brackets/braces
            .replace(/,(\s*[}\]])/g, '$1')
            // Fix missing commas between objects
            .replace(/}(\s*){/g, '},$1{')
            // Fix missing commas between array elements
            .replace(/}(\s*)\]/g, '},$1]')
            // Basic string value fixing (simplified)
            .replace(/:\s*([a-zA-Z0-9_]+)([,}\]])/g, ': "$1"$2');
        
        return cleaned;
    } catch (error) {
        console.error('Error cleaning JSON:', error.message);
        return jsonString;
    }
}

// Function to call GPT-4o API with better error handling
async function generateExamWithGPT() {
    try {
        console.log('🤖 Generating NEW exam with GPT-4o...');
        
        const response = await axios.post(OPENAI_API_URL, {
            model: 'gpt-4o',
            messages: [
                {
                    role: 'system',
                    content: 'You are a JSON generator. You must respond with ONLY valid JSON. No markdown, no explanations, no code blocks. Just pure, valid JSON that can be parsed directly.'
                },
                {
                    role: 'user',
                    content: EXAM_GENERATION_PROMPT
                }
            ],
            max_tokens: 8000,
            temperature: 0.8,
            response_format: { type: "json_object" } // Force JSON response
        }, {
            headers: {
                'Authorization': `Bearer ${OPENAI_API_KEY}`,
                'Content-Type': 'application/json'
            }
        });

        let gptResponse = response.data.choices[0].message.content.trim();
        console.log('📝 GPT-4o response received');
        console.log('Raw response length:', gptResponse.length);
        
        // Clean the JSON
        const cleanedResponse = cleanAndFixJSON(gptResponse);
        console.log('Cleaned response preview:', cleanedResponse.substring(0, 200) + '...');
        
        // Try to parse the JSON
        let examData;
        try {
            examData = JSON.parse(cleanedResponse);
        } catch (parseError) {
            console.error('First parse attempt failed:', parseError.message);
            
            // Try alternative cleaning approach
            const alternativeCleaned = gptResponse
                .replace(/```json/g, '')
                .replace(/```/g, '')
                .replace(/,(\s*[}\]])/g, '$1') // Remove trailing commas
                .trim();
            
            console.log('Trying alternative cleaning...');
            examData = JSON.parse(alternativeCleaned);
        }
        
        // Validate the structure
        if (!examData.exam || !examData.exam.domain || !examData.exam.csv_files || !examData.exam.tasks) {
            throw new Error('Invalid exam structure received from GPT');
        }
        
        console.log('✅ NEW Exam data parsed successfully');
        console.log('🎯 Domain:', examData.exam.domain);
        console.log('📊 CSV files:', examData.exam.csv_files.length);
        console.log('📋 Tasks:', examData.exam.tasks.length);
        
        return examData;
    } catch (error) {
        console.error('❌ Error generating exam with GPT-4o:', error.message);
        if (error.response) {
            console.error('API Error Status:', error.response.status);
            console.error('API Error Data:', error.response.data);
        }
        
        // Fallback to random sample exam
        console.log('🔄 Falling back to random sample exam...');
        return getRandomSampleExam();
    }
}

// Enhanced sample exams with relational data - UPDATED TO REMOVE FILE SAVING
function getRandomSampleExam() {
    const sampleExams = [
        {
            exam: {
                domain: "E-commerce Platform Management",
                csv_files: [
                    {
                        filename: "customers.csv",
                        content: [
                            { "CustomerID": "C001", "Name": "John Smith", "Email": "john.smith@email.com", "Phone": "555-0101", "City": "New York", "RegistrationDate": "2023-01-15", "Status": "Premium" },
                            { "CustomerID": "C002", "Name": "Sarah Johnson", "Email": "sarah.j@email.com", "Phone": "555-0102", "City": "Los Angeles", "RegistrationDate": "2023-02-20", "Status": "Regular" },
                            { "CustomerID": "C003", "Name": "Mike Wilson", "Email": "mike.w@email.com", "Phone": "555-0103", "City": "Chicago", "RegistrationDate": "2023-01-10", "Status": "Premium" },
                            { "CustomerID": "C004", "Name": "Emma Davis", "Email": "emma.d@email.com", "Phone": "555-0104", "City": "Houston", "RegistrationDate": "2023-03-05", "Status": "Regular" },
                            { "CustomerID": "C005", "Name": "David Brown", "Email": "david.b@email.com", "Phone": "555-0105", "City": "Phoenix", "RegistrationDate": "2023-01-25", "Status": "VIP" },
                            { "CustomerID": "C006", "Name": "Lisa Garcia", "Email": "lisa.g@email.com", "Phone": "555-0106", "City": "Philadelphia", "RegistrationDate": "2023-02-14", "Status": "Regular" },
                            { "CustomerID": "C007", "Name": "Tom Anderson", "Email": "tom.a@email.com", "Phone": "555-0107", "City": "San Antonio", "RegistrationDate": "2023-01-30", "Status": "Premium" },
                            { "CustomerID": "C008", "Name": "Anna Lee", "Email": "anna.l@email.com", "Phone": "555-0108", "City": "San Diego", "RegistrationDate": "2023-03-12", "Status": "Regular" },
                            { "CustomerID": "C009", "Name": "Chris Martin", "Email": "chris.m@email.com", "Phone": "555-0109", "City": "Dallas", "RegistrationDate": "2023-02-08", "Status": "Premium" },
                            { "CustomerID": "C010", "Name": "Sophie Chen", "Email": "sophie.c@email.com", "Phone": "555-0110", "City": "San Jose", "RegistrationDate": "2023-01-18", "Status": "VIP" },
                            { "CustomerID": "C011", "Name": "Kevin Zhang", "Email": "kevin.z@email.com", "Phone": "555-0111", "City": "Austin", "RegistrationDate": "2023-02-25", "Status": "Regular" },
                            { "CustomerID": "C012", "Name": "Maria Rodriguez", "Email": "maria.r@email.com", "Phone": "555-0112", "City": "Jacksonville", "RegistrationDate": "2023-01-22", "Status": "Premium" }
                        ]
                    },
                    {
                        filename: "orders.csv",
                        content: [
                            { "OrderID": "O001", "CustomerID": "C001", "ProductName": "Laptop Pro", "Quantity": "1", "Price": "1299.99", "OrderDate": "2023-03-15", "Status": "Delivered" },
                            { "OrderID": "O002", "CustomerID": "C001", "ProductName": "Wireless Mouse", "Quantity": "2", "Price": "49.99", "OrderDate": "2023-03-20", "Status": "Delivered" },
                            { "OrderID": "O003", "CustomerID": "C002", "ProductName": "Smartphone", "Quantity": "1", "Price": "799.99", "OrderDate": "2023-03-18", "Status": "Shipped" },
                            { "OrderID": "O004", "CustomerID": "C003", "ProductName": "Tablet", "Quantity": "1", "Price": "599.99", "OrderDate": "2023-03-22", "Status": "Delivered" },
                            { "OrderID": "O005", "CustomerID": "C003", "ProductName": "Keyboard", "Quantity": "1", "Price": "129.99", "OrderDate": "2023-03-25", "Status": "Processing" },
                            { "OrderID": "O006", "CustomerID": "C004", "ProductName": "Monitor", "Quantity": "2", "Price": "299.99", "OrderDate": "2023-03-19", "Status": "Delivered" },
                            { "OrderID": "O007", "CustomerID": "C005", "ProductName": "Gaming Chair", "Quantity": "1", "Price": "399.99", "OrderDate": "2023-03-21", "Status": "Delivered" },
                            { "OrderID": "O008", "CustomerID": "C005", "ProductName": "Desk Lamp", "Quantity": "3", "Price": "79.99", "OrderDate": "2023-03-23", "Status": "Shipped" },
                            { "OrderID": "O009", "CustomerID": "C006", "ProductName": "Headphones", "Quantity": "1", "Price": "199.99", "OrderDate": "2023-03-17", "Status": "Delivered" },
                            { "OrderID": "O010", "CustomerID": "C007", "ProductName": "Webcam", "Quantity": "1", "Price": "89.99", "OrderDate": "2023-03-24", "Status": "Processing" },
                            { "OrderID": "O011", "CustomerID": "C008", "ProductName": "Printer", "Quantity": "1", "Price": "249.99", "OrderDate": "2023-03-16", "Status": "Delivered" },
                            { "OrderID": "O012", "CustomerID": "C009", "ProductName": "External Drive", "Quantity": "2", "Price": "119.99", "OrderDate": "2023-03-26", "Status": "Shipped" },
                            { "OrderID": "O013", "CustomerID": "C010", "ProductName": "Smart Watch", "Quantity": "1", "Price": "349.99", "OrderDate": "2023-03-14", "Status": "Delivered" },
                            { "OrderID": "O014", "CustomerID": "C010", "ProductName": "Phone Case", "Quantity": "4", "Price": "29.99", "OrderDate": "2023-03-27", "Status": "Processing" },
                            { "OrderID": "O015", "CustomerID": "C011", "ProductName": "Bluetooth Speaker", "Quantity": "1", "Price": "159.99", "OrderDate": "2023-03-13", "Status": "Delivered" },
                            { "OrderID": "O016", "CustomerID": "C012", "ProductName": "Laptop Stand", "Quantity": "1", "Price": "69.99", "OrderDate": "2023-03-28", "Status": "Shipped" },
                            { "OrderID": "O017", "CustomerID": "C001", "ProductName": "Cable Organizer", "Quantity": "5", "Price": "19.99", "OrderDate": "2023-03-29", "Status": "Processing" },
                            { "OrderID": "O018", "CustomerID": "C003", "ProductName": "Power Bank", "Quantity": "2", "Price": "89.99", "OrderDate": "2023-03-30", "Status": "Delivered" },
                            { "OrderID": "O019", "CustomerID": "C005", "ProductName": "USB Hub", "Quantity": "1", "Price": "39.99", "OrderDate": "2023-03-31", "Status": "Shipped" },
                            { "OrderID": "O020", "CustomerID": "C007", "ProductName": "Screen Protector", "Quantity": "3", "Price": "24.99", "OrderDate": "2023-04-01", "Status": "Processing" }
                        ]
                    }
                ],
                tasks: [
                    { 
                        "id": 1, 
                        "description": "Develop a comprehensive customer analysis system that loads data from both customers.csv and orders.csv files to identify high-value customers and their purchasing patterns. Your analysis should filter customers who have Premium or VIP status and have placed orders totaling more than $500 in value across all their transactions. For each qualifying customer, calculate their total order value, average order amount, number of orders placed, and most frequently purchased product category. Display this information in a detailed tabular format on the screen showing customer details alongside their comprehensive purchasing statistics. The system should also identify which customers have the highest individual order values and print any customers who might be considered for loyalty program upgrades based on their spending patterns." 
                    },
                    { 
                        "id": 2, 
                        "description": "Create an advanced order fulfillment and customer relationship management analysis that examines the correlation between customer status levels and their order behaviors across both data files. Generate a comprehensive business intelligence report that analyzes order distribution patterns, identifies customers with multiple orders, calculates average order processing times by customer tier, and determines which customer segments generate the most revenue per transaction. Your analysis should include statistical breakdowns of order statuses by customer type, identification of repeat customers and their loyalty metrics, and recommendations for inventory management based on popular product combinations. Display this detailed analytical report on the screen only, ensuring the output includes executive summary sections, detailed customer profiles, and actionable business insights for management decision-making." 
                    },
                    { 
                        "id": 3, 
                        "description": "Implement a sophisticated multi-dimensional sorting and data validation system that organizes the combined customer and order data using complex hierarchical criteria. Your sorting algorithm should first arrange customers by their status level (VIP, Premium, Regular), then within each status group by their total order value in descending order, and finally by registration date for customers with similar spending patterns. The system must include comprehensive error handling for data inconsistencies such as missing customer records for existing orders, invalid date formats, negative quantities or prices, and duplicate order IDs. Additionally, implement data integrity checks that verify all orders have corresponding customer records and flag any anomalies in the customer-order relationships. Print the sorted results on the screen with clear indicators of any data quality issues discovered during processing, and display detailed error logs for any problematic records encountered." 
                    },
                    { 
                        "id": 4, 
                        "description": "Design and implement a comprehensive e-commerce analytics dashboard using advanced Java collections that creates multiple interconnected data structures for deep business analysis. Construct a Map<String, List<Customer>> that groups customers by city for geographic analysis, a Map<String, List<Order>> that organizes orders by status for operational insights, and a Map<String, CustomerOrderSummary> that links each customer to their complete order history and calculated metrics. Your dashboard should perform complex statistical calculations including customer lifetime value analysis, geographic revenue distribution, product popularity rankings, seasonal ordering patterns, and customer retention rates. Display detailed analytics reports on the screen showing top-performing cities by revenue, most valuable customer segments, order fulfillment efficiency metrics, and predictive insights for inventory planning. The system should also identify cross-selling opportunities by analyzing customer purchase patterns and print recommendations for targeted marketing campaigns based on customer behavior analysis and geographic trends." 
                    }
                ],
                overview: "You are developing a comprehensive e-commerce platform management system for a growing online retail company that needs to analyze customer behavior, order patterns, and business performance across multiple dimensions. The system must provide detailed insights into customer segmentation, order fulfillment efficiency, revenue optimization, and strategic business intelligence to support data-driven decision making. This platform serves as the central analytics hub for understanding customer relationships, optimizing inventory management, and identifying growth opportunities in the competitive e-commerce marketplace."
            }
        }
    ];
    
    const randomIndex = Math.floor(Math.random() * sampleExams.length);
    const selectedExam = sampleExams[randomIndex];
    console.log(`🎲 Using random sample exam ${randomIndex + 1}: ${selectedExam.exam.domain}`);
    return selectedExam;
}

// Rest of the code remains the same...
function convertExamToFiles(examData) {
    const files = [];
    
    examData.exam.csv_files.forEach(csvFile => {
        if (csvFile.content && csvFile.content.length > 0) {
            const headers = Object.keys(csvFile.content[0]);
            let csvContent = headers.join(',') + '\n';
            csvFile.content.forEach(row => {
                const values = headers.map(header => row[header] || '');
                csvContent += values.join(',') + '\n';
            });
            
            // Add CSV file to data directory
            files.push({
                name: csvFile.filename,
                path: `data/${csvFile.filename}`,
                content: csvContent.trim(),
                isDirectory: false
            });
            
            // ALSO add CSV file to src directory for easier access
            files.push({
                name: csvFile.filename,
                path: `src/${csvFile.filename}`,
                content: csvContent.trim(),
                isDirectory: false
            });
        }
    });
    
    const mainJava = `import java.io.*;
import java.util.*;
import java.util.stream.Collectors;

/**
 * ${examData.exam.domain}
 * ${examData.exam.overview}
 */
public class Main {
    public static void main(String[] args) {
        System.out.println("Hello World!");
    }
}`;

    files.push({
        name: 'Main.java',
        path: 'src/Main.java',
        content: mainJava,
        isDirectory: false
    });
    
    return files;
}

function runCommand(cmd, args, options = {}) {
    try {
        console.log(`Running command: ${cmd} ${args.join(' ')}`);
        const result = spawnSync(cmd, args, { 
            encoding: 'utf8',
            timeout: 30000,
            ...options 
        });
        const stdout = result.stdout || '';
        const stderr = result.stderr || '';
        console.log(`Command result - Code: ${result.status}, Stdout: ${stdout.substring(0, 200)}, Stderr: ${stderr.substring(0, 200)}`);
        return { stdout, stderr, code: result.status ?? 0 };
    } catch (err) {
        console.log(`Command error: ${err.message}`);
        return { stdout: '', stderr: err.message, code: -1 };
    }
}

function writeFilesToWorkspace(files) {
    files.forEach(file => {
        if (!file.isDirectory) {
            const fullPath = path.join(WORKSPACE_DIR, file.path);
            const dir = path.dirname(fullPath);
            fs.mkdirSync(dir, { recursive: true });
            fs.writeFileSync(fullPath, file.content, 'utf8');
        }
    });
}

async function compileAndRun(mainClassName = 'Main') {
    const srcDir = path.join(WORKSPACE_DIR, 'src');
    const workspaceDir = WORKSPACE_DIR;
    
    console.log(`Starting Java compilation and execution for class: ${mainClassName}`);
    console.log('Source directory:', srcDir);
    console.log('Working directory:', workspaceDir);
    
    try {
        const javaCheck = runCommand('java', ['-version']);
        const javacCheck = runCommand('javac', ['-version']);
        
        if (javaCheck.code === 0 && javacCheck.code === 0) {
            console.log('Using local/container Java for compilation');
            
const javaFiles = fs.readdirSync(srcDir)
  .filter(f => f.endsWith('.java'));
if (javaFiles.length === 0) {
   return { output: '', error: 'No .java files found in src' };
 }
const compileResult = runCommand(
  'javac',
  ['-cp', '.', ...javaFiles],
   { cwd: srcDir }
 );
            if (compileResult.code !== 0) {
                return {
                    output: '',
                    error: `Compilation failed:\n${compileResult.stderr}`
                };
            }
            
            // Run from workspace root so data/ directory is accessible
            const runResult = runCommand('java', ['-cp', 'src', mainClassName], {
                cwd: workspaceDir
            });
            
            return {
                output: runResult.stdout,
                error: runResult.stderr
            };
        } else {
            return {
                output: '',
                error: 'Java is not available. Please install Java JDK 17+ or use Docker.'
            };
        }
        
    } catch (error) {
        console.error('Error in compileAndRun:', error);
        return {
            output: '',
            error: `Execution failed: ${error.message}`
        };
    }
}

// SECURITY ROUTES

// Admin login endpoint
app.post('/admin/login', rateLimitAdmin, (req, res) => {
    const { password } = req.body;
    
    if (!password) {
        return res.status(400).json({ error: 'Password required' });
    }
    
    // Simple password check (in production, use proper hashing)
    const expectedPassword = process.env.ADMIN_PASSWORD || 'admin123!@#';
    
    if (password === expectedPassword) {
        // Generate session token
        const sessionToken = crypto.randomBytes(32).toString('hex');
        const expiresIn = 24 * 60 * 60 * 1000; // 24 hours
        
        sessionStore.set(sessionToken, {
            created: Date.now(),
            expires: Date.now() + expiresIn
        });
        
        console.log(`🔐 Admin login successful from ${req.ip}`);
        
        res.json({
            success: true,
            sessionToken,
            expiresIn,
            message: 'Authentication successful'
        });
    } else {
        console.warn(`🚫 Failed admin login attempt from ${req.ip}`);
        res.status(401).json({ error: 'Invalid credentials' });
    }
});

// Admin logout endpoint
app.post('/admin/logout', authenticateAdmin, (req, res) => {
    const sessionToken = req.headers['x-session-token'];
    
    if (sessionToken && sessionStore.has(sessionToken)) {
        sessionStore.delete(sessionToken);
    }
    
    res.json({ success: true, message: 'Logged out successfully' });
});

// Secure endpoint to retrieve exam JSON
app.get('/admin/exam/:examId', authenticateAdmin, (req, res) => {
    try {
        const { examId } = req.params;
        const { format } = req.query;
        
        if (!examStore.has(examId)) {
            return res.status(404).json({ 
                error: 'Exam not found',
                examId,
                availableExams: Array.from(examStore.keys())
            });
        }
        
        const examData = examStore.get(examId);
        
        // Log access
        console.log(`📋 Admin retrieved exam ${examId} from ${req.ip}`);
        
        if (format === 'download') {
            // Send as downloadable file
            res.setHeader('Content-Disposition', `attachment; filename="exam-${examId}.json"`);
            res.setHeader('Content-Type', 'application/json');
            return res.send(JSON.stringify(examData, null, 2));
        }
        
        // Send as JSON response
        res.json({
            success: true,
            examId,
            timestamp: examData.timestamp,
            exam: examData.exam,
            metadata: {
                domain: examData.exam.domain,
                csvFiles: examData.exam.csv_files.length,
                tasks: examData.exam.tasks.length,
                generated: examData.timestamp
            }
        });
        
    } catch (error) {
        console.error('Error retrieving exam:', error);
        res.status(500).json({ error: 'Failed to retrieve exam data' });
    }
});

// List all available exams
app.get('/admin/exams', authenticateAdmin, (req, res) => {
    try {
        const exams = Array.from(examStore.entries()).map(([id, data]) => ({
            examId: id,
            domain: data.exam.domain,
            csvFiles: data.exam.csv_files.length,
            tasks: data.exam.tasks.length,
            timestamp: data.timestamp,
            age: Date.now() - new Date(data.timestamp).getTime()
        }));
        
        res.json({
            success: true,
            count: exams.length,
            exams: exams.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
        });
        
    } catch (error) {
        console.error('Error listing exams:', error);
        res.status(500).json({ error: 'Failed to list exams' });
    }
});

// Get exam tasks only (without CSV data)
app.get('/admin/exam/:examId/tasks', authenticateAdmin, (req, res) => {
    try {
        const { examId } = req.params;
        
        if (!examStore.has(examId)) {
            return res.status(404).json({ error: 'Exam not found' });
        }
        
        const examData = examStore.get(examId);
        
        res.json({
            success: true,
            examId,
            domain: examData.exam.domain,
            overview: examData.exam.overview,
            tasks: examData.exam.tasks,
            timestamp: examData.timestamp
        });
        
    } catch (error) {
        console.error('Error retrieving exam tasks:', error);
        res.status(500).json({ error: 'Failed to retrieve exam tasks' });
    }
});

// EXISTING ROUTES (updated to store exams)

// Routes
app.post('/exam', async (req, res) => {
    try {
        console.log('🚀 Starting FRESH exam generation...');
        console.log('🔄 No caching - generating completely new exam');
        
        const examData = await generateExamWithGPT();
        const files = convertExamToFiles(examData);
        
        writeFilesToWorkspace(files);
        console.log('📁 NEW exam files written to workspace');
        
        // Store exam with unique ID for admin retrieval
        const examId = crypto.randomBytes(16).toString('hex');
        examStore.set(examId, {
            exam: examData.exam,
            timestamp: new Date().toISOString(),
            files: files
        });
        
        // Clean up old exams (keep last 50)
        if (examStore.size > 50) {
            const oldestKey = examStore.keys().next().value;
            examStore.delete(oldestKey);
        }
        
        console.log(`💾 Exam stored with ID: ${examId}`);
        
        res.json({
            exam: examData.exam,
            files: files,
            examId: examId // Include exam ID in response for admin reference
        });
    } catch (error) {
        console.error('❌ Error generating exam:', error);
        res.status(500).json({ error: 'Failed to generate exam' });
    }
});

app.post('/run', async (req, res) => {
    const sid = getSID(req);
const now = Date.now();
const last = runRate.get(sid) || 0;
if (now - last < 2000) {
  return res.status(429).json({ error: 'Too many runs. Wait a bit.' });
}
runRate.set(sid, now);
    try {
        const { files, mainFile } = req.body;
        
        if (!files) {
            return res.status(400).json({ error: 'No files provided' });
        }
        
        console.log('Received files for execution:', files.map(f => f.path));
        console.log('Main file specified:', mainFile);
        
        writeFilesToWorkspace(files);
        
        let fileToRun = 'Main';
        
        if (mainFile) {
            const mainFilePath = mainFile.replace(/^src\//, '').replace(/\.java$/, '');
            fileToRun = mainFilePath;
            console.log(`Running specified file: ${fileToRun}`);
        } else {
            const javaFiles = files.filter(f => f.path.endsWith('.java') && !f.isDirectory);
            
            for (const file of javaFiles) {
                console.log(`Checking file: ${file.path}`);
                const hasMainMethod = file.content.includes('public static void main(String');
                console.log(`  - Has main method: ${hasMainMethod}`);
                
                if (hasMainMethod) {
                    fileToRun = file.path.replace(/^src\//, '').replace(/\.java$/, '');
                    console.log(`✅ Auto-detected main file: ${fileToRun}`);
                    break;
                }
            }
        }
        
        const result = await compileAndRun(fileToRun);
        console.log('Execution result:', result);
        res.json(result);
    } catch (error) {
        console.error('Error running code:', error);
        res.status(500).json({ error: 'Failed to run code' });
    }
});

app.post('/reset', (req, res) => {
    try {
        console.log('⚠️  Reset not supported - generate new exam instead');
        res.json({ 
            success: false, 
            message: 'Reset not supported. Refresh page to generate new exam.' 
        });
    } catch (error) {
        console.error('Error resetting exam:', error);
        res.status(500).json({ error: 'Failed to reset exam' });
    }
});

app.post('/submit', (req, res) => {
    try {
        const { files } = req.body;
        
        if (!files) {
            return res.status(400).json({ error: 'No files provided' });
        }
        
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const submissionDir = path.join(SUBMISSIONS_DIR, `submission-${timestamp}`);
        
        fs.mkdirSync(submissionDir, { recursive: true });
        
        files.forEach(file => {
            if (!file.isDirectory) {
                const filePath = path.join(submissionDir, file.name);
                fs.writeFileSync(filePath, file.content, 'utf8');
            }
        });
        
        console.log(`Exam submitted and saved to: ${submissionDir}`);
        res.json({ success: true, submissionId: timestamp });
    } catch (error) {
        console.error('Error submitting exam:', error);
        res.status(500).json({ error: 'Failed to submit exam' });
    }
});

// Evaluate submission against exam JSON and return ONLY the evaluation JSON
async function evaluateHeuristics(files, exam){
        const javaFiles = (files || []).filter(f => !f.isDirectory && /\.java$/i.test(f.name || f.path || ''));
        const allCode = javaFiles.map(f => f.content || '').join('\n');

        const csvFiles = (exam.csv_files || []).map(cf => typeof cf === 'string' ? cf : cf.filename).filter(Boolean);
        const domain = exam.domain || 'Unknown Domain';
        const overview = exam.overview || '';

        const hasMain = /public\s+static\s+void\s+main\s*\(/.test(allCode);
        const hasPrint = /System\.out\.(println|printf)/.test(allCode);
        const usesFileRead = /(BufferedReader|FileReader|Files\s*\.|Paths\s*\.|Scanner\s*\(|CSVReader)/.test(allCode);
        const mentionsCsvNames = csvFiles.some(n => allCode.includes(n));
        const usesStreams = /\.stream\s*\(|Collectors\./.test(allCode);
        const usesFilter = /\.filter\s*\(|if\s*\(.*(&&|\|\|).*/.test(allCode);
        const usesMap = /\bMap<|HashMap<|Collectors\.groupingBy/.test(allCode);
        const usesSort = /\.sorted\s*\(|Collections\.sort|Comparator\./.test(allCode);
        const usesTryCatch = /try\s*\{[\s\S]*?\}\s*catch\s*\(/.test(allCode);
        const usesNumberParsing = /(Integer\.parseInt|Double\.parseDouble|Float\.parseFloat|Long\.parseLong)/.test(allCode);

        // Helper to compute percentage and status
        function pctToStatus(p) {
            if (p >= 50) return { status: 'Correctly Solved', percentage: p };
            if (p >= 30) return { status: 'Partially Correct', percentage: p };
            return { status: 'Incorrectly Solved', percentage: p };
        }

        // Task 1: load + filter + display
        let t1Score = 0;
        if (usesFileRead || mentionsCsvNames) t1Score += 35;
        if (usesFilter || usesStreams) t1Score += 35;
        if (hasPrint) t1Score += 30;
        t1Score = Math.min(100, t1Score);

        // Task 2: relationships + analytics + reporting
        let t2Score = 0;
        const usesBothCSVs = csvFiles.length >= 2 && mentionsCsvNames;
        if (usesBothCSVs) t2Score += 30;
        if (usesMap || /join|merge|link|key/i.test(allCode)) t2Score += 30;
        if (usesStreams || /average|sum|count|revenue|distribution|segment|loyal/i.test(allCode)) t2Score += 20;
        if (hasPrint) t2Score += 20;
        t2Score = Math.min(100, t2Score);

        // Task 3: sorting + validation + error handling
        let t3Score = 0;
        if (usesSort) t3Score += 40;
        if (usesTryCatch) t3Score += 30;
        if (usesNumberParsing) t3Score += 30;
        t3Score = Math.min(100, t3Score);

        // Task 4: data structures + statistics + dashboards
        let t4Score = 0;
        const usesMultipleMaps = (allCode.match(/\bMap</g) || []).length >= 2 || /Collectors\.groupingBy/.test(allCode);
        if (usesMultipleMaps) t4Score += 40;
        if (/lifetime|retention|popularity|ranking|distribution|pattern|predict/i.test(allCode) || usesStreams) t4Score += 30;
        if (hasPrint) t4Score += 30;
        t4Score = Math.min(100, t4Score);

        // Special rules
        // If Task 3 or Task 4 ≥50%, then Task 2 is automatically ≥50%.
        if (t3Score >= 50 || t4Score >= 50) {
            t2Score = Math.max(t2Score, 50);
        }

        // If Task 2 has no System.out.println AND not compensated by Task 4 printing, enforce grade 4 scenario
        const t2PrintAbsentHardFail = !hasPrint && t4Score < 50;

        const t1 = pctToStatus(t1Score);
        const t2 = pctToStatus(t2Score);
        const t3 = pctToStatus(t3Score);
        const t4 = pctToStatus(t4Score);

        const pass = [t1, t2, t3, t4].map(t => t.percentage >= 50);
        const partial = [t1, t2, t3, t4].map(t => t.percentage >= 30 && t.percentage < 50);
        const passCount = pass.filter(Boolean).length;
        const partialCount = partial.filter(Boolean).length;

        // Compute final grade
        let finalGrade;
        if (!hasMain) {
            finalGrade = 4; // No functioning main class
        } else if (passCount === 0) {
            finalGrade = 4; // None ≥50%
        } else if (t2PrintAbsentHardFail) {
            finalGrade = 4; // Task 2 println missing and not compensated by Task 4
        } else {
            if (passCount === 4) finalGrade = 10;
            else if (passCount === 3) finalGrade = partialCount >= 1 ? 9 : 8;
            else if (passCount === 2) finalGrade = partialCount >= 1 ? 7 : 6;
            else if (passCount === 1) {
                // Fall back to Task 1 mapping
                if (t1.percentage < 10) finalGrade = 2;
                else if (t1.percentage < 30) finalGrade = 3;
                else if (t1.percentage < 50) finalGrade = 4;
                else finalGrade = 5;
                // If both T1 and T2 are ≥50 explicitly, ensure grade 6
                if (t1.percentage >= 50 && t2.percentage >= 50) finalGrade = Math.max(finalGrade, 6);
            } else {
                finalGrade = 4; // Default safety
            }
        }

        // Bound grade
        finalGrade = Math.max(2, Math.min(10, finalGrade));

                const nowIso = new Date().toISOString();
                return {
            exam: {
                domain,
                csv_files: csvFiles,
                overview
            },
            evaluation: {
                task1: { percentage: t1.percentage, status: t1.status, explanation: 'Heuristic: load/filter/print evidence.' },
                task2: { percentage: t2.percentage, status: t2.status, explanation: 'Heuristic: relationships/analytics/reporting evidence.' },
                task3: { percentage: t3.percentage, status: t3.status, explanation: 'Heuristic: sort/error handling/data validation evidence.' },
                task4: { percentage: t4.percentage, status: t4.status, explanation: 'Heuristic: data structures/statistics/reporting evidence.' }
            },
            final_grade: finalGrade,
            timestamp: nowIso
                };
}

async function evaluateWithGPT(files, exam){
    const prompt = `You are an automated grader. Return ONLY valid JSON with this exact structure and snake_case keys:\n{\n  \"exam\": {\"domain\": \"string\", \"csv_files\": [\"string\"], \"overview\": \"string\"},\n  \"evaluation\": {\n    \"task1\": {\"percentage\": number, \"status\": \"Correctly Solved|Partially Correct|Incorrectly Solved\", \"explanation\": \"string\"},\n    \"task2\": {\"percentage\": number, \"status\": \"Correctly Solved|Partially Correct|Incorrectly Solved\", \"explanation\": \"string\"},\n    \"task3\": {\"percentage\": number, \"status\": \"Correctly Solved|Partially Correct|Incorrectly Solved\", \"explanation\": \"string\"},\n    \"task4\": {\"percentage\": number, \"status\": \"Correctly Solved|Partially Correct|Incorrectly Solved\", \"explanation\": \"string\"}\n  },\n  \"final_grade\": number,\n  \"timestamp\": \"ISO-8601 string\"\n}\n\nGrading rules: Prefer higher grades only if multiple tasks ≥50%. Apply constraints similar to: if no main method then final_grade=4; if none ≥50% then 4; if 3-4 pass then 9-10, if 2 pass then 6-7 depending on partials; if 1 pass then map Task 1 percent to 2-5 with min 6 if both T1 and T2 ≥50. Use only the provided code and exam context. Strictly output only JSON.`;

    const javaFiles = (files || []).filter(f => !f.isDirectory && /\.java$/i.test(f.name || f.path || ''));
    const codeBundle = javaFiles.map(f => `// ${f.path || f.name}\n${f.content}`).join('\n\n');
    const csvFiles = (exam.csv_files || []).map(cf => typeof cf === 'string' ? cf : cf.filename).filter(Boolean);
    const payload = {
        model: 'gpt-4o',
        messages: [
            { role: 'system', content: 'You are a JSON-only auto-grader. Respond with only the JSON object.' },
            { role: 'user', content: `${prompt}\n\nExam domain: ${exam.domain}\nOverview: ${exam.overview}\nCSV files: ${csvFiles.join(', ')}\n\nCode:\n${codeBundle}` }
        ],
        temperature: 0.2,
        max_tokens: 800,
        response_format: { type: 'json_object' }
    };

    if (!OPENAI_API_KEY) throw new Error('OPENAI_API_KEY missing');

    const response = await axios.post(OPENAI_API_URL, payload, {
        headers: {
            'Authorization': `Bearer ${OPENAI_API_KEY}`,
            'Content-Type': 'application/json'
        }
    });
    const content = (response.data.choices?.[0]?.message?.content || '').trim();
    const json = JSON.parse(content);
    if (json && json.exam) {
        const files = json.exam.csv_files || [];
        json.exam.csv_files = files.map(x => typeof x === 'string' ? x : (x && x.filename) ? x.filename : String(x));
    }
    return json;
}

app.post('/evaluate', async (req, res) => {
    try{
        const { files, exam } = req.body || {};
        if (!files || !exam) return res.status(400).json({ error: 'Missing files or exam' });

        let result;
        try{
            result = await evaluateWithGPT(files, exam);
        } catch(err){
            console.warn('⚠️ OpenAI evaluation failed, falling back to heuristics:', err?.message || err);
            result = await evaluateHeuristics(files, exam);
        }
        res.setHeader('Content-Type', 'application/json');
        return res.send(JSON.stringify(result));
    } catch(error){
        console.error('Error evaluating submission:', error);
        return res.status(500).json({ error: 'Failed to evaluate submission' });
    }
});

app.get('/health', (req, res) => {
    res.json({ status: 'OK', timestamp: new Date().toISOString() });
});

// Admin health check
app.get('/admin/health', authenticateAdmin, (req, res) => {
    res.json({ 
        status: 'OK', 
        timestamp: new Date().toISOString(),
        examCount: examStore.size,
        sessionCount: sessionStore.size
    });
});

function getSessionId(req){
  return req.query.sid || req.headers['x-session-token'] || req.ip;
}

app.post('/telemetry', (req, res) => {
  try {
    const sid = getSessionId(req);
    console.log('[telemetry] from sid=', sid, 'ip=', req.ip, 'len=', (req.body||'').length);
    const now = Date.now();
    let evt = {};
    try { evt = JSON.parse(req.body || '{}'); } catch {
      evt = Object.fromEntries(new URLSearchParams(req.body || ''));
      if (!evt.ts) evt.ts = now;
    }
    const rec = telemetryStore.get(sid) || { score: 0, events: [] };
    rec.events.push({ ...evt, at: now });
    if (evt.evt === 'blur' || evt.evt === 'hidden') rec.score += 10;
    if (evt.evt === 'resize') rec.score += 5;
    telemetryStore.set(sid, rec);
  } catch { /* ignore */ }
  res.status(204).end();
});

app.get('/admin/telemetry/:sid', authenticateAdmin, (req, res) => {
  const sid = req.params.sid;
  res.json(telemetryStore.get(sid) || { score: 0, events: [] });
});


app.listen(PORT, () => {
    console.log(`🚀 AI Exam IDE Server running on port ${PORT}`);
    console.log(`📁 Workspace directory: ${WORKSPACE_DIR}`);
    console.log(`📤 Submissions directory: ${SUBMISSIONS_DIR}`);
    console.log(`🔄 Caching DISABLED - Fresh exams every time`);
    console.log(`🔐 Admin secret key: ${ADMIN_SECRET_KEY.substring(0, 8)}...`);
    console.log(`🔑 Admin password: ${process.env.ADMIN_PASSWORD || 'admin123!@#'}`);
    
    const javaCheck = runCommand('java', ['-version']);
    const javacCheck = runCommand('javac', ['-version']);
    
    if (javaCheck.code === 0 && javacCheck.code === 0) {
        console.log('✅ Java is available for code execution');
    } else {
        console.log('❌ Java not found - please install Java JDK 17+');
    }
    
    if (OPENAI_API_KEY && OPENAI_API_KEY !== 'your-openai-api-key-here') {
        console.log('✅ OpenAI API key configured');
    } else {
        console.log('⚠️  OpenAI API key not configured - using fallback exams');
    }
    
    console.log('\n📋 Admin API Endpoints:');
    console.log('  POST /admin/login - Get session token');
    console.log('  GET /admin/exams - List all exams');
    console.log('  GET /admin/exam/:examId - Get specific exam');
    console.log('  GET /admin/exam/:examId/tasks - Get exam tasks only');
    console.log('  GET /admin/exam/:examId?format=download - Download exam JSON');
});