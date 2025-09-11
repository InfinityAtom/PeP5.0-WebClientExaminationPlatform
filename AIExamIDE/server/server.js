const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { spawn, spawnSync } = require('child_process');

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(cors());
app.use(express.json({ limit: '10mb' }));

// Directories
const WORKSPACE_DIR = path.join(__dirname, 'workspace');
const SUBMISSIONS_DIR = path.join(__dirname, 'submissions');

// Ensure directories exist
fs.mkdirSync(WORKSPACE_DIR, { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'src'), { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'data'), { recursive: true });
fs.mkdirSync(SUBMISSIONS_DIR, { recursive: true });

// Utility: Run a shell command and return stdout, stderr, and exit code
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

// In-memory cache of current exam and initial files
let currentExam = null;
let initialFiles = [];

// Sample exam data
const SAMPLE_EXAM = {
    domain: "Retail Inventory Management",
    overview: "You are tasked with analyzing a retail store's inventory data. The exam will test your ability to filter data, generate output, handle sorting with threads, and use interfaces to group data.",
    tasks: [
        {
            title: "Task 1: Load and Display Products",
            description: "Load product data from CSV and display all products with their details."
        },
        {
            title: "Task 2: Filter Products by Category",
            description: "Filter products by category and display only Electronics items."
        },
        {
            title: "Task 3: Sort Products by Price",
            description: "Sort all products by price in ascending order and display them."
        },
        {
            title: "Task 4: Calculate Statistics",
            description: "Calculate and display total inventory value and average price per category."
        }
    ]
};

// Generate sample files
function generateSampleFiles() {
    const files = [];
    
    // Generate CSV data
    const csvContent = `ID,Name,Category,Stock,Price
1,Laptop,Electronics,4,1200.50
2,Smartphone,Electronics,10,799.99
3,Jeans,Clothing,3,49.99
4,T-Shirt,Clothing,25,19.99
5,Blender,Home Appliances,4,89.50
6,Coffee Maker,Home Appliances,2,129.99
7,Action Figure,Toys,30,15.99
8,Board Game,Toys,12,29.99`;

    files.push({
        name: 'products.csv',
        path: 'data/products.csv',
        content: csvContent,
        isDirectory: false
    });

    // Generate Product.java
    const productJava = `import java.io.*;
import java.util.*;

public class Product {
    private int id;
    private String name;
    private String category;
    private int stock;
    private double price;
    
    public Product(int id, String name, String category, int stock, double price) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.stock = stock;
        this.price = price;
    }
    
    // Getters
    public int getId() { return id; }
    public String getName() { return name; }
    public String getCategory() { return category; }
    public int getStock() { return stock; }
    public double getPrice() { return price; }
    
    @Override
    public String toString() {
        return String.format("Product{id=%d, name='%s', category='%s', stock=%d, price=%.2f}", 
                           id, name, category, stock, price);
    }
    
    public static List<Product> loadFromCSV(String filename) {
        List<Product> products = new ArrayList<>();
        try (BufferedReader br = new BufferedReader(new FileReader(filename))) {
            String line = br.readLine(); // Skip header
            while ((line = br.readLine()) != null) {
                String[] parts = line.split(",");
                if (parts.length >= 5) {
                    int id = Integer.parseInt(parts[0].trim());
                    String name = parts[1].trim();
                    String category = parts[2].trim();
                    int stock = Integer.parseInt(parts[3].trim());
                    double price = Double.parseDouble(parts[4].trim());
                    products.add(new Product(id, name, category, stock, price));
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
        return products;
    }
}`;

    files.push({
        name: 'Product.java',
        path: 'src/Product.java',
        content: productJava,
        isDirectory: false
    });

    // Generate Main.java with working example
    const mainJava = `import java.util.*;
import java.util.stream.Collectors;

public class Main {
    public static void main(String[] args) {
        System.out.println("=== Retail Inventory Management System ===\\n");
        
        // Load products from CSV
        List<Product> products = Product.loadFromCSV("../data/products.csv");
        
        // Task 1 - Display all products
        System.out.println("Task 1: All Products");
        for (Product product : products) {
            System.out.println(product);
        }
        
        System.out.println("\\n" + "=".repeat(50) + "\\n");
        
        // Task 2 - Filter products by Electronics category
        System.out.println("Task 2: Electronics Products");
        products.stream()
            .filter(p -> "Electronics".equals(p.getCategory()))
            .forEach(System.out::println);
        
        System.out.println("\\n" + "=".repeat(50) + "\\n");
        
        // Task 3 - Sort products by price (ascending)
        System.out.println("Task 3: Products Sorted by Price");
        products.stream()
            .sorted((p1, p2) -> Double.compare(p1.getPrice(), p2.getPrice()))
            .forEach(System.out::println);
        
        System.out.println("\\n" + "=".repeat(50) + "\\n");
        
        // Task 4 - Calculate statistics
        System.out.println("Task 4: Statistics");
        double totalValue = products.stream()
            .mapToDouble(p -> p.getPrice() * p.getStock())
            .sum();
        System.out.println("Total Inventory Value: $" + String.format("%.2f", totalValue));
        
        Map<String, Double> avgPriceByCategory = products.stream()
            .collect(Collectors.groupingBy(
                Product::getCategory,
                Collectors.averagingDouble(Product::getPrice)
            ));
        
        System.out.println("Average Price by Category:");
        avgPriceByCategory.forEach((category, avgPrice) -> 
            System.out.println("  " + category + ": $" + String.format("%.2f", avgPrice))
        );
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

// Write files to workspace
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

// Compile and run Java code using local Java or container Java
async function compileAndRun(mainClassName = 'Main') {
    const srcDir = path.join(WORKSPACE_DIR, 'src');
    
    console.log(`Starting Java compilation and execution for class: ${mainClassName}`);
    console.log('Source directory:', srcDir);
    
    try {
        // Check if we have Java available
        const javaCheck = runCommand('java', ['-version']);
        const javacCheck = runCommand('javac', ['-version']);
        
        if (javaCheck.code === 0 && javacCheck.code === 0) {
            console.log('Using local/container Java for compilation');
            
            // Compile all Java files
            const compileResult = runCommand('javac', ['-cp', '.', '*.java'], {
                cwd: srcDir
            });
            
            if (compileResult.code !== 0) {
                return {
                    output: '',
                    error: `Compilation failed:\n${compileResult.stderr}`
                };
            }
            
            // Run the specified main class
            const runResult = runCommand('java', ['-cp', '.', mainClassName], {
                cwd: srcDir
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

// Routes
app.post('/exam', async (req, res) => {
    try {
        console.log('Generating exam...');
        currentExam = SAMPLE_EXAM;
        initialFiles = generateSampleFiles();
        
        // Write initial files to workspace
        writeFilesToWorkspace(initialFiles);
        console.log('Exam files written to workspace');
        
        res.json({
            exam: currentExam,
            files: initialFiles
        });
    } catch (error) {
        console.error('Error generating exam:', error);
        res.status(500).json({ error: 'Failed to generate exam' });
    }
});

app.post('/run', async (req, res) => {
    try {
        const { files, mainFile } = req.body;
        
        if (!files) {
            return res.status(400).json({ error: 'No files provided' });
        }
        
        console.log('Received files for execution:', files.map(f => f.path));
        console.log('Main file specified:', mainFile);
        
        // Write current files to workspace
        writeFilesToWorkspace(files);
        
        // Determine which file to run
        let fileToRun = 'Main'; // Default class name
        
        if (mainFile) {
            // Extract class name from the specified main file
            const mainFilePath = mainFile.replace(/^src\//, '').replace(/\.java$/, '');
            fileToRun = mainFilePath;
            console.log(`Running specified file: ${fileToRun}`);
        } else {
            // Auto-detect: look for files with main method - SIMPLIFIED
            const javaFiles = files.filter(f => f.path.endsWith('.java') && !f.isDirectory);
            
            for (const file of javaFiles) {
                console.log(`Checking file: ${file.path}`);
                
                // Simple string-based detection
                const hasMainMethod = file.content.includes('public static void main(String');
                
                console.log(`  - Has main method: ${hasMainMethod}`);
                
                if (hasMainMethod) {
                    fileToRun = file.path.replace(/^src\//, '').replace(/\.java$/, '');
                    console.log(`✅ Auto-detected main file: ${fileToRun}`);
                    break;
                }
            }
        }
        
        // Compile and run with the determined main class
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
        if (initialFiles.length > 0) {
            writeFilesToWorkspace(initialFiles);
        }
        res.json({ success: true });
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

app.get('/health', (req, res) => {
    res.json({ status: 'OK', timestamp: new Date().toISOString() });
});

app.listen(PORT, () => {
    console.log(`🚀 AI Exam IDE Server running on port ${PORT}`);
    console.log(`📁 Workspace directory: ${WORKSPACE_DIR}`);
    console.log(`📤 Submissions directory: ${SUBMISSIONS_DIR}`);
    
    // Check Java availability
    const javaCheck = runCommand('java', ['-version']);
    const javacCheck = runCommand('javac', ['-version']);
    
    if (javaCheck.code === 0 && javacCheck.code === 0) {
        console.log('✅ Java is available for code execution');
    } else {
        console.log('❌ Java not found - please install Java JDK 17+');
    }
});