import java.io.*;
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
}