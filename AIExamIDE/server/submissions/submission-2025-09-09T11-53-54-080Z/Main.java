import java.util.*;
import java.util.stream.Collectors;

public class Main {
    public static void main(String[] args) {
        System.out.println("=== Retail Inventory Management System ===\n");
        
        // Load products from CSV
        List<Product> products = Product.loadFromCSV("data/products.csv");
        
        // TODO: Task 1 - Display all products
        System.out.println("Task 1: All Products");
        // Implement code to display all products
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
        // TODO: Task 2 - Filter products by Electronics category
        System.out.println("Task 2: Electronics Products");
        // Implement code to filter and display only Electronics products
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
        // TODO: Task 3 - Sort products by price (ascending)
        System.out.println("Task 3: Products Sorted by Price");
        // Implement code to sort products by price and display them
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
        // TODO: Task 4 - Calculate statistics
        System.out.println("Task 4: Statistics");
        // Implement code to calculate total inventory value and average price per category
    }
}