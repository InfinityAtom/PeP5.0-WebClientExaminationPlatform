import java.util.*;
import java.util.stream.Collectors;

public class Main_copy {
    public static void main(String[] args) {
        System.out.println("=== Retail Inventory Management System ===\n");
        
        // Load products from CSV
        List<Product> products = Product.loadFromCSV("../data/products.csv");
        
        // Task 1 - Display all products
        System.out.println("Task 1: All Products");
        for (Product product : products) {
            System.out.println(product);
        }
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
       
    }
}