const { defineConfig } = require("cypress");
const createBundler = require("@bahmutov/cypress-esbuild-preprocessor");
const addCucumberPreprocessorPlugin = require("@badeball/cypress-cucumber-preprocessor").addCucumberPreprocessorPlugin;
const createEsbuildPlugin = require("@badeball/cypress-cucumber-preprocessor/esbuild").createEsbuildPlugin;


module.exports = defineConfig({
  

  e2e: {
    
    env: {
      baseUrl: 'https://ambitious-grass-00554670f-develop.eastus2.5.azurestaticapps.net/'
    },
    // retries: {
    //   runMode: 1,  // Number of retries when running through `cypress run`
    //   openMode: 1  // Number of retries when running in `cypress open`
    // },
    viewportWidth: 1280,
    viewportHeight: 720,
    defaultCommandTimeout: 50000, 
    experimentalSessionAndOrigin: true,
    experimentalMemoryManagement: true,
    experimentalModifyObstructiveThirdPartyCode: true,
    chromeWebSecurity:false,
    screenshotOnRunFailure: true,
    video: false, // Consider adding if you don't need videos, saves time/space
    
    async setupNodeEvents(on, config) {

      const bundler = createBundler({
        plugins: [createEsbuildPlugin(config)],
      });

      on("file:preprocessor", bundler);
      await addCucumberPreprocessorPlugin(on, config);

      
    
      return config;
    },

    specPattern: "cypress/e2e/**/*.feature", // specify the pattern for your feature files
    supportFile: "cypress/support/e2e.js",
  },
});
