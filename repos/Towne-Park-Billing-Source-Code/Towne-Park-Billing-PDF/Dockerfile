# Step 1: Use a Node.js image to set up a build environment
FROM node:lts AS builder
 
WORKDIR /app
 
# Step 2: Copy package.json and install dependencies
COPY package*.json ./
RUN npm install
 
# Step 3: Copy the application source and build it
COPY . .
 
# Make sure your package.json has a "build" script for transpiling JSX
RUN npm run build
 
# Step 4: Use a smaller Node.js image for the production environment
FROM node:lts-slim AS runtime
 
WORKDIR /app
 
# Step 5: Copy only the built output from the builder stage
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/public ./public
COPY package*.json ./
 
# Step 6: Install production dependencies only
#RUN npm install --only=production
# TODO: need to understand why when using the "--only=production" command line argument that
RUN npm install
 
# Expose the port that Fastify will run on
EXPOSE 3000
 
# Step 7: Command to run the app
# Update to the main entry point
CMD ["node", "dist/server.js"]
 
 