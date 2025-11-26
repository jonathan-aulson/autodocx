const report = require("multiple-cucumber-html-reporter");
report.generate({
jsonDir: "cypress/cucumber-json",  // ** Path of .json file **//
reportPath: "cypress/reports",
metadata: {
browser: {
name: "chrome",
version: "134",
},
device: "Local test machine",
platform: {
name: "window",
version: "11",
},
},
});