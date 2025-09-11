# Use Node.js 18 as base image
FROM node:18

# Install Docker CLI
RUN apt-get update && apt-get install -y docker.io

# Set working directory
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm install --production

# Copy server source code
COPY server.js ./

# Create workspace and submissions directories
RUN mkdir -p workspace/src workspace/data submissions

# Expose port 3000
EXPOSE 3000

# Default command to run the server
CMD ["node", "server.js"]