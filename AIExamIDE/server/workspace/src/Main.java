import java.util.*;
import java.util.stream.Collectors;

public class Main {
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
        
        // Task 2 - Filter products by Electronics category
        System.out.println("Task 2: Electronics Products");
        products.stream()
            .filter(p -> "Electronics".equals(p.getCategory()))
            .forEach(System.out::println);
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
        // Task 3 - Sort products by price (ascending)
        System.out.println("Task 3: Products Sorted by Price");
        products.stream()
            .sorted((p1, p2) -> Double.compare(p1.getPrice(), p2.getPrice()))
            .forEach(System.out::println);
        
        System.out.println("\n" + "=".repeat(50) + "\n");
        
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
}