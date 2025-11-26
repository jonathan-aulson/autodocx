class ApiKeyValidator {
  constructor() {
    this.validApiKeys = new Set();
    this.loadApiKeysFromEnv();
  }

  loadApiKeysFromEnv() {
    const apiTokensEnv = process.env.API_TOKEN;
    
    if (!apiTokensEnv) {
      console.log("API_TOKEN environment variable not set. API validation is disabled.");
      this.validationEnabled = false;
      return;
    }

    const apiTokens = apiTokensEnv.split(',').map(token => token.trim()).filter(token => token);
    
    if (apiTokens.length === 0) {
      console.log("No valid API tokens found. API validation is disabled.");
      this.validationEnabled = false;
      return;
    }

    this.validApiKeys.clear();
    apiTokens.forEach(token => this.validApiKeys.add(token));
    this.validationEnabled = true;
    
    console.log(`Loaded ${apiTokens.length} API token(s) from environment variables. API validation is enabled.`);
  }

  validateApiKey(apiKey) {
    if (!apiKey) {
      return false;
    }

    return this.validApiKeys.has(apiKey);
  }

  createMiddleware() {
    return async (request, reply) => {
      // If validation is disabled, skip all checks
      if (!this.validationEnabled) {
        return; // Allow request to proceed
      }

      const apiKey = request.headers['x-api-key'] || request.headers['authorization']?.replace('Bearer ', '');
      
      if (!apiKey) {
        return reply.status(401).send({
          error: "Unauthorized",
          message: "API key is required. Provide it via 'x-api-key' header or 'Authorization: Bearer <key>' header."
        });
      }

      const isValid = this.validateApiKey(apiKey);
      
      if (!isValid) {
        return reply.status(403).send({
          error: "Forbidden",
          message: "Invalid API key provided."
        });
      }
    };
  }
}

module.exports = ApiKeyValidator;